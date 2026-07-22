// Copyright © 2019-2026 Sergii Artemenko

using System;
using System.Globalization;
using System.Linq;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xtate.DataModel.EcmaScript.Internal;
using Xtate.DataModel.Services;
using Xtate.DataTypes;

namespace Xtate.DataModel.EcmaScript.Test.UnitTests;

[TestClass]
public class InternalCoverageTest
{
	[TestMethod]
	public void HelperConvertsEverySupportedDataModelValueToJavaScript()
	{
		var engine = new Engine();
		DataModelDateTime date = new DateTimeOffset(2026, 7, 22, 12, 30, 0, TimeSpan.Zero);
		var array = DataModelConverter.CreateAsArray();
		array.Add("item");
		var obj = DataModelConverter.CreateAsObject();
		obj["key"] = "value";

		Assert.IsTrue(EcmaScriptHelper.ConvertToJsValue(engine, default).IsUndefined());
		Assert.IsTrue(EcmaScriptHelper.ConvertToJsValue(engine, DataModelValue.Null).IsNull());
		Assert.IsTrue(EcmaScriptHelper.ConvertToJsValue(engine, true).AsBoolean());
		Assert.AreEqual("text", EcmaScriptHelper.ConvertToJsValue(engine, "text").AsString());
		Assert.AreEqual(12.5, EcmaScriptHelper.ConvertToJsValue(engine, 12.5).AsNumber());
		Assert.AreEqual(date.ToString("o", DateTimeFormatInfo.InvariantInfo), EcmaScriptHelper.ConvertToJsValue(engine, date).AsString());
		Assert.IsInstanceOfType<DataModelArrayWrapper>(EcmaScriptHelper.ConvertToJsValue(engine, array).AsObject());
		Assert.IsInstanceOfType<DataModelObjectWrapper>(EcmaScriptHelper.ConvertToJsValue(engine, obj).AsObject());
	}

	[TestMethod]
	public void HelperConvertsEverySupportedJavaScriptValueToDataModel()
	{
		var engine = new Engine();
		var dateText = "2026-07-22T12:30:00.0000000+00:00";

		Assert.IsTrue(EcmaScriptHelper.ConvertFromJsValue(JsValue.Undefined).IsUndefined());
		Assert.AreEqual(DataModelValueType.Null, EcmaScriptHelper.ConvertFromJsValue(JsValue.Null).Type);
		Assert.IsTrue(EcmaScriptHelper.ConvertFromJsValue(JsValue.FromObject(engine, true)).AsBoolean());
		Assert.AreEqual("ordinary text", EcmaScriptHelper.ConvertFromJsValue("ordinary text").AsString());
		Assert.AreEqual(DataModelValueType.DateTime, EcmaScriptHelper.ConvertFromJsValue(dateText).Type);
		Assert.AreEqual(42.5, EcmaScriptHelper.ConvertFromJsValue(42.5).AsNumber().ToDouble());
		Assert.AreEqual(DataModelValueType.DateTime, EcmaScriptHelper.ConvertFromJsValue(engine.Evaluate("new Date('2026-07-22T12:30:00Z')")).Type);

		var array = EcmaScriptHelper.ConvertFromJsValue(engine.Evaluate("[1,,3]"));
		Assert.AreEqual(3, array.AsList().Count);
		Assert.AreEqual(1, array.AsList()[0].AsNumber().ToInt32());
		Assert.IsTrue(array.AsList()[1].IsUndefined());
		Assert.AreEqual(3, array.AsList()[2].AsNumber().ToInt32());

		var obj = EcmaScriptHelper.ConvertFromJsValue(engine.Evaluate("({ visible: 'yes', get computed() { return 7; } })"));
		Assert.AreEqual("yes", obj.AsList()["visible"].AsString());
		Assert.AreEqual(7, obj.AsList()["computed"].AsNumber().ToInt32());

		var wrapped = DataModelConverter.CreateAsObject();
		wrapped["key"] = "value";
		Assert.AreSame(wrapped, EcmaScriptHelper.ConvertFromJsValue(new DataModelObjectWrapper(engine, wrapped)).AsList());
		Assert.ThrowsExactly<InvalidOperationException>(() => EcmaScriptHelper.ConvertFromJsValue(engine.Evaluate("Symbol('x')")));
	}

