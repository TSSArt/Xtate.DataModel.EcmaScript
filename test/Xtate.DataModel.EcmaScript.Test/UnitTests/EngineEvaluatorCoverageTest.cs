// Copyright © 2019-2026 Sergii Artemenko

using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Acornima.Ast;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.Ancestor;
using Xtate.DataModel.EcmaScript.Internal;
using Xtate.DataModel.EcmaScript.Services;
using Xtate.DataModel.Services;
using Xtate.DataTypes;
using Xtate.ResourceLoaders;
using Xtate.StateMachine;

namespace Xtate.DataModel.EcmaScript.Test.UnitTests;

[TestClass]
public class EngineEvaluatorCoverageTest
{
	private JsValue EngineEval(EcmaScriptEngine engine, Prepared<Script> program, bool startNewScope) => engine.Eval(new EcmaScriptProgram(program), startNewScope).AsTask().Result;

	[TestMethod]
	public void EngineSynchronizesGlobalVariablesAndTheInPredicate()
	{
		var dataModel = new DataModelList { ["existing"] = "root", [string.Empty] = "not-a-variable" };
		var inState = new Mock<IInStateController>();
		inState.Setup(controller => controller.InState(It.Is<IIdentifier>(id => id.Value == "active"))).Returns(true);
		var engine = CreateEngine(dataModel, inState.Object);

		Assert.AreEqual("root", EngineEval(engine, Compile("existing").Script, startNewScope: false).AsString());
		Assert.IsTrue(EngineEval(engine, Compile("In('active')").Script, startNewScope: true).AsBoolean());
		Assert.IsFalse(EngineEval(engine, Compile("In('inactive')").Script, startNewScope: true).AsBoolean());

		dataModel["added"] = 17;
		Assert.AreEqual(17, EngineEval(engine, Compile("added").Script, startNewScope: false).AsNumber());
		dataModel.RemoveFirst("existing", caseInsensitive: false);
		Assert.AreEqual("undefined", EngineEval(engine, Compile("typeof existing").Script, startNewScope: false).AsString());
		Assert.AreEqual("not-a-variable", dataModel[string.Empty].AsString());
	}

	[TestMethod]
	public async Task EngineAssignmentsHandleRootVariablesMembersAndPersistentGlobals()
	{
		var nested = DataModelConverter.CreateAsObject();
		nested["value"] = "before";
		var dataModel = new DataModelList { ["target"] = "old", ["nested"] = nested, ["setterCreations"] = 0 };
		var engine = CreateEngine(dataModel);

		await engine.Exec(Compile("target = 'new'"), startNewScope: false);
		Assert.AreEqual("new", dataModel["target"].AsString());

		var memberSetter = await engine.Eval(Compile("setterCreations++, (__value => (nested.value = __value))"), startNewScope: false);
		Assert.AreEqual(1, dataModel["setterCreations"].AsNumber().ToInt32());
		engine.Call(memberSetter, new JsString("changed"));
		Assert.AreEqual("changed", nested["value"].AsString());

		await engine.Exec(Compile("var created = 42"), startNewScope: false);
		Assert.AreEqual(42, (await engine.Eval(Compile("created"), startNewScope: false)).AsNumber());

		await engine.Exec(Compile("var another = 43"), startNewScope: true);
		Assert.AreEqual(43, (await engine.Eval(Compile("another"), startNewScope: false)).AsNumber());
		Assert.AreEqual("undefined", EngineEval(engine, Compile("typeof __xtate_location_value").Script, startNewScope: false).AsString());
	}

