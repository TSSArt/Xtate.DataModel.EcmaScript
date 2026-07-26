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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jint;
using Jint.Native;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xtate.DataModel.EcmaScript.Internal;
using Xtate.DataModel.Services;
using Xtate.DataTypes;

namespace Xtate.DataModel.EcmaScript.Test.UnitTests;

[TestClass]
public class InternalCoverageTest
{
	[TestMethod]
	public void HelperConvertsEverySupportedJavaScriptValueToDataModel()
	{
		var engine = new Engine();

		Assert.IsTrue(EcmaScriptHelper.JsValueToDataModelValue(JsValue.Undefined).IsUndefined());
		Assert.AreEqual(DataModelValueType.Null, EcmaScriptHelper.JsValueToDataModelValue(JsValue.Null).Type);
		Assert.IsTrue(EcmaScriptHelper.JsValueToDataModelValue(JsValue.FromObject(engine, value: true)).AsBoolean());
		Assert.AreEqual(expected: "ordinary text", EcmaScriptHelper.JsValueToDataModelValue("ordinary text").AsString());
		Assert.AreEqual(expected: 42.5, EcmaScriptHelper.JsValueToDataModelValue(42.5).AsNumber().ToDouble());
		Assert.AreEqual(DataModelValueType.DateTime, EcmaScriptHelper.JsValueToDataModelValue(engine.Evaluate("new Date('2026-07-22T12:30:00Z')")).Type);

		var array = EcmaScriptHelper.JsValueToDataModelValue(engine.Evaluate("[1,,3]"));
		Assert.AreEqual(expected: 3, array.AsList().Count);
		Assert.AreEqual(expected: 1, array.AsList()[0].AsNumber().ToInt32());
		Assert.IsTrue(array.AsList()[1].IsUndefined());
		Assert.AreEqual(expected: 3, array.AsList()[2].AsNumber().ToInt32());

		var obj = EcmaScriptHelper.JsValueToDataModelValue(engine.Evaluate("({ visible: 'yes', get computed() { return 7; } })"));
		Assert.AreEqual(expected: "yes", obj.AsList()["visible"].AsString());
		Assert.AreEqual(expected: 7, obj.AsList()["computed"].AsNumber().ToInt32());

		var wrapped = DataModelConverter.CreateAsObject();
		wrapped["key"] = "value";
		Assert.AreSame(wrapped, EcmaScriptHelper.JsValueToDataModelValue(new DataModelObjectWrapper(engine, wrapped)).AsList());
		Assert.ThrowsExactly<InvalidOperationException>(() => EcmaScriptHelper.JsValueToDataModelValue(engine.Evaluate("Symbol('x')")));
	}

	[TestMethod]
	public void HelperRecognizesOnlyCanonicalNonNegativeIntegerIndexes()
	{
		AssertIndex(new JsNumber(0), expected: true, expectedIndex: 0);
		AssertIndex(new JsNumber(int.MaxValue), expected: true, int.MaxValue);
		AssertIndex(new JsNumber(-1), expected: false, expectedIndex: 0);
		AssertIndex(new JsNumber(1.5), expected: false, expectedIndex: 0);
		AssertIndex(new JsNumber((double)int.MaxValue + 1), expected: false, expectedIndex: 0);
		AssertIndex(new JsString("12"), expected: true, expectedIndex: 12);
		AssertIndex(new JsString("-1"), expected: false, expectedIndex: 0);
		AssertIndex(new JsString("01"), expected: true, expectedIndex: 1);
		AssertIndex(new JsString("x"), expected: false, expectedIndex: 0);
		AssertIndex(JsValue.FromObject(new Engine(), value: true), expected: false, expectedIndex: 0);

		static void AssertIndex(JsValue value, bool expected, int expectedIndex)
		{
			var result = EcmaScriptHelper.IsArrayIndex(value, out var index);

			Assert.AreEqual(expected, result);
			Assert.AreEqual(expectedIndex, (int)index);
		}
	}

	[TestMethod]
	public void ObjectWrapperSynchronizesPropertiesWithTheBackingList()
	{
		var engine = new Engine();
		var list = DataModelConverter.CreateAsObject();
		list["existing"] = "before";
		var wrapper = new DataModelObjectWrapper(engine, list);
		engine.Global.Set(property: "model", wrapper);

		Assert.AreSame(list, wrapper.Target);
		Assert.AreEqual(expected: "before", engine.Evaluate("model.existing").AsString());
		engine.Execute("model.existing = 'after'; model.created = 42;");
		Assert.AreEqual(expected: "after", list["existing"].AsString());
		Assert.AreEqual(expected: 42, list["created"].AsNumber().ToInt32());
		Assert.AreEqual(expected: 2, wrapper.GetOwnProperties().Count());
		CollectionAssert.AreEquivalent(new[] { "existing", "created" }, wrapper.GetOwnProperties().Select(static p => p.Key.ToString()).ToArray());

		wrapper.RemoveOwnProperty("existing");
		Assert.IsFalse(list.ContainsKey(key: "existing", caseInsensitive: false));
		wrapper.RemoveOwnProperty(new JsNumber(1));
		wrapper.RemoveOwnProperty("missing");
		Assert.AreSame(PropertyDescriptor.Undefined, wrapper.GetOwnProperty(JsValue.FromObject(engine, value: true)));
		Assert.IsTrue(wrapper.Get(property: "missing", wrapper).IsUndefined());
		Assert.IsTrue(wrapper.DefineOwnProperty(new JsNumber(1), new PropertyDescriptor(value: "number", writable: true, enumerable: true, configurable: true)));
	}

