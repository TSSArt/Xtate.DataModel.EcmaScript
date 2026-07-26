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
using System.Globalization;
using System.Linq;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.DataModel.EcmaScript.Internal;
using Xtate.DataModel.EcmaScript.Services;
using Xtate.DataModel.Services;
using Xtate.DataTypes;

namespace Xtate.DataModel.EcmaScript.Test.UnitTests;

[TestClass]
public class EcmaScriptConversionTest
{
	[TestMethod]
	public void HelperConvertsEveryScalarDataModelTypeToJavaScript()
	{
		var engine = CreateEngine();
		var jint = engine.JintEngine;
		DataModelDateTime dateTime = new DateTimeOffset(2026, 7, 25, 12, 30, 0, TimeSpan.Zero);

		Assert.IsTrue(EcmaScriptHelper.DataModelValueToJsValue(jint, default).IsUndefined());
		Assert.IsTrue(EcmaScriptHelper.DataModelValueToJsValue(jint, DataModelValue.Null).IsNull());
		Assert.IsTrue(EcmaScriptHelper.DataModelValueToJsValue(jint, true).AsBoolean());
		Assert.AreEqual("text", EcmaScriptHelper.DataModelValueToJsValue(jint, "text").AsString());
		Assert.AreEqual(12.5, EcmaScriptHelper.DataModelValueToJsValue(jint, 12.5).AsNumber());
	}

