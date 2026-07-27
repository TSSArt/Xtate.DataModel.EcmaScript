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

using Jint;
using Jint.Native;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.DataModel.EcmaScript.Internal;
using Xtate.DataModel.EcmaScript.Services;
using Xtate.DataModel.Services;
using Xtate.DataTypes;

namespace Xtate.DataModel.EcmaScript.Test.UnitTests;

[TestClass]
public class DeepObjectSynchronizationTest
{
	[TestMethod]
	public void DeepDataModelTreeIsReadableFromJavaScript()
	{
		var engine = CreateEngine();
		var tree = CreateDeepTree();
		Expose(engine, name: "model", tree.Root);

		Assert.AreEqual(expected: "root", engine.JintEngine.Evaluate("model.profile.name").AsString());
		Assert.AreEqual(expected: "first", engine.JintEngine.Evaluate("model.profile.items[0].title").AsString());
		Assert.IsTrue(engine.JintEngine.Evaluate("model.profile.items[0].details.active").AsBoolean());
	}

	[TestMethod]
	public void JavaScriptMutationsOfExistingDeepLeavesUpdateBackingTree()
	{
		var engine = CreateEngine();
		var tree = CreateDeepTree();
		Expose(engine, name: "model", tree.Root);

		engine.JintEngine.Execute(
			"model.profile.name = 'changed';"
			+ "model.profile.items[0].title = 'updated';"
			+ "model.profile.items[0].details.active = false");

		Assert.AreEqual(expected: "changed", tree.Profile["name"].AsString());
		Assert.AreEqual(expected: "updated", tree.FirstItem["title"].AsString());
		Assert.IsFalse(tree.Details["active"].AsBoolean());
	}

	[TestMethod]
	public void BackingTreeMutationsAreVisibleThroughExistingJavaScriptWrapper()
	{
		var engine = CreateEngine();
		var tree = CreateDeepTree();
		Expose(engine, name: "model", tree.Root);

		tree.Profile["name"] = "from-data-model";
		tree.Details["active"] = false;
		tree.Details["late"] = 17;
		tree.Items.Add("tail");

		Assert.AreEqual(expected: "from-data-model", engine.JintEngine.Evaluate("model.profile.name").AsString());
		Assert.IsFalse(engine.JintEngine.Evaluate("model.profile.items[0].details.active").AsBoolean());
		Assert.AreEqual(expected: 17, engine.JintEngine.Evaluate("model.profile.items[0].details.late").AsNumber());
		Assert.AreEqual(expected: "tail", engine.JintEngine.Evaluate("model.profile.items[1]").AsString());
	}

	[TestMethod]
	public void JavaScriptReplacementOfDeepObjectPropertyUpdatesBackingTree()
	{
		var engine = CreateEngine();
		var tree = CreateDeepTree();
		Expose(engine, name: "model", tree.Root);

		engine.JintEngine.Execute(
			"model.profile.items[0].details = {"
			+ " active: false,"
			+ " nested: { count: 3 }"
			+ "}");

		var details = tree.FirstItem["details"].AsList();

		Assert.IsFalse(details["active"].AsBoolean());
		Assert.AreEqual(expected: 3, details["nested"].AsList()["count"].AsNumber().ToInt32());
	}

	[TestMethod]
	public void JavaScriptReplacementOfDeepArrayElementUpdatesBackingTree()
	{
		var engine = CreateEngine();
		var tree = CreateDeepTree();
		Expose(engine, name: "model", tree.Root);

		engine.JintEngine.Execute(
			"model.profile.items[0] = {"
			+ " title: 'replacement',"
			+ " details: { active: false }"
			+ "}");

		var replacement = tree.Items[0].AsList();

		Assert.AreEqual(expected: "replacement", replacement["title"].AsString());
		Assert.IsFalse(replacement["details"].AsList()["active"].AsBoolean());
	}

	[TestMethod]
	public void WrappedDeepTreeRoundTripPreservesRootAndNestedReferences()
	{
		var engine = CreateEngine();
		var tree = CreateDeepTree();
		var jsValue = Expose(engine, name: "model", tree.Root);

		var dataModelValue = EcmaScriptHelper.JsValueToDataModelValue(jsValue);
		var roundTrip = EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, dataModelValue);