	[TestMethod]
	public void HelperRecognizesOnlyCanonicalNonNegativeIntegerIndexes()
	{
		AssertIndex(new JsNumber(0), expected: true, expectedIndex: 0);
		AssertIndex(new JsNumber(int.MaxValue), expected: true, expectedIndex: int.MaxValue);
		AssertIndex(new JsNumber(-1), expected: false, expectedIndex: 0);
		AssertIndex(new JsNumber(1.5), expected: false, expectedIndex: 0);
		AssertIndex(new JsNumber((double)int.MaxValue + 1), expected: false, expectedIndex: 0);
		AssertIndex(new JsString("12"), expected: true, expectedIndex: 12);
		AssertIndex(new JsString("-1"), expected: false, expectedIndex: 0);
		AssertIndex(new JsString("01"), expected: true, expectedIndex: 1);
		AssertIndex(new JsString("x"), expected: false, expectedIndex: 0);
		AssertIndex(JsValue.FromObject(new Engine(), true), expected: false, expectedIndex: 0);

		static void AssertIndex(JsValue value, bool expected, int expectedIndex)
		{
			Assert.AreEqual(expected, EcmaScriptHelper.TryGetArrayIndex(value, out var index));
			Assert.AreEqual(expectedIndex, index);
		}
	}

	[TestMethod]
	public void PropertyAccessorReadsWritesAndProtectsMissingReadonlyProperties()
	{
		var engine = new Engine();
		var list = new DataModelList { ["key"] = "before" };
		var descriptor = EcmaScriptHelper.CreatePropertyAccessor(engine, list, "key");
		engine.Global.DefineOwnProperty("value", descriptor);

		Assert.AreEqual("before", engine.Evaluate("value").AsString());
		engine.Execute("value = 'after'");
		Assert.AreEqual("after", list["key"].AsString());

		var readOnly = list.CloneAsReadOnly();
		var missing = EcmaScriptHelper.CreatePropertyAccessor(engine, readOnly, "missing");
		Assert.IsTrue(missing.Value.IsUndefined());
		Assert.IsFalse(missing.Writable);
		Assert.IsFalse(missing.Enumerable);
		Assert.IsFalse(missing.Configurable);
	}

	[TestMethod]
	public void ArrayWrapperImplementsIndexedLengthAndPrototypeOperations()
	{
		var engine = new Engine();
		var list = DataModelConverter.CreateAsArray();
		list.Add("first");
		list.Add("second");
		var wrapper = new DataModelArrayWrapper(engine, list);
		engine.Global.Set("items", wrapper);

		Assert.AreSame(list, wrapper.Target);
		Assert.AreSame(list, wrapper.ToObject());
		Assert.AreEqual("first", wrapper.Get("0", wrapper).AsString());
		Assert.IsTrue(wrapper.Get("8", wrapper).IsUndefined());
		Assert.AreEqual(2, wrapper.Get("length", wrapper).AsNumber());
		Assert.IsTrue(wrapper.HasProperty("0"));
		Assert.IsFalse(wrapper.HasProperty("8"));
		Assert.IsTrue(wrapper.HasProperty("length"));
		Assert.IsTrue(wrapper.HasProperty("push"));
		Assert.IsTrue(wrapper.Get("push", wrapper).IsCallable());

		Assert.AreEqual(3, engine.Evaluate("items.push('third')").AsNumber());
		Assert.AreEqual("third", list[2].AsString());
		Assert.AreEqual("first,second,third", engine.Evaluate("items.join(',')").AsString());
		Assert.IsTrue(wrapper.Set("1", "changed", wrapper));
		Assert.IsTrue(wrapper.Set("custom", "property", wrapper));
		Assert.AreEqual("property", wrapper.Get("custom", wrapper).AsString());
		Assert.AreEqual("changed", list[1].AsString());
		Assert.IsTrue(wrapper.Set("length", 4, wrapper));
		Assert.AreEqual(4, list.Count);
		Assert.IsTrue(wrapper.Delete("3"));
		Assert.IsTrue(list[3].IsUndefined());
		Assert.IsTrue(wrapper.Delete("99"));
	}