	[TestMethod]
	public void ObjectWrapperHandlesDuplicateBackingKeys()
	{
		var list = DataModelConverter.CreateAsObject();
		list.Add(key: "duplicate", value: "first");
		list.Add(key: "duplicate", value: "second");

		var wrapper = new DataModelObjectWrapper(new Engine(), list);

		Assert.HasCount(expected: 1, wrapper.GetOwnProperties());
	}

	[TestMethod]
	public void ResourceFormattingHelpersCoverEveryArity()
	{
		Assert.AreEqual(expected: "one", Res.Format(format: "{0}", arg: "one"));
		Assert.AreEqual(expected: "one-two", Res.Format(format: "{0}-{1}", arg0: "one", arg1: "two"));
		Assert.AreEqual(expected: "one-two-three", Res.Format(format: "{0}-{1}-{2}", arg0: "one", arg1: "two", arg2: "three"));
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
		Assert.AreEqual(expected: "value", wrapper.Get(property: "existing", wrapper).AsString());
		var missing = wrapper.GetOwnProperty("missing");
		Assert.IsExactInstanceOfType<ProxyPropertyDescriptor>(missing);
		Assert.IsFalse(missing.Writable);
	}

	[TestMethod]
	public void ListWrapperImplementsCollectionAndListOperations()
	{
		var engine = new Engine();
		var list = DataModelConverter.CreateAsArray();
		list.Add("first");
		list.Add("second");
		IList<JsValue?> wrapper = new DataModelListWrapper(engine, list);

		Assert.IsTrue(wrapper.Contains("first"));
		Assert.IsFalse(wrapper.Contains("missing"));
		Assert.AreEqual(expected: 1, wrapper.IndexOf("second"));
		Assert.IsFalse(wrapper.IsReadOnly);

		var destination = new JsValue?[4];
		wrapper.CopyTo(destination, arrayIndex: 1);
		Assert.AreEqual(expected: "first", destination[1]!.AsString());
		Assert.AreEqual(expected: "second", destination[2]!.AsString());

		var enumerator = ((IEnumerable)wrapper).GetEnumerator();
		Assert.IsTrue(enumerator.MoveNext());
		Assert.AreEqual(expected: "first", ((JsValue)enumerator.Current).AsString());
		Assert.IsTrue(enumerator.MoveNext());
		Assert.AreEqual(expected: "second", ((JsValue)enumerator.Current).AsString());
		Assert.IsFalse(enumerator.MoveNext());

		wrapper.Insert(index: 1, "inserted");
		Assert.AreEqual(expected: "inserted", list[1].AsString());
		Assert.IsTrue(wrapper.Remove("inserted"));
		Assert.IsFalse(wrapper.Remove("missing"));
		wrapper.RemoveAt(index: 1);
		Assert.HasCount(expected: 1, list);

		var readOnlyWrapper = new DataModelListWrapper(engine, list.CloneAsReadOnly());
		Assert.IsTrue(readOnlyWrapper.IsReadOnly);

		wrapper.Clear();
		Assert.HasCount(expected: 0, list);
	}

	[TestMethod]
	public void ObjectWrapperRefreshesStaleAndNonProxyProperties()
	{
		var engine = new Engine();
		var list = DataModelConverter.CreateAsObject();
		list["stale"] = "value";
		var wrapper = new TestDataModelObjectWrapper(engine, list);

		Assert.IsExactInstanceOfType<ProxyPropertyDescriptor>(wrapper.GetOwnProperty("stale"));
		list.RemoveFirst("stale", caseInsensitive: false);
		Assert.AreSame(PropertyDescriptor.Undefined, wrapper.GetOwnProperty("stale"));

		wrapper.SetOwn(
			"data",
			new PropertyDescriptor(value: "stored", writable: true, enumerable: true, configurable: true));
		Assert.AreEqual(expected: "stored", list["data"].AsString());

		wrapper.SetOwn(
			"accessor",
			new GetSetPropertyDescriptor(JsValue.Undefined, JsValue.Undefined, enumerable: true, configurable: true));
		Assert.IsFalse(list.ContainsKey(key: "accessor", caseInsensitive: false));

		wrapper.SetOwn(
			engine.Evaluate("Symbol('numeric')"),
			new PropertyDescriptor(value: "numeric", writable: true, enumerable: true, configurable: true));
		var properties = wrapper.GetOwnProperties().ToArray();
		Assert.IsTrue(properties.Any(static property => property.Key.IsSymbol()));
	}

