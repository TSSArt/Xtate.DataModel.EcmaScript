#region Copyright © 2019-2021 Sergii Artemenko

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

#endregion

using System;
using System.Globalization;
using Jint;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Xtate.Core;

namespace Xtate.DataModel.EcmaScript
{
	[PublicAPI]
	internal static class EcmaScriptHelper
	{
		public const string JintVersionPropertyName = "JintVersion";
		public const string InFunctionName          = "In";

		public static readonly string[] ParseFormats     = { @"o", @"u", @"s", @"r" };
		public static readonly string   JintVersionValue = TypeInfo<Engine>.Instance.AssemblyVersion;

		private static readonly PropertyDescriptor ReadonlyUndefinedPropertyDescriptor = new(JsValue.Undefined, writable: false, enumerable: false, configurable: false);

		public static PropertyDescriptor CreatePropertyAccessor(Engine engine, DataModelList list, string property)
		{
			if (list.Access != DataModelAccess.Writable && !list.ContainsKey(property, caseInsensitive: false))
			{
				return ReadonlyUndefinedPropertyDescriptor;
			}

			var jsGet = new DelegateWrapper(engine, new Func<JsValue>(Getter));
			var jsSet = new DelegateWrapper(engine, new Action<JsValue>(Setter));

			return new PropertyDescriptor(jsGet, jsSet, enumerable: true, configurable: false);

			JsValue Getter() => ConvertToJsValue(engine, list[property, caseInsensitive: false]);

			void Setter(JsValue value) => list[property, caseInsensitive: false] = ConvertFromJsValue(value);
		}

		public static PropertyDescriptor CreateArrayIndexAccessor(Engine engine, DataModelList list, int index)
		{
			if (list.Access != DataModelAccess.Writable && index >= list.Count)
			{
				return ReadonlyUndefinedPropertyDescriptor;
			}

			var jsGet = new DelegateWrapper(engine, new Func<JsValue>(Getter));
			var jsSet = new DelegateWrapper(engine, new Action<JsValue>(Setter));

			return new PropertyDescriptor(jsGet, jsSet, enumerable: true, configurable: false);

			JsValue Getter() => ConvertToJsValue(engine, list[index]);

			void Setter(JsValue value) => list[index] = ConvertFromJsValue(value);
		}

		public static JsValue ConvertToJsValue(Engine engine, DataModelValue value)
		{
			static ObjectInstance GetWrapper(Engine engine, DataModelList list) =>
				DataModelConverter.IsArray(list)
					? new DataModelArrayWrapper(engine, list)
					: new DataModelObjectWrapper(engine, list);

			return value.Type switch
				   {
					   DataModelValueType.Undefined => JsValue.Undefined,
					   DataModelValueType.Null      => JsValue.Null,
					   DataModelValueType.Boolean   => new JsValue(value.AsBoolean()),
					   DataModelValueType.String    => new JsValue(value.AsString()),
					   DataModelValueType.Number    => new JsValue(value.AsNumber()),
					   DataModelValueType.DateTime  => new JsValue(value.AsDateTime().ToString(format: @"o", DateTimeFormatInfo.InvariantInfo)),
					   DataModelValueType.List      => new JsValue(GetWrapper(engine, value.AsList())),
					   _                            => Infrastructure.UnexpectedValue<JsValue>(value.Type, Resources.Exception_UnsupportedValueType)
				   };
		}

		public static DataModelValue ConvertFromJsValue(JsValue value)
		{
			return value.Type switch
				   {
					   Types.Undefined                  => default,
					   Types.Null                       => DataModelValue.Null,
					   Types.Boolean                    => new DataModelValue(value.AsBoolean()),
					   Types.String                     => CreateDateTimeOrStringValue(value.AsString()),
					   Types.Number                     => new DataModelValue(value.AsNumber()),
					   Types.Object when value.IsDate() => new DataModelValue(value.AsDate().ToDateTime()),
					   Types.Object                     => CreateDataModelValue(value.AsObject()),
					   _                                => Infrastructure.UnexpectedValue<DataModelValue>(value.Type, Resources.Exception_UnsupportedValueType)
				   };
		}

		private static DataModelValue CreateDateTimeOrStringValue(string value) =>
			DataModelDateTime.TryParseExact(value, ParseFormats, provider: null, DateTimeStyles.None, out var dateTime)
				? new DataModelValue(dateTime)
				: new DataModelValue(value);

		private static DataModelValue CreateDataModelValue(ObjectInstance objectInstance)
		{
			switch (objectInstance)
			{
				case ArrayInstance array:
				{
					var list = DataModelConverter.CreateAsArray();

					foreach (var pair in array.GetOwnProperties())
					{
						if (ArrayInstance.IsArrayIndex(pair.Key, out var index))
						{
							list[(int) index] = ConvertFromJsValue(array.Get(pair.Key));
						}
					}

					return new DataModelValue(list);
				}

				default:
				{
					var list = DataModelConverter.CreateAsObject();

					foreach (var pair in objectInstance.GetOwnProperties())
					{
						list.Add(pair.Key, ConvertFromJsValue(objectInstance.Get(pair.Key)));
					}

					return new DataModelValue(list);
				}
			}
		}
	}
}