	[TestMethod]
	public void ArrayWrapperDescriptorsEnumerationAndKeysReflectTheBackingList()
	{
		var engine = new Engine();
		var list = DataModelConverter.CreateAsArray();
		list.Add("value");
		var wrapper = new DataModelArrayWrapper(engine, list);
		wrapper.CreateDataProperty("extra", 17);

		var item = wrapper.GetOwnProperty("0");
		Assert.AreEqual("value", item.Value.AsString());
		Assert.IsTrue(item.Writable);
		Assert.IsTrue(item.Enumerable);
		Assert.IsTrue(item.Configurable);
		Assert.AreSame(PropertyDescriptor.Undefined, wrapper.GetOwnProperty("9"));
		var length = wrapper.GetOwnProperty("length");
		Assert.AreEqual(1, length.Value.AsNumber());
		Assert.IsFalse(length.Enumerable);
		Assert.IsFalse(length.Configurable);
		Assert.AreEqual(17, wrapper.GetOwnProperty("extra").Value.AsNumber());

		var properties = wrapper.GetOwnProperties().ToArray();
		CollectionAssert.AreEqual(new[] { "0", "length", "extra" }, properties.Select(static pair => pair.Key.ToString()).ToArray());
		var keys = wrapper.GetOwnPropertyKeys(Types.String);
		CollectionAssert.AreEqual(new[] { "0", "length", "extra" }, keys.Select(static key => key.ToString()).ToArray());
		Assert.AreEqual(0, wrapper.GetOwnPropertyKeys(Types.Symbol).Count);
	}

	[TestMethod]
	public void ArrayWrapperDefineDeleteAndInvalidLengthsCoverMutationRules()
	{
		var engine = new Engine();
		var list = DataModelConverter.CreateAsArray();
		list.Add("value");
		var wrapper = new DataModelArrayWrapper(engine, list);
		var data = new PropertyDescriptor("new", writable: true, enumerable: true, configurable: true);
		var accessor = new GetSetPropertyDescriptor(JsValue.Undefined, JsValue.Undefined, enumerable: true, configurable: true);

		Assert.IsTrue(wrapper.DefineOwnProperty("1", data));
		Assert.AreEqual("new", list[1].AsString());
		Assert.IsFalse(wrapper.DefineOwnProperty("2", accessor));
		Assert.IsTrue(wrapper.DefineOwnProperty("1", new PropertyDescriptor()));
		Assert.IsTrue(wrapper.DefineOwnProperty("length", new PropertyDescriptor()));
		Assert.IsFalse(wrapper.DefineOwnProperty("length", accessor));
		Assert.IsFalse(wrapper.Set("length", "2", wrapper));
		Assert.IsFalse(wrapper.Set("length", -1, wrapper));
		Assert.IsFalse(wrapper.Set("length", 1.5, wrapper));
		Assert.IsFalse(wrapper.Set("length", (double)int.MaxValue + 1, wrapper));
		Assert.IsTrue(wrapper.DefineOwnProperty("length", new PropertyDescriptor(1, writable: true, enumerable: false, configurable: false)));
		Assert.AreEqual(1, list.Count);

		var receiver = engine.Evaluate("({})").AsObject();
		Assert.IsTrue(wrapper.Set("own", 5, receiver));
		Assert.AreEqual(5, receiver.Get("own").AsNumber());
		Assert.IsTrue(wrapper.Delete("own"));
	}

	[TestMethod]
	public void ReadonlyArrayWrapperRejectsAllBackingStoreMutations()
	{
		var engine = new Engine();
		var writable = DataModelConverter.CreateAsArray();
		writable.Add("value");
		var list = writable.CloneAsReadOnly();
		var wrapper = new DataModelArrayWrapper(engine, list);
		var descriptor = new PropertyDescriptor("new", writable: true, enumerable: true, configurable: true);

		Assert.IsFalse(wrapper.Extensible);
		Assert.IsFalse(wrapper.Set("0", "new", wrapper));
		Assert.IsFalse(wrapper.Set("length", 0, wrapper));
		Assert.IsFalse(wrapper.Delete("0"));
		Assert.IsFalse(wrapper.DefineOwnProperty("0", descriptor));
		Assert.IsFalse(wrapper.DefineOwnProperty("1", descriptor));
		Assert.IsFalse(wrapper.GetOwnProperty("0").Writable);
		Assert.IsFalse(wrapper.GetOwnProperty("length").Writable);
	}