	[TestMethod]
	public async Task ValueConditionAndScriptEvaluatorsExposeAllContractViews()
	{
		var dataModel = new DataModelList { ["number"] = 7 };
		var engine = CreateEngine(dataModel);
		var source = Mock.Of<IValueExpression>(expression => expression.Expression == "number");
		var evaluator = new EcmaScriptValueExpressionEvaluator(source, Compile("number")) { EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };

		Assert.AreSame(source, ((IAncestorProvider)evaluator).Ancestor);
		Assert.AreEqual("number", evaluator.Expression);
		Assert.AreEqual(7, await ((IIntegerEvaluator)evaluator).EvaluateInteger());
		Assert.AreEqual("7", await ((IStringEvaluator)evaluator).EvaluateString());
		Assert.AreEqual(7d, (await ((IObjectEvaluator)evaluator).EvaluateObject()).ToObject());

		var arrayEvaluator = new EcmaScriptValueExpressionEvaluator(Mock.Of<IValueExpression>(), Compile("['one', 2]"))
			{ EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };
		var values = await arrayEvaluator.EvaluateArray();
		Assert.HasCount(2, values);
		Assert.AreEqual("one", values[0].ToObject());
		Assert.AreEqual(2d, values[1].ToObject());

		var conditionSource = Mock.Of<IConditionExpression>(expression => expression.Expression == "number === 7");
		var condition = new EcmaScriptConditionExpressionEvaluator(conditionSource, Compile("number === 7"))
			{ EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };
		Assert.AreSame(conditionSource, ((IAncestorProvider)condition).Ancestor);
		Assert.AreEqual("number === 7", condition.Expression);
		Assert.IsTrue(await ((IBooleanEvaluator)condition).EvaluateBoolean());

		var scriptSource = Mock.Of<IScriptExpression>(expression => expression.Expression == "number = 8");
		var script = new EcmaScriptScriptExpressionEvaluator(scriptSource, Compile("number = 8"))
			{ EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };
		Assert.AreSame(scriptSource, ((IAncestorProvider)script).Ancestor);
		Assert.AreEqual("number = 8", script.Expression);
		await script.Execute();
		Assert.AreEqual(8, dataModel["number"].AsNumber().ToInt32());
	}

	[TestMethod]
	public async Task LocationEvaluatorReadsNamesAndAssignsGlobalVariablesAndMembers()
	{
		var nested = DataModelConverter.CreateAsObject();
		nested["value"] = "before";
		var dataModel = new DataModelList { ["target"] = "old", ["nested"] = nested };
		var engine = CreateEngine(dataModel);
		var source = Mock.Of<ILocationExpression>(expression => expression.Expression == "target");
		var targetProgram = Compile("target");
		var target = new EcmaScriptLocationExpressionEvaluator(source, targetProgram)
			{ EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };

		Assert.AreSame(source, ((IAncestorProvider)target).Ancestor);
		Assert.AreEqual("target", target.Expression);
		Assert.AreEqual("target", await target.GetName());
		Assert.AreEqual("old", (await target.GetValue()).ToObject());
		await target.SetValue(new EcmaScriptObject(new JsString("new")));
		Assert.AreEqual("new", dataModel["target"].AsString());
		await target.SetValue(new TestObject("again"));
		Assert.AreEqual("again", dataModel["target"].AsString());

		var memberSource = Mock.Of<ILocationExpression>(expression => expression.Expression == "nested.value");
		var memberProgram = Compile("nested.value");
		var member = new EcmaScriptLocationExpressionEvaluator(memberSource, memberProgram)
			{ EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };
		Assert.AreEqual("value", await member.GetName());
		await member.SetValue(new TestObject("member"));
		Assert.AreEqual("member", nested["value"].AsString());

		var collisionValue = DataModelConverter.CreateAsObject();
		collisionValue["value"] = "before";
		dataModel["__xtate_location_value"] = collisionValue;
		var collisionSource = Mock.Of<ILocationExpression>(expression => expression.Expression == "__xtate_location_value.value");
		var collisionProgram = Compile("__xtate_location_value.value");
		var collision = new EcmaScriptLocationExpressionEvaluator(collisionSource, collisionProgram)
			{ EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };
		await collision.SetValue(new TestObject("collision"));
		Assert.AreEqual("collision", collisionValue["value"].AsString());

		await target.DeclareLocalVariable();
		await target.SetValue(new TestObject("local"));
		Assert.AreEqual("local", (await target.GetValue()).ToObject());
		Assert.AreEqual("local", dataModel["target"].AsString());
	}