	[TestMethod]
	public void HelperCoversDatesForeignWrappersAndWrapperRoundTrips()
	{
		var engine = new Engine();
		DataModelDateTime date = new DateTimeOffset(2026, 7, 26, 12, 30, 0, TimeSpan.Zero);

		Assert.IsTrue(EcmaScriptHelper.DataModelValueToJsValue(engine, date).IsDate());

		var foreignWrapper = JsValue.FromObject(engine, new object());
		var convertedForeignWrapper = EcmaScriptHelper.JsValueToDataModelValue(foreignWrapper);
		Assert.IsTrue(DataModelConverter.IsObject(convertedForeignWrapper.AsList()));

		var list = DataModelConverter.CreateAsObject();
		list["value"] = 42;
		var jsWrapper = EcmaScriptHelper.DataModelValueToJsValue(engine, list);
		var wrappedValue = EcmaScriptHelper.JsValueToDataModelValue(jsWrapper);
		Assert.AreSame(jsWrapper.AsObject(), EcmaScriptHelper.DataModelValueToJsValue(engine, wrappedValue).AsObject());
	}

	[TestMethod]
	public void ContractAnnotationsExposeBothConstructorForms()
	{
		var concise = new ContractAnnotationAttribute("null => null");
		var full = new ContractAnnotationAttribute("notnull => notnull", forceFullStates: true);

		Assert.AreEqual(expected: "null => null", concise.Contract);
		Assert.IsFalse(concise.ForceFullStates);
		Assert.AreEqual(expected: "notnull => notnull", full.Contract);
		Assert.IsTrue(full.ForceFullStates);
	}

	[TestMethod]
	[DoNotParallelize]
	public void HelperRejectsAnUnknownDataModelValueType()
	{
		if (Environment.OSVersion.Platform != PlatformID.Win32NT || !Environment.Is64BitProcess)
		{
			Assert.Inconclusive("The test-only native method patch requires a 64-bit Windows process.");
		}

		var typeGetter = typeof(DataModelValue).GetProperty(nameof(DataModelValue.Type))!.GetMethod!;

		using (NativeMethodPatch.ReturnInt32(typeGetter, int.MaxValue))
		{
			Assert.ThrowsExactly<InvalidOperationException>(() => EcmaScriptHelper.DataModelValueToJsValue(new Engine(), default));
		}

		Assert.AreEqual(DataModelValueType.Undefined, default(DataModelValue).Type);
	}

	private sealed class TestDataModelObjectWrapper(Engine engine, DataModelList list) : DataModelObjectWrapper(engine, list)
	{
		public void SetOwn(JsValue property, PropertyDescriptor descriptor) => SetOwnProperty(property, descriptor);
	}

	private sealed class NativeMethodPatch : IDisposable
	{
		private const uint PageExecuteReadWrite = 0x40;

		private readonly IntPtr _address;

		private readonly byte[] _originalCode;

		private NativeMethodPatch(MethodInfo method, int returnValue)
		{
			RuntimeHelpers.PrepareMethod(method.MethodHandle);
			_address = FollowJumpStubs(method.MethodHandle.GetFunctionPointer());
			_originalCode = new byte[6];
			Marshal.Copy(_address, _originalCode, startIndex: 0, _originalCode.Length);

			var replacementCode = new byte[]
								  {
									  0xB8,
									  (byte)returnValue,
									  (byte)(returnValue >> 8),
									  (byte)(returnValue >> 16),
									  (byte)(returnValue >> 24),
									  0xC3
								  };

			WriteCode(replacementCode);
		}

		public static NativeMethodPatch ReturnInt32(MethodInfo method, int returnValue) => new(method, returnValue);

		public void Dispose()
		{
			WriteCode(_originalCode);
		}

		private void WriteCode(byte[] code)
		{
			if (!VirtualProtect(_address, new UIntPtr((uint)code.Length), PageExecuteReadWrite, out var oldProtection))
			{
				throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
			}

			try
			{
				Marshal.Copy(code, startIndex: 0, _address, code.Length);

				if (!FlushInstructionCache(GetCurrentProcess(), _address, new UIntPtr((uint)code.Length)))
				{
					throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
				}
			}
			finally
			{
				_ = VirtualProtect(_address, new UIntPtr((uint)code.Length), oldProtection, out _);
			}
		}

		private static IntPtr FollowJumpStubs(IntPtr address)
		{
			for (var i = 0; i < 4; i ++)
			{
				if (Marshal.ReadByte(address) == 0xFF && Marshal.ReadByte(address, ofs: 1) == 0x25)
				{
					var pointerAddress = IntPtr.Add(address, 6 + Marshal.ReadInt32(address, ofs: 2));
					address = Marshal.ReadIntPtr(pointerAddress);

					continue;
				}

				if (Marshal.ReadByte(address) == 0xE9)
				{
					address = IntPtr.Add(address, 5 + Marshal.ReadInt32(address, ofs: 1));

					continue;
				}

				break;
			}

			return address;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool VirtualProtect(IntPtr address, UIntPtr size, uint newProtection, out uint oldProtection);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool FlushInstructionCache(IntPtr process, IntPtr address, UIntPtr size);

		[DllImport("kernel32.dll")]
		private static extern IntPtr GetCurrentProcess();
	}
}
