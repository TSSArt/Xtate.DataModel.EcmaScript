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

using System.Globalization;
using Jint.Runtime;
using Jint.Runtime.Interop;
using Xtate.DataModel.Services;
using Xtate.DataTypes;

namespace Xtate.DataModel.EcmaScript.Internal;

internal static class EcmaScriptHelper
{
	public static JsValue DataModelValueToJsValue(Engine engine, DataModelValue value)
	{
		if (value.TryGetAs<WrapperContainer>(out var wrapper))
		{
			return wrapper.ObjectInstance;
		}

		return value.Type switch
			   {
				   DataModelValueType.Undefined => JsValue.Undefined,
				   DataModelValueType.Null      => JsValue.Null,
				   DataModelValueType.Boolean   => value.AsBoolean(),
				   DataModelValueType.String    => value.AsString(),
				   DataModelValueType.Number    => value.AsNumber().ToDouble(),
				   DataModelValueType.DateTime  => new JsDate(engine, value.AsDateTime().ToDateTime()),
				   DataModelValueType.List      => GetWrapper(engine, value.AsList()),
				   _                            => throw new InvalidOperationException(Resources.Exception_UnsupportedValueType)
			   };

		static ObjectInstance GetWrapper(Engine engine, DataModelList list) =>
			DataModelConverter.IsArray(list) ? ObjectWrapper.Create(engine, new DataModelListWrapper(engine, list)) : new DataModelObjectWrapper(engine, list);
	}

	public static DataModelValue JsValueToDataModelValue(JsValue jsValue) =>
		jsValue.Type switch
		{
			Types.Undefined                    => DataModelValue.Undefined,
			Types.Null                         => DataModelValue.Null,
			Types.Boolean                      => jsValue.AsBoolean(),
			Types.String                       => jsValue.ToString(),
			Types.Number                       => jsValue.AsNumber(),
			Types.Object when jsValue.IsDate() => jsValue.AsDate().ToDateTime(),
			Types.Object                       => ObjectInstanceToDataModelValue(jsValue.AsObject()),
			_                                  => throw new InvalidOperationException(Resources.Exception_UnsupportedValueType)
		};

	private static DataModelValue ObjectInstanceToDataModelValue(ObjectInstance objectInstance)
	{
		if (objectInstance is IObjectWrapper { Target: { } target })
		{
			switch (target)
			{
				case DataModelList list:
					return new DataModelValue(new WrapperContainer(objectInstance, list));

				case DataModelListWrapper { List: var list }:
					return new DataModelValue(new WrapperContainer(objectInstance, list));
			}
		}

		switch (objectInstance)
		{
			case ArrayInstance array:
			{
				var list = DataModelConverter.CreateAsArray();

				foreach (var (key, _) in array.GetOwnProperties())
				{
					if (IsArrayIndex(key, out var index))
					{
						list[index] = JsValueToDataModelValue(array.Get(key));
					}
				}

				return list;
			}
			default:
			{
				var list = DataModelConverter.CreateAsObject();

				foreach (var (key, _) in objectInstance.GetOwnProperties())
				{
					list.Add(key.ToString(), JsValueToDataModelValue(objectInstance.Get(key)));
				}

				return list;
			}
		}
	}

	public static bool IsArrayIndex(JsValue val, out int index)
	{
		if (val.IsNumber())
		{
			var value = val.AsNumber();

			if (value is >= 0 and <= int.MaxValue && value - Math.Truncate(value) == 0)
			{
				index = (int)value;

				return true;
			}

			index = 0;

			return false;
		}

		if (!val.IsSymbol() && int.TryParse(val.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var parsedIndex) && parsedIndex >= 0)
		{
			index = parsedIndex;

			return true;
		}

		index = 0;

		return false;
	}

	private class WrapperContainer(ObjectInstance objectInstance, DataModelList list) : ILazyValue
	{
		public readonly ObjectInstance ObjectInstance = objectInstance;

	#region Interface ILazyValue

		public DataModelValue Value => list;

	#endregion
	}
}