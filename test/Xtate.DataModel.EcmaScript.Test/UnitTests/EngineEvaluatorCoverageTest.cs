// Copyright © 2019-2026 Sergii Artemenko

using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
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
	[TestMethod]
	public void EngineSynchronizesGlobalVariablesAndTheInPredicate()
	{
		var dataModel = new DataModelList { ["existing"] = "root", [string.Empty] = "not-a-variable" };
		var inState = new Mock<IInStateController>();
		inState.Setup(controller => controller.InState(It.Is<IIdentifier>(id => id.Value == "active"))).Returns(true);
		var engine = CreateEngine(dataModel, inState.Object);

		Assert.AreEqual("root", engine.Eval(Compile("existing"), startNewScope: false).AsString());
		Assert.IsTrue(engine.Eval(Compile("In('active')"), startNewScope: true).AsBoolean());
		Assert.IsFalse(engine.Eval(Compile("In('inactive')"), startNewScope: true).AsBoolean());

		dataModel["added"] = 17;
		Assert.AreEqual(17, engine.Eval(Compile("added"), startNewScope: false).AsNumber());
		dataModel.RemoveFirst("existing", caseInsensitive: false);
		Assert.AreEqual("undefined", engine.Eval(Compile("typeof existing"), startNewScope: false).AsString());
		Assert.AreEqual("not-a-variable", dataModel[string.Empty].AsString());
	}

	[TestMethod]
	public void EngineExecutionScopesRemoveCreatedGlobalsAndRestoreShadowedData()
	{
		var dataModel = new DataModelList { ["item"] = "root" };
		var engine = CreateEngine(dataModel);

		engine.Exec(Compile("var persistent = 1"), startNewScope: false);
		Assert.AreEqual(1, engine.Eval(Compile("persistent"), startNewScope: false).AsNumber());
		engine.Exec(Compile("var temporary = 2"), startNewScope: true);
		Assert.AreEqual("undefined", engine.Eval(Compile("typeof temporary"), startNewScope: false).AsString());

		engine.EnterExecutionContext();
		engine.DeclareLocalVariable("item");
		engine.DeclareLocalVariable("item");
		engine.SetLocationValue(Compile($"item = {EcmaScriptEngine.LocationValueProperty}"), "item", "local");
		Assert.AreEqual("local", engine.Eval(Compile("item"), startNewScope: false).AsString());
		Assert.AreEqual("root", dataModel["item"].AsString());
		engine.LeaveExecutionContext();
		Assert.AreEqual("root", engine.Eval(Compile("item"), startNewScope: false).AsString());

		Assert.ThrowsExactly<JavaScriptException>(() => engine.Eval(Compile("throw new Error('failure')"), startNewScope: true));
		Assert.ThrowsExactly<JavaScriptException>(() => engine.Exec(Compile("throw new Error('failure')"), startNewScope: true));
	}

	[TestMethod]
	public void EngineAssignmentsHandleRootVariablesMembersAndNativeValues()
	{
		var nested = DataModelConverter.CreateAsObject();
		nested["value"] = "before";
		var dataModel = new DataModelList { ["target"] = "old", ["nested"] = nested };
		var engine = CreateEngine(dataModel);

		engine.SetLocationValue(Compile($"target = {EcmaScriptEngine.LocationValueProperty}"), "target", "new");
		Assert.AreEqual("new", dataModel["target"].AsString());
		engine.SetLocationValue(Compile($"nested.value = {EcmaScriptEngine.LocationValueProperty}"), identifierName: null, new JsString("changed"));
		Assert.AreEqual("changed", nested["value"].AsString());
		engine.SetLocationValue(Compile($"created = {EcmaScriptEngine.LocationValueProperty}"), "created", 42);
		Assert.AreEqual(42, dataModel["created"].AsNumber().ToInt32());
		engine.EnterExecutionContext();
		engine.DeclareLocalVariable("unrelated");
		engine.SetLocationValue(Compile($"another = {EcmaScriptEngine.LocationValueProperty}"), "another", 43);
		engine.LeaveExecutionContext();
		Assert.AreEqual(43, dataModel["another"].AsNumber().ToInt32());
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
	public async Task LocationEvaluatorReadsNamesAssignsRootAndMemberAndHandlesLocalVariables()
	{
		var nested = DataModelConverter.CreateAsObject();
		nested["value"] = "before";
		var dataModel = new DataModelList { ["target"] = "old", ["nested"] = nested };
		var engine = CreateEngine(dataModel);
		var source = Mock.Of<ILocationExpression>(expression => expression.Expression == "target");
		var targetProgram = Compile("target");
		var target = new EcmaScriptLocationExpressionEvaluator(source, targetProgram, EcmaScriptLocationExpressionEvaluator.GetLeftExpression(targetProgram.Program!))
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
		var member = new EcmaScriptLocationExpressionEvaluator(memberSource, memberProgram, EcmaScriptLocationExpressionEvaluator.GetLeftExpression(memberProgram.Program!))
			{ EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };
		Assert.AreEqual("value", await member.GetName());
		await member.SetValue(new TestObject("member"));
		Assert.AreEqual("member", nested["value"].AsString());

		engine.EnterExecutionContext();
		await target.DeclareLocalVariable();
		await target.SetValue(new TestObject("local"));
		Assert.AreEqual("local", (await target.GetValue()).ToObject());
		engine.LeaveExecutionContext();
		Assert.AreEqual("again", dataModel["target"].AsString());
	}

	[TestMethod]
	public async Task LocationEvaluatorRejectsNamelessAndUnsupportedLocations()
	{
		var engine = CreateEngine(new DataModelList());
		var emptySource = Mock.Of<ILocationExpression>(expression => expression.Expression == "undefined");
		var evaluator = new EcmaScriptLocationExpressionEvaluator(emptySource, Compile("undefined"), leftExpression: null)
			{ EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };

		await Assert.ThrowsExactlyAsync<ExecutionException>(async () => await evaluator.GetName());
		await Assert.ThrowsExactlyAsync<ExecutionException>(async () => await evaluator.DeclareLocalVariable());
		Assert.IsNull(EcmaScriptLocationExpressionEvaluator.GetLeftExpression(Compile("1 + 2").Program!));
		Assert.IsNull(EcmaScriptLocationExpressionEvaluator.GetLeftExpression(Compile("if (true) {} ").Program!));
		var unsupported = Compile("1 + 2");
		Assert.ThrowsExactly<InvalidOperationException>(() => new EcmaScriptLocationExpressionEvaluator(emptySource, unsupported, ((ExpressionStatement)unsupported.Program!.Body[0]).Expression) { EngineFactory = null! });
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

	[TestMethod]
	public async Task CustomActionEvaluatorAlwaysLeavesItsExecutionScope()
	{
		var engine = CreateEngine(new DataModelList());
		var evaluator = new EcmaScriptCustomActionEvaluator(Mock.Of<ICustomAction>()) { EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };

		await evaluator.Execute();
		engine.EnterExecutionContext();
		engine.LeaveExecutionContext();
	}

	private static EcmaScriptEngine CreateEngine(DataModelList dataModel, IInStateController? inStateController = null) =>
		new()
		{
			DataModelController = Mock.Of<IDataModelController>(controller => controller.DataModel == dataModel),
			InStateController = inStateController ?? Mock.Of<IInStateController>()
		};

	private static Prepared<Script> Compile(string source) => Engine.PrepareScript(source);

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
