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
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Xtate.DataModel.EcmaScript.Properties;
using Xtate.DataModel.Services;
using Xtate.DataTypes;

namespace Xtate.DataModel.EcmaScript.Internal;

internal static class EcmaScriptHelper
{
    private static readonly string[] ParseFormats = [@"o", @"u", @"s", @"r"];

    private static readonly PropertyDescriptor ReadonlyUndefinedPropertyDescriptor = new(JsValue.Undefined, writable: false, enumerable: false, configurable: false);

    public static PropertyDescriptor CreatePropertyAccessor(Engine engine, DataModelList list, string property)
    {
		if (list.Access != DataModelAccess.Writable && !list.ContainsKey(property, caseInsensitive: false))
        {
            return ReadonlyUndefinedPropertyDescriptor;
        }

        var jsGet = JsValue.FromObject(engine, new Func<JsValue>(Getter));
        var jsSet = JsValue.FromObject(engine, new Action<JsValue>(Setter));

        return new GetSetPropertyDescriptor(jsGet, jsSet, enumerable: true, configurable: false);

        JsValue Getter() => ConvertToJsValue(engine, list[property, caseInsensitive: false]);

        void Setter(JsValue value) => list[property, caseInsensitive: false] = ConvertFromJsValue(value);
    }

    public static JsValue ConvertToJsValue(Engine engine, DataModelValue value)
    {
        return value.Type switch
               {
                   DataModelValueType.Undefined => JsValue.Undefined,
                   DataModelValueType.Null      => JsValue.Null,
                   DataModelValueType.Boolean   => value.AsBoolean(),
                   DataModelValueType.String    => value.AsString(),
                   DataModelValueType.Number    => value.AsNumber().ToDouble(),
                   DataModelValueType.DateTime  => value.AsDateTime().ToString(format: @"o", DateTimeFormatInfo.InvariantInfo),
                   DataModelValueType.List      => GetWrapper(engine, value.AsList()),
                   _                            => throw new InvalidOperationException(Resources.Exception_UnsupportedValueType)
               };

        static ObjectInstance GetWrapper(Engine engine, DataModelList list) =>
            DataModelConverter.IsArray(list)
                ? new DataModelArrayWrapper(engine, list)
                : new DataModelObjectWrapper(engine, list);
    }

    public static DataModelValue ConvertFromJsValue(JsValue value) =>
        value.Type switch
        {
            Types.Undefined                  => default,
            Types.Null                       => DataModelValue.Null,
            Types.Boolean                    => new DataModelValue(value.AsBoolean()),
            Types.String                     => CreateDateTimeOrStringValue(value.AsString()),
            Types.Number                     => new DataModelValue(value.AsNumber()),
            Types.Object when value.IsDate() => new DataModelValue(value.AsDate().ToDateTime()),
            Types.Object                     => CreateDataModelValue(value.AsObject()),
            _                                => throw new InvalidOperationException(Resources.Exception_UnsupportedValueType)
        };

    private static DataModelValue CreateDateTimeOrStringValue(string value) =>
        DataModelDateTime.TryParseExact(value, ParseFormats, provider: null, DateTimeStyles.None, out var dateTime)
            ? new DataModelValue(dateTime)
            : new DataModelValue(value);

    private static DataModelValue CreateDataModelValue(ObjectInstance objectInstance)
    {
        if (objectInstance is IObjectWrapper { Target: DataModelList wrappedList })
        {
            return new DataModelValue(wrappedList);
        }

        switch (objectInstance)
        {
            case ArrayInstance array:
            {
                var list = DataModelConverter.CreateAsArray();

                foreach (var (key, _) in array.GetOwnProperties())
                {
                    if (TryGetArrayIndex(key, out var index))
                    {
                        list[index] = ConvertFromJsValue(array.Get(key));
                    }
                }

                return new DataModelValue(list);
            }

            default:
            {
                var list = DataModelConverter.CreateAsObject();

                foreach (var (key, _) in objectInstance.GetOwnProperties())
                {
                    if (key.IsString())
                    {
                        list.Add(key.AsString(), ConvertFromJsValue(objectInstance.Get(key)));
                    }
                }

                return new DataModelValue(list);
            }
        }
    }

    public static bool TryGetArrayIndex(JsValue property, out int index)
    {
        index = 0;

        if (property is JsNumber jsNumber && jsNumber.AsNumber() is var number)
        {
            if (number is >= 0 and <= int.MaxValue && number - Math.Truncate(number) == 0)
            {
                index = (int)number;

                return true;
            }

            return false;
        }

        if (property is JsString jsString && jsString.ToString() is var value)
        {
            return int.TryParse(value, NumberStyles.None, NumberFormatInfo.InvariantInfo, out index) && index >= 0;
        }

        return false;
    }
}