	[TestMethod]
	public async Task LocationEvaluatorSetValueUpdatesRootAndNestedLocations()
	{
		var nested = DataModelConverter.CreateAsObject();
		nested["value"] = "before";
		var collisionValue = DataModelConverter.CreateAsObject();
		collisionValue["value"] = "before";
		var dataModel = new DataModelList
						{
							["target"] = "old",
							["nested"] = nested,
							["__xtate_location_value"] = collisionValue
						};
		var engine = CreateEngine(dataModel);

		var target = new EcmaScriptLocationExpressionEvaluator(
			Mock.Of<ILocationExpression>(expression => expression.Expression == "target"),
			Compile("target"))
					 {
						 EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine)
					 };
		await target.SetValue(new EcmaScriptObject(new JsString("new")));

		var member = new EcmaScriptLocationExpressionEvaluator(
			Mock.Of<ILocationExpression>(expression => expression.Expression == "nested.value"),
			Compile("nested.value"))
					 {
						 EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine)
					 };
		await member.SetValue(new TestObject("member"));

		var collision = new EcmaScriptLocationExpressionEvaluator(
			Mock.Of<ILocationExpression>(expression => expression.Expression == "__xtate_location_value.value"),
			Compile("__xtate_location_value.value"))
						{
							EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine)
						};
		await collision.SetValue(new TestObject("collision"));

		Assert.AreEqual("new", dataModel["target"].AsString());
		Assert.AreEqual("member", nested["value"].AsString());
		Assert.AreEqual("collision", collisionValue["value"].AsString());
	}

	[TestMethod]
	public async Task LocationEvaluatorDeclaresOnlyIdentifierLocations()
	{
		var dataModel = new DataModelList();
		var engine = CreateEngine(dataModel);
		var item = new EcmaScriptLocationExpressionEvaluator(
			Mock.Of<ILocationExpression>(expression => expression.Expression == "item"),
			Compile("item"))
				   {
					   EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine)
				   };

		await item.DeclareLocalVariable();
		await item.SetValue(new TestObject("local"));

		Assert.AreEqual("local", (await item.GetValue()).ToObject());
		Assert.IsFalse(dataModel.ContainsKey("item", caseInsensitive: false));

		var member = new EcmaScriptLocationExpressionEvaluator(
			Mock.Of<ILocationExpression>(expression => expression.Expression == "item.value"),
			Compile("item.value"))
					 {
						 EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine)
					 };

		await Assert.ThrowsExactlyAsync<ExecutionException>(async () => await member.DeclareLocalVariable());
	}

	[TestMethod]
	public void LocationEvaluatorRejectsUnsupportedExpressions()
	{
		var emptySource = Mock.Of<ILocationExpression>(expression => expression.Expression == "undefined");
		var unsupported = Compile("1 + 2");

		Assert.ThrowsExactly<InvalidOperationException>(() => new EcmaScriptLocationExpressionEvaluator(emptySource, unsupported) { EngineFactory = null! });
	}

	[TestMethod]
	public async Task LocationEvaluatorAvoidsSetterParameterNameCollisions()
	{
		var target = DataModelConverter.CreateAsObject();
		target["value"] = "before";
		var dataModel = new DataModelList { ["__xv"] = target };
		var engine = CreateEngine(dataModel);
		var evaluator = new EcmaScriptLocationExpressionEvaluator(
			Mock.Of<ILocationExpression>(expression => expression.Expression == "__xv.value"),
			Compile("__xv.value"))
						{
							EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine)
						};

		await evaluator.SetValue(new TestObject("after"));

		Assert.AreEqual("after", target["value"].AsString());
	}

	[TestMethod]
	public async Task LocationEvaluatorReportsAnUnavailableName()
	{
		var program = Compile("undefined");
		var statement = (ExpressionStatement)program.Script.Program!.Body.Single();
		var expressionField = typeof(ExpressionStatement).GetField("<Expression>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.IsNotNull(expressionField);
		expressionField.SetValue(statement, null);
		var evaluator = new EcmaScriptLocationExpressionEvaluator(
			Mock.Of<ILocationExpression>(expression => expression.Expression == "undefined"),
			program)
						{
							EngineFactory = null!
						};

		await Assert.ThrowsExactlyAsync<ExecutionException>(async () => await evaluator.GetName());
	}

	[TestMethod]
	public async Task ExternalScriptEvaluatorLoadsExecutesAndPreservesSourceContract()
	{
		var dataModel = new DataModelList { ["value"] = 1 };
		var engine = CreateEngine(dataModel);
		var uri = new Uri("https://example.test/script.js");
		var source = Mock.Of<IExternalScriptExpression>(expression => expression.Uri == uri);
		var evaluator = new EcmaScriptExternalScriptExpressionEvaluator(source) { EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };

		Assert.AreSame(source, ((IAncestorProvider)evaluator).Ancestor);
		Assert.AreEqual(uri, evaluator.Uri);
		((IExternalScriptConsumer)evaluator).SetContent("value = 9");
		await evaluator.Execute();
		Assert.AreEqual(9, dataModel["value"].AsNumber().ToInt32());
	}

	[TestMethod]
	public async Task JsonContentAndExternalDataEvaluatorsParseObjectsNullAndResources()
	{
		var inline = new TestInlineContentEvaluator(Mock.Of<IInlineContent>(content => content.Value == "{\"key\":\"value\"}")) { Logger = null! };
		Assert.AreEqual("value", inline.Parse().AsList()["key"].AsString());
		Assert.AreEqual(DataModelValueType.Null, new TestInlineContentEvaluator(Mock.Of<IInlineContent>()) { Logger = null! }.Parse().Type);

		var body = new TestContentBodyEvaluator(Mock.Of<IContentBody>(content => content.Value == "[1,2]")) { Logger = null! };
		Assert.AreEqual(2, body.Parse().AsList().Count);
		Assert.AreEqual(DataModelValueType.Null, new TestContentBodyEvaluator(Mock.Of<IContentBody>()) { Logger = null! }.Parse().Type);

		await using var resource = new Resource(new MemoryStream(Encoding.UTF8.GetBytes("{\"loaded\":true}")), new ContentType("application/json"));
		var external = new TestExternalDataExpressionEvaluator(Mock.Of<IExternalDataExpression>()) { DataConverter = null!, ResourceLoader = null! };
		Assert.IsTrue((await external.Parse(resource)).AsList()["loaded"].AsBoolean());
	}

	private static EcmaScriptEngine CreateEngine(DataModelList dataModel, IInStateController? inStateController = null) =>
		new()
		{
			DataModelController = Mock.Of<IDataModelController>(controller => controller.DataModel == dataModel),
			InStateController = inStateController ?? Mock.Of<IInStateController>()
		};

	private static EcmaScriptProgram Compile(string source) => EcmaScriptProgram.ParseScript(source);

	private static Prepared<Script> CompileSetter(string location) => Compile($@"__value => ({location} = __value)").Script;

	private sealed class TestObject(object? value) : IObject
	{
		public object? ToObject() => value;
	}

	private sealed class TestInlineContentEvaluator(IInlineContent content) : EcmaScriptInlineContentEvaluator(content)
	{
		public DataModelValue Parse() => ParseToDataModel();
	}

	private sealed class TestContentBodyEvaluator(IContentBody content) : EcmaScriptContentBodyEvaluator(content)
	{
		public DataModelValue Parse() => ParseToDataModel();
	}

	private sealed class TestExternalDataExpressionEvaluator(IExternalDataExpression expression) : EcmaScriptExternalDataExpressionEvaluator(expression)
	{
		public ValueTask<DataModelValue> Parse(Resource resource) => ParseToDataModel(resource);
	}
}