		Assert.AreSame(tree.Root, dataModelValue.AsList());
		Assert.AreSame(tree.Profile, dataModelValue.AsList()["profile"].AsList());
		Assert.AreSame(tree.Items, dataModelValue.AsList()["profile"].AsList()["items"].AsList());
		Assert.AreSame(jsValue.AsObject(), roundTrip.AsObject());
	}

	[TestMethod]
	public void SharedNestedDataModelReferenceStaysSynchronizedAcrossJavaScriptPaths()
	{
		var engine = CreateEngine();
		var shared = DataModelConverter.CreateAsObject();
		shared["value"] = 1;
		var root = DataModelConverter.CreateAsObject();
		root["left"] = shared;
		root["right"] = shared;
		Expose(engine, name: "model", root);

		engine.JintEngine.Execute("model.left.value = 2");

		Assert.AreEqual(expected: 2, shared["value"].AsNumber().ToInt32());
		Assert.AreEqual(expected: 2, engine.JintEngine.Evaluate("model.right.value").AsNumber());
	}

	[TestMethod]
	public void DeepJavaScriptTreeConvertsToDataModelAndBackWithLiveMutations()
	{
		var engine = CreateEngine();
		var jsValue = engine.JintEngine.Evaluate("({ branch: { items: [{ value: 1 }] } })");
		var dataModelValue = EcmaScriptHelper.JsValueToDataModelValue(jsValue);
		var roundTrip = EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, dataModelValue);
		engine.JintEngine.SetValue(name: "roundTrip", roundTrip);

		engine.JintEngine.Execute("roundTrip.branch.items[0].value = 9");

		var item = dataModelValue.AsList()["branch"].AsList()["items"].AsList()[0].AsList();

		Assert.AreEqual(expected: 9, item["value"].AsNumber().ToInt32());
		Assert.AreEqual(expected: 9, engine.JintEngine.Evaluate("roundTrip.branch.items[0].value").AsNumber());
	}

	[TestMethod]
	public void DeepUndefinedAndNullValuesStaySynchronized()
	{
		var engine = CreateEngine();
		var values = DataModelConverter.CreateAsArray();
		values.Add(DataModelValue.Undefined);
		values.Add(DataModelValue.Null);
		var root = DataModelConverter.CreateAsObject();
		root["values"] = values;
		Expose(engine, name: "model", root);

		Assert.IsTrue(engine.JintEngine.Evaluate("model.values[0]").IsUndefined());
		Assert.IsTrue(engine.JintEngine.Evaluate("model.values[1]").IsNull());

		engine.JintEngine.Execute("model.values[0] = null; model.values[1] = undefined");

		Assert.IsTrue(values[1].IsUndefined());
		Assert.AreEqual(DataModelValueType.Null, values[0].Type);
	}

	[TestMethod]
	public void RemovingDeepBackingPropertyMakesJavaScriptPropertyUndefined()
	{
		var engine = CreateEngine();
		var tree = CreateDeepTree();
		Expose(engine, name: "model", tree.Root);
		Assert.IsTrue(engine.JintEngine.Evaluate("model.profile.items[0].details.active").AsBoolean());

		tree.Details.RemoveFirst(key: "active", caseInsensitive: false);

		Assert.AreEqual(
			expected: "undefined",
			engine.JintEngine.Evaluate("typeof model.profile.items[0].details.active").AsString());
	}

	private static EcmaScriptEngine CreateEngine()
	{
		var dataModel = new DataModelList();

		return new EcmaScriptEngine
			   {
				   DataModelController = Mock.Of<IDataModelController>(controller => controller.DataModel == dataModel),
				   InStateController = Mock.Of<IInStateController>()
			   };
	}

	private static JsValue Expose(EcmaScriptEngine engine, string name, DataModelList root)
	{
		var value = EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, root);
		engine.JintEngine.SetValue(name, value);

		return value;
	}

	private static (DataModelList Root, DataModelList Profile, DataModelList Items, DataModelList FirstItem, DataModelList Details) CreateDeepTree()
	{
		var details = DataModelConverter.CreateAsObject();
		details["active"] = true;

		var firstItem = DataModelConverter.CreateAsObject();
		firstItem["title"] = "first";
		firstItem["details"] = details;

		var items = DataModelConverter.CreateAsArray();
		items.Add(firstItem);

		var profile = DataModelConverter.CreateAsObject();
		profile["name"] = "root";
		profile["items"] = items;

		var root = DataModelConverter.CreateAsObject();
		root["profile"] = profile;

		return (root, profile, items, firstItem, details);
	}
}