	[TestMethod]
	public void HelperCreatesLiveWrappersForLists()
	{
		var engine = CreateEngine();
		var array = DataModelConverter.CreateAsArray();
		array.Add("first");
		var obj = DataModelConverter.CreateAsObject();
		obj["name"] = "before";

		var jsArray = EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, array);
		var jsObject = EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, obj);
		engine.JintEngine.SetValue("items", jsArray);
		engine.JintEngine.SetValue("model", jsObject);

		Assert.IsInstanceOfType<DataModelObjectWrapper>(jsObject.AsObject());
		Assert.AreEqual("first", engine.JintEngine.Evaluate("items[0]").AsString());
		Assert.AreEqual("before", engine.JintEngine.Evaluate("model.name").AsString());

		engine.JintEngine.Execute("items[0] = 'changed'; model.name = 'after'");

		Assert.AreEqual("changed", array[0].AsString());
		Assert.AreEqual("after", obj["name"].AsString());
	}

	[TestMethod]
	public void ArrayWrapperReadsAndWritesTheBackingList()
	{
		var engine = CreateEngine();
		var array = DataModelConverter.CreateAsArray();
		array.Add("first");
		var wrapper = EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, array);
		engine.JintEngine.SetValue("items", wrapper);

		Assert.AreEqual("first", engine.JintEngine.Evaluate("items[0]").AsString());

		engine.JintEngine.Execute("items[0] = 'changed'");

		Assert.AreEqual("changed", array[0].AsString());
	}

	[TestMethod]
	public void HelperConvertsJavaScriptScalarsAndDatesToDataModelValues()
	{
		var engine = CreateEngine();
		var jint = engine.JintEngine;

		Assert.IsTrue(Convert(jint.Evaluate("undefined")).IsUndefined());
		Assert.AreEqual(DataModelValueType.Null, Convert(jint.Evaluate("null")).Type);
		Assert.IsTrue(Convert(jint.Evaluate("true")).AsBoolean());
		Assert.AreEqual("ordinary text", Convert(jint.Evaluate("'ordinary text'")).AsString());
		Assert.AreEqual(42.5, Convert(jint.Evaluate("42.5")).AsNumber().ToDouble());
		Assert.AreEqual(DataModelValueType.DateTime, Convert(jint.Evaluate("new Date('2026-07-25T12:30:00Z')")).Type);
	}

	[TestMethod]
	public void HelperConvertsSparseJavaScriptArraysToDataModelArrays()
	{
		var engine = CreateEngine();

		var value = Convert(engine.JintEngine.Evaluate("[1,,3]"));
		var list = value.AsList();

		Assert.IsTrue(DataModelConverter.IsArray(list));
		Assert.AreEqual(3, list.Count);
		Assert.AreEqual(1, list[0].AsNumber().ToInt32());
		Assert.IsTrue(list[1].IsUndefined());
		Assert.AreEqual(3, list[2].AsNumber().ToInt32());
	}

	[TestMethod]
	public void HelperConvertsJavaScriptObjectsIncludingAccessors()
	{
		var engine = CreateEngine();

		var value = Convert(engine.JintEngine.Evaluate("({ visible: 'yes', get computed() { return 7; } })"));
		var list = value.AsList();

		Assert.IsTrue(DataModelConverter.IsObject(list));
		Assert.AreEqual("yes", list["visible"].AsString());
		Assert.AreEqual(7, list["computed"].AsNumber().ToInt32());
	}


	[TestMethod]
	public void ObjectWrapperReflectsBackingListChangesAndEnumeration()
	{
		var engine = CreateEngine();
		var list = DataModelConverter.CreateAsObject();
		list["existing"] = "before";
		var wrapper = (DataModelObjectWrapper)EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, list).AsObject();
		engine.JintEngine.SetValue("model", wrapper);

		CollectionAssert.AreEquivalent(new[] { "existing" }, wrapper.GetOwnProperties().Select(pair => pair.Key.ToString()).ToArray());
		list["added"] = 17;
		CollectionAssert.AreEquivalent(new[] { "existing", "added" }, wrapper.GetOwnProperties().Select(pair => pair.Key.ToString()).ToArray());

		engine.JintEngine.Execute("model.existing = 'after'");
		Assert.AreEqual("after", list["existing"].AsString());
		Assert.AreEqual(17, engine.JintEngine.Evaluate("model.added").AsNumber());

		list.RemoveFirst("existing", caseInsensitive: false);
		CollectionAssert.AreEquivalent(new[] { "added" }, wrapper.GetOwnProperties().Select(pair => pair.Key.ToString()).ToArray());
		Assert.AreEqual("undefined", engine.JintEngine.Evaluate("typeof model.existing").AsString());
	}

	[TestMethod]
	public void ObjectWrapperRemoveOwnPropertyUpdatesBackingList()
	{
		var engine = CreateEngine();
		var list = DataModelConverter.CreateAsObject();
		list["value"] = "present";
		var wrapper = (DataModelObjectWrapper)EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, list).AsObject();
		_ = wrapper.GetOwnProperties().ToArray();

		wrapper.RemoveOwnProperty("value");
		wrapper.RemoveOwnProperty(new JsNumber(1));

		Assert.IsFalse(list.ContainsKey("value", caseInsensitive: false));
	}

	[TestMethod]
	public void PropertyDescriptorReadsAndWritesTheBackingEntry()
	{
		var engine = CreateEngine();
		var list = new DataModelList { ["value"] = "before" };
		var descriptor = new ProxyPropertyDescriptor(engine.JintEngine, list, "value");
		engine.JintEngine.Global.DefineOwnProperty("modelValue", descriptor);

		Assert.IsTrue(descriptor.HasUnderlyingKey);
		Assert.AreEqual("before", engine.JintEngine.Evaluate("modelValue").AsString());

		engine.JintEngine.Execute("modelValue = 'after'");

		Assert.AreEqual("after", list["value"].AsString());
		list.RemoveFirst("value", caseInsensitive: false);
		Assert.IsFalse(descriptor.HasUnderlyingKey);
	}


	[TestMethod]
	public void ResourceFormattingSupportsEveryArity()
	{
		Assert.AreEqual("one", Res.Format("{0}", "one"));
		Assert.AreEqual("one-two", Res.Format("{0}-{1}", "one", "two"));
		Assert.AreEqual("one-two-three", Res.Format("{0}-{1}-{2}", "one", "two", "three"));
	}

	private static DataModelValue Convert(JsValue value) => EcmaScriptHelper.JsValueToDataModelValue(value);

	private static EcmaScriptEngine CreateEngine()
	{
		var dataModel = new DataModelList();

		return new EcmaScriptEngine
		{
			DataModelController = Mock.Of<IDataModelController>(controller => controller.DataModel == dataModel),
			InStateController = Mock.Of<IInStateController>()
		};
	}

	private sealed class TestObject(object? value) : IObject
	{
		public object? ToObject() => value;
	}

	[TestMethod]
	public void DataModelListToJsValueConversionTest()
	{
		var jintEngine = CreateEngine().JintEngine;

		var list = new DataModelList
				   {
					   [0] = new DataModelList { {"key", "nested"} },
					   [1] = "second",
					   [2] = "third"
				   };

		var jsValue = EcmaScriptHelper.DataModelValueToJsValue(jintEngine, list);
		jintEngine.Global.FastSetProperty("vvv", new PropertyDescriptor(jsValue, true, true, true));
		var value = jintEngine.Evaluate("vvv[1]");
		var value2 = jintEngine.Evaluate("vvv[0].key");

		jintEngine.Evaluate("vvv[3] = 'fourth'");

		Assert.AreEqual("second", value.AsString());
		Assert.AreEqual("fourth", list[3].AsString());
		Assert.AreEqual("nested", value2.AsString());
	}

	[TestMethod]
	public void NestedObjectMutationUpdatesTheOriginalDataModelList()
	{
		var engine = CreateEngine();
		var nested = DataModelConverter.CreateAsObject();
		nested["value"] = "before";
		var list = DataModelConverter.CreateAsArray();
		list.Add(nested);
		engine.JintEngine.SetValue("items", EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, list));

		engine.JintEngine.Execute("items[0].value = 'after'");

		Assert.AreEqual("after", nested["value"].AsString());
	}

	[TestMethod]
	public void NestedObjectPropertyCreationUpdatesTheOriginalDataModelList()
	{
		var engine = CreateEngine();
		var nested = DataModelConverter.CreateAsObject();
		var list = DataModelConverter.CreateAsArray();
		list.Add(nested);
		engine.JintEngine.SetValue("items", EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, list));

		engine.JintEngine.Execute("items[0].created = 42");

		Assert.AreEqual(42, nested["created"].AsNumber().ToInt32());
		Assert.AreEqual(42, engine.JintEngine.Evaluate("items[0].created").AsNumber());

		nested["created"] = 43;
		Assert.AreEqual(43, engine.JintEngine.Evaluate("items[0].created").AsNumber());

		engine.JintEngine.Execute("items[0].created = 44");
		Assert.AreEqual(44, nested["created"].AsNumber().ToInt32());
	}

	[TestMethod]
	public void NestedArraySupportsReadsAndWrites()
	{
		var engine = CreateEngine();
		var nested = DataModelConverter.CreateAsArray();
		nested.Add("first");
		nested.Add("second");
		var list = DataModelConverter.CreateAsArray();
		list.Add(nested);
		engine.JintEngine.SetValue("matrix", EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, list));

		Assert.AreEqual("second", engine.JintEngine.Evaluate("matrix[0][1]").AsString());

		engine.JintEngine.Execute("matrix[0][1] = 'changed'; matrix[0][2] = 'third'");

		Assert.AreEqual("changed", nested[1].AsString());
		Assert.AreEqual("third", nested[2].AsString());
	}

	[TestMethod]
	public void WrapperExposesPropertiesAddedToTheBackingObjectAfterCreation()
	{
		var engine = CreateEngine();
		var list = DataModelConverter.CreateAsObject();
		var wrapper = EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, list);
		engine.JintEngine.SetValue("model", wrapper);

		list["late"] = "available";

		Assert.AreEqual("available", engine.JintEngine.Evaluate("model.late").AsString());
	}

	[TestMethod]
	public void ObjectWrapperKeepsPropertyNamesCaseSensitive()
	{
		var engine = CreateEngine();
		var list = DataModelConverter.CreateAsObject();
		list["Name"] = "upper";
		list["name"] = "lower";
		engine.JintEngine.SetValue("model", EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, list));

		Assert.AreEqual("upper", engine.JintEngine.Evaluate("model.Name").AsString());
		Assert.AreEqual("lower", engine.JintEngine.Evaluate("model.name").AsString());

		engine.JintEngine.Execute("model.Name = 'changed'");

		Assert.AreEqual("changed", list["Name"].AsString());
		Assert.AreEqual("lower", list["name"].AsString());
	}

	[TestMethod]
	public void DataModelWrappersRoundTripWithoutLosingIdentity()
	{
		var engine = CreateEngine();
		var obj = DataModelConverter.CreateAsObject();
		obj["value"] = 1;
		var array = DataModelConverter.CreateAsArray();
		array.Add(2);
		var jsObject = EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, obj);
		var jsArray = EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, array);

		var objectValue = Convert(jsObject);
		var arrayValue = Convert(jsArray);
		var objectRoundTrip = EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, objectValue);
		var arrayRoundTrip = EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, arrayValue);

		Assert.AreSame(obj, objectValue.AsList());
		Assert.AreSame(array, arrayValue.AsList());
		Assert.AreSame(jsObject.AsObject(), objectRoundTrip.AsObject());
		Assert.AreSame(jsArray.AsObject(), arrayRoundTrip.AsObject());
	}

	[TestMethod]
	public void JavaScriptNestedStructuresConvertRecursively()
	{
		var engine = CreateEngine();

		var value = Convert(engine.JintEngine.Evaluate("({ child: { value: 7 }, items: [1, { name: 'two' }] })"));
		var root = value.AsList();
		var items = root["items"].AsList();

		Assert.AreEqual(7, root["child"].AsList()["value"].AsNumber().ToInt32());
		Assert.AreEqual(1, items[0].AsNumber().ToInt32());
		Assert.AreEqual("two", items[1].AsList()["name"].AsString());
	}

	[TestMethod]
	public void JavaScriptUndefinedObjectPropertyRemainsPresentInTheDataModel()
	{
		var engine = CreateEngine();

		var list = Convert(engine.JintEngine.Evaluate("({ present: undefined })")).AsList();

		Assert.IsTrue(list.ContainsKey("present", caseInsensitive: false));
		Assert.IsTrue(list["present"].IsUndefined());
	}

	[TestMethod]
	public void ReadOnlyObjectWrapperAllowsReadsAndRejectsWrites()
	{
		var engine = CreateEngine();
		var source = DataModelConverter.CreateAsObject();
		source["value"] = "unchanged";
		var list = source.CloneAsReadOnly();
		var wrapper = EcmaScriptHelper.DataModelValueToJsValue(engine.JintEngine, list);
		engine.JintEngine.SetValue("model", wrapper);

		Assert.IsFalse(wrapper.AsObject().Extensible);
		Assert.AreEqual("unchanged", engine.JintEngine.Evaluate("model.value").AsString());
		Assert.ThrowsExactly<InvalidOperationException>(() => engine.JintEngine.Execute("model.value = 'changed'"));
		Assert.AreEqual("unchanged", list["value"].AsString());
	}
}