	[TestMethod]
	public void NonExtensibleWritableArrayCoversDescriptorRestrictions()
	{
		var engine = new Engine();
		var list = DataModelConverter.CreateAsArray();
		list.Add("value");
		var wrapper = new DataModelArrayWrapper(engine, list);
		wrapper.PreventExtensions();

		Assert.IsFalse(wrapper.Set("0", "new", wrapper));
		Assert.IsFalse(wrapper.Set("length", 2, wrapper));
		Assert.IsFalse(wrapper.DefineOwnProperty("1", new PropertyDescriptor("new", true, true, true)));
		Assert.IsFalse(wrapper.DefineOwnProperty("0", new PropertyDescriptor("new", true, true, true)));
		Assert.IsTrue(wrapper.DefineOwnProperty("0", new PropertyDescriptor()));
		Assert.IsTrue(wrapper.Delete("0"));
	}

	[TestMethod]
	public void ObjectWrapperSynchronizesPropertiesWithTheBackingList()
	{
		var engine = new Engine();
		var list = DataModelConverter.CreateAsObject();
		list["existing"] = "before";
		var wrapper = new DataModelObjectWrapper(engine, list);
		engine.Global.Set("model", wrapper);

		Assert.AreSame(list, wrapper.Target);
		Assert.AreEqual("before", engine.Evaluate("model.existing").AsString());
		engine.Execute("model.existing = 'after'; model.created = 42;");
		Assert.AreEqual("after", list["existing"].AsString());
		Assert.AreEqual(42, list["created"].AsNumber().ToInt32());
		Assert.AreEqual(2, wrapper.GetOwnProperties().Count());
		CollectionAssert.AreEquivalent(new[] { "existing", "created" }, wrapper.GetOwnProperties().Select(static p => p.Key.ToString()).ToArray());

		wrapper.RemoveOwnProperty("existing");
		Assert.IsFalse(list.ContainsKey("existing", caseInsensitive: false));
		wrapper.RemoveOwnProperty(new JsNumber(1));
		wrapper.RemoveOwnProperty("missing");
		Assert.AreSame(PropertyDescriptor.Undefined, wrapper.GetOwnProperty(JsValue.FromObject(engine, true)));
		Assert.IsInstanceOfType<GetSetPropertyDescriptor>(wrapper.GetOwnProperty("missing"));
		Assert.IsTrue(wrapper.Get("missing", wrapper).IsUndefined());
		Assert.IsFalse(wrapper.DefineOwnProperty("defined", new PropertyDescriptor("direct", writable: true, enumerable: true, configurable: true)));
		Assert.AreEqual("direct", list["defined"].AsString());
		Assert.IsTrue(wrapper.DefineOwnProperty(new JsNumber(1), new PropertyDescriptor("number", writable: true, enumerable: true, configurable: true)));
	}

	[TestMethod]
	public void ObjectWrapperHandlesDuplicateBackingKeys()
	{
		var list = DataModelConverter.CreateAsObject();
		list.Add("duplicate", "first");
		list.Add("duplicate", "second");

		var wrapper = new DataModelObjectWrapper(new Engine(), list);

		Assert.HasCount(1, wrapper.GetOwnProperties());
	}

	[TestMethod]
	public void ResourceFormattingHelpersCoverEveryArity()
	{
		Assert.AreEqual("one", Res.Format("{0}", "one"));
		Assert.AreEqual("one-two", Res.Format("{0}-{1}", "one", "two"));
		Assert.AreEqual("one-two-three", Res.Format("{0}-{1}-{2}", "one", "two", "three"));
	}

	[TestMethod]
	public void ReadonlyObjectWrapperExposesExistingAndUndefinedMissingProperties()
	{
		var engine = new Engine();
		var source = DataModelConverter.CreateAsObject();
		source["existing"] = "value";
		var list = source.CloneAsReadOnly();
		var wrapper = new DataModelObjectWrapper(engine, list);

		Assert.IsFalse(wrapper.Extensible);
		Assert.AreEqual("value", wrapper.Get("existing", wrapper).AsString());
		var missing = wrapper.GetOwnProperty("missing");
		Assert.IsTrue(missing.Value.IsUndefined());
		Assert.IsFalse(missing.Writable);
	}
}
