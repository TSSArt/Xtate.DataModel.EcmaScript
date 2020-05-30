using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Jint;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Xtate.Annotations;

namespace Xtate.DataModel.EcmaScript
{
	[PublicAPI]
	internal static class EcmaScriptHelper
	{
		public const string JintVersionPropertyName = "JintVersion";
		public const string InFunctionName          = "In";

		public static readonly string[] ParseFormats     = { "o", "u", "s", "r" };
		public static readonly string   JintVersionValue = typeof(Engine).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? @"(unknown)";

		private static readonly PropertyDescriptor ReadonlyUndefinedPropertyDescriptor = new PropertyDescriptor(JsValue.Undefined, writable: false, enumerable: false, configurable: false);

		public static PropertyDescriptor CreatePropertyAccessor(Engine engine, DataModelObject obj, string property)
		{
			if (obj.Access != DataModelAccess.Writable && !obj.Contains(property))
			{
				return ReadonlyUndefinedPropertyDescriptor;
			}

			var jsGet = new DelegateWrapper(engine, new Func<JsValue>(Getter));
			var jsSet = new DelegateWrapper(engine, new Action<JsValue>(Setter));

			return new PropertyDescriptor(jsGet, jsSet, enumerable: true, configurable: false);

			JsValue Getter() => ConvertToJsValue(engine, obj[property]);

			void Setter(JsValue value) => obj[property] = ConvertFromJsValue(value);
		}

		public static PropertyDescriptor CreateArrayIndexAccessor(Engine engine, DataModelArray array, int index)
		{
			if (array.Access != DataModelAccess.Writable && index >= array.Length)
			{
				return ReadonlyUndefinedPropertyDescriptor;
			}

			var jsGet = new DelegateWrapper(engine, new Func<JsValue>(Getter));
			var jsSet = new DelegateWrapper(engine, new Action<JsValue>(Setter));

			return new PropertyDescriptor(jsGet, jsSet, enumerable: true, configurable: false);

			JsValue Getter() => ConvertToJsValue(engine, array[index]);

			void Setter(JsValue value) => array[index] = ConvertFromJsValue(value);
		}

		public static JsValue ConvertToJsValue(Engine engine, DataModelValue value)
		{
			return value.Type switch
			{
					DataModelValueType.Undefined => JsValue.Undefined,
					DataModelValueType.Null => JsValue.Null,
					DataModelValueType.Boolean => new JsValue(value.AsBoolean()),
					DataModelValueType.String => new JsValue(value.AsString()),
					DataModelValueType.Number => new JsValue(value.AsNumber()),
					DataModelValueType.DateTime => new JsValue(value.AsDateTime().ToString(format: @"o", DateTimeFormatInfo.InvariantInfo)),
					DataModelValueType.Object => new JsValue(new DataModelObjectWrapper(engine, value.AsObject())),
					DataModelValueType.Array => new JsValue(new DataModelArrayWrapper(engine, value.AsArray())),
					_ => throw new ArgumentOutOfRangeException(nameof(value), value.Type, Resources.Exception_UnsupportedValueType)
			};
		}

		public static DataModelValue ConvertFromJsValue(JsValue value)
		{
			return value.Type switch
			{
					Types.Undefined => default,
					Types.Null => DataModelValue.Null,
					Types.Boolean => new DataModelValue(value.AsBoolean()),
					Types.String => CreateDateTimeOrStringValue(value.AsString()),
					Types.Number => new DataModelValue(value.AsNumber()),
					Types.Object when value.IsDate() => new DataModelValue(value.AsDate().ToDateTime()),
					Types.Object => CreateDataModelValue(value.AsObject()),
					_ => throw new ArgumentOutOfRangeException(nameof(value), value.Type, Resources.Exception_UnsupportedValueType)
			};
		}

		private static DataModelValue CreateDateTimeOrStringValue(string val) =>
				DataModelDateTime.TryParseExact(val, ParseFormats, provider: null, DateTimeStyles.None, out var dateTime)
						? new DataModelValue(dateTime)
						: new DataModelValue(val);

		private static DataModelValue CreateDataModelValue(ObjectInstance objectInstance)
		{
			switch (objectInstance)
			{
				case ArrayInstance array:
					var dataModelArray = new DataModelArray((int) array.GetLength());

					foreach (var pair in array.GetOwnProperties())
					{
						if (ArrayInstance.IsArrayIndex(pair.Key, out var index))
						{
							dataModelArray[(int) index] = ConvertFromJsValue(array.Get(pair.Key));
						}
					}

					return new DataModelValue(dataModelArray);

				default:
				{
					IEnumerable<KeyValuePair<string, PropertyDescriptor>> ownProperties = objectInstance.GetOwnProperties();
					var capacity = ownProperties is ICollection<KeyValuePair<string, PropertyDescriptor>> collection ? collection.Count : 0;

					var dataModelObject = new DataModelObject(capacity);

					foreach (var pair in ownProperties)
					{
						dataModelObject[pair.Key] = ConvertFromJsValue(objectInstance.Get(pair.Key));
					}

					return new DataModelValue(dataModelObject);
				}
			}
		}

		public static object? GetAncestor<T>(in T ancestorProvider) where T : IAncestorProvider => ancestorProvider.Ancestor;
	}
}