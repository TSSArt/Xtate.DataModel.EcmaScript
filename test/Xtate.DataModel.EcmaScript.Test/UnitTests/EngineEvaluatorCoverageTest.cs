// Copyright © 2019-2026 Sergii Artemenko
// 
// This file is part of the Xtate project. <https://xtate.net/>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Threading.Tasks;
using Acornima.Ast;
using Jint;
using Jint.Native;
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
	private static JsValue EngineEval(EcmaScriptEngine engine, Prepared<Script> program, bool startNewScope) => engine.Eval(new EcmaScriptProgram(program), startNewScope).AsTask().Result;

	[TestMethod]
	public void EngineSynchronizesGlobalVariablesAndTheInPredicate()
	{
		var dataModel = new DataModelList { ["existing"] = "root", [string.Empty] = "not-a-variable" };
		var inState = new Mock<IInStateController>();
		inState.Setup(controller => controller.InState(It.Is<IIdentifier>(id => id.Value == "active"))).Returns(true);
		var engine = CreateEngine(dataModel, inState.Object);

		Assert.AreEqual(expected: "root", EngineEval(engine, Compile("existing").Script, startNewScope: false).AsString());
		Assert.IsTrue(EngineEval(engine, Compile("In('active')").Script, startNewScope: true).AsBoolean());
		Assert.IsFalse(EngineEval(engine, Compile("In('inactive')").Script, startNewScope: true).AsBoolean());

		dataModel["added"] = 17;
		Assert.AreEqual(expected: 17, EngineEval(engine, Compile("added").Script, startNewScope: false).AsNumber());
		dataModel.RemoveFirst(key: "existing", caseInsensitive: false);
		Assert.AreEqual(expected: "undefined", EngineEval(engine, Compile("typeof existing").Script, startNewScope: false).AsString());
		Assert.AreEqual(expected: "not-a-variable", dataModel[string.Empty].AsString());
	}

	[TestMethod]
	public async Task EngineAssignmentsHandleRootVariablesMembersAndPersistentGlobals()
	{
		var nested = DataModelConverter.CreateAsObject();
		nested["value"] = "before";
		var dataModel = new DataModelList { ["target"] = "old", ["nested"] = nested, ["setterCreations"] = 0 };
		var engine = CreateEngine(dataModel);

		await engine.Exec(Compile("target = 'new'"), startNewScope: true);
		Assert.AreEqual(expected: "new", dataModel["target"].AsString());

		var memberSetter = await engine.Eval(Compile("setterCreations++, (__value => (nested.value = __value))"), startNewScope: true);
		Assert.AreEqual(expected: 1, dataModel["setterCreations"].AsNumber().ToInt32());
		engine.Call(memberSetter, new JsString("changed"));
		Assert.AreEqual(expected: "changed", nested["value"].AsString());

		await engine.Exec(Compile("var created = 42"), startNewScope: true);
		Assert.AreEqual(expected: 42, (await engine.Eval(Compile("created"), startNewScope: true)).AsNumber());

		await engine.Exec(Compile("var another = 43"), startNewScope: true);
		Assert.AreEqual(expected: 43, (await engine.Eval(Compile("another"), startNewScope: true)).AsNumber());
		Assert.AreEqual(expected: "undefined", EngineEval(engine, Compile("typeof __xtate_location_value").Script, startNewScope: false).AsString());
	}

	[TestMethod]
	public async Task ValueConditionAndScriptEvaluatorsExposeAllContractViews()
	{
		var dataModel = new DataModelList { ["number"] = 7 };
		var engine = CreateEngine(dataModel);
		var source = Mock.Of<IValueExpression>(expression => expression.Expression == "number");
		var evaluator = new EcmaScriptValueExpressionEvaluator(source, Compile("number")) { EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };

		Assert.AreSame(source, ((IAncestorProvider)evaluator).Ancestor);
		Assert.AreEqual(expected: "number", evaluator.Expression);
		Assert.AreEqual(expected: 7, await ((IIntegerEvaluator)evaluator).EvaluateInteger());
		Assert.AreEqual(expected: "7", await ((IStringEvaluator)evaluator).EvaluateString());
		Assert.AreEqual(expected: 7d, (await ((IObjectEvaluator)evaluator).EvaluateObject()).ToObject());

		var arrayEvaluator = new EcmaScriptValueExpressionEvaluator(Mock.Of<IValueExpression>(), Compile("['one', 2]"))
							 { EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };
		var values = await arrayEvaluator.EvaluateArray();
		Assert.HasCount(expected: 2, values);
		Assert.AreEqual(expected: "one", values[0].ToObject());
		Assert.AreEqual(expected: 2d, values[1].ToObject());

		var conditionSource = Mock.Of<IConditionExpression>(expression => expression.Expression == "number === 7");
		var condition = new EcmaScriptConditionExpressionEvaluator(conditionSource, Compile("number === 7"))
						{ EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };
		Assert.AreSame(conditionSource, ((IAncestorProvider)condition).Ancestor);
		Assert.AreEqual(expected: "number === 7", condition.Expression);
		Assert.IsTrue(await ((IBooleanEvaluator)condition).EvaluateBoolean());

		var scriptSource = Mock.Of<IScriptExpression>(expression => expression.Expression == "number = 8");
		var script = new EcmaScriptScriptExpressionEvaluator(scriptSource, Compile("number = 8"))
					 { EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };
		Assert.AreSame(scriptSource, ((IAncestorProvider)script).Ancestor);
		Assert.AreEqual(expected: "number = 8", script.Expression);
		await script.Execute();
		Assert.AreEqual(expected: 8, dataModel["number"].AsNumber().ToInt32());
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
		Assert.AreEqual(expected: "target", target.Expression);
		Assert.AreEqual(expected: "target", await target.GetName());
		Assert.AreEqual(expected: "old", (await target.GetValue()).ToObject());
		await target.SetValue(new EcmaScriptObject(new JsString("new")));
		Assert.AreEqual(expected: "new", dataModel["target"].AsString());
		await target.SetValue(new TestObject("again"));
		Assert.AreEqual(expected: "again", dataModel["target"].AsString());

		var memberSource = Mock.Of<ILocationExpression>(expression => expression.Expression == "nested.value");
		var memberProgram = Compile("nested.value");
		var member = new EcmaScriptLocationExpressionEvaluator(memberSource, memberProgram)
					 { EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };
		Assert.AreEqual(expected: "value", await member.GetName());
		await member.SetValue(new TestObject("member"));
		Assert.AreEqual(expected: "member", nested["value"].AsString());

		var collisionValue = DataModelConverter.CreateAsObject();
		collisionValue["value"] = "before";
		dataModel["__xtate_location_value"] = collisionValue;
		var collisionSource = Mock.Of<ILocationExpression>(expression => expression.Expression == "__xtate_location_value.value");
		var collisionProgram = Compile("__xtate_location_value.value");
		var collision = new EcmaScriptLocationExpressionEvaluator(collisionSource, collisionProgram)
						{ EngineFactory = () => new ValueTask<EcmaScriptEngine>(engine) };
		await collision.SetValue(new TestObject("collision"));
		Assert.AreEqual(expected: "collision", collisionValue["value"].AsString());

		await target.DeclareLocalVariable();
		await target.SetValue(new TestObject("local"));
		Assert.AreEqual(expected: "local", (await target.GetValue()).ToObject());
		Assert.AreEqual(expected: "local", dataModel["target"].AsString());
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

		Assert.AreEqual(expected: "new", dataModel["target"].AsString());
		Assert.AreEqual(expected: "member", nested["value"].AsString());
		Assert.AreEqual(expected: "collision", collisionValue["value"].AsString());
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

		Assert.AreEqual(expected: "local", (await item.GetValue()).ToObject());
		Assert.IsFalse(dataModel.ContainsKey(key: "item", caseInsensitive: false));

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

		Assert.AreEqual(expected: "after", target["value"].AsString());
	}

	[TestMethod]
	public async Task LocationEvaluatorReportsAnUnavailableName()
	{
		var program = Compile("undefined");
		var statement = (ExpressionStatement)program.Script.Program!.Body.Single();
		var expressionField = typeof(ExpressionStatement).GetField(name: "<Expression>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.IsNotNull(expressionField);
		expressionField.SetValue(statement, value: null);
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
		Assert.AreEqual(expected: 9, dataModel["value"].AsNumber().ToInt32());
	}

	[TestMethod]
	public async Task JsonContentAndExternalDataEvaluatorsParseObjectsNullAndResources()
	{
		var inline = new TestInlineContentEvaluator(Mock.Of<IInlineContent>(content => content.Value == "{\"key\":\"value\"}")) { Logger = null! };
		Assert.AreEqual(expected: "value", inline.Parse().AsList()["key"].AsString());
		Assert.AreEqual(DataModelValueType.Null, new TestInlineContentEvaluator(Mock.Of<IInlineContent>()) { Logger = null! }.Parse().Type);

		var body = new TestContentBodyEvaluator(Mock.Of<IContentBody>(content => content.Value == "[1,2]")) { Logger = null! };
		Assert.AreEqual(expected: 2, body.Parse().AsList().Count);
		Assert.AreEqual(DataModelValueType.Null, new TestContentBodyEvaluator(Mock.Of<IContentBody>()) { Logger = null! }.Parse().Type);

		await using var resource = new Resource(new MemoryStream([.. "{\"loaded\":true}"u8]), new ContentType("application/json"));
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

	private sealed class TestObject(object? value) : IObject
	{
	#region Interface IObject

		public object? ToObject() => value;

	#endregion
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