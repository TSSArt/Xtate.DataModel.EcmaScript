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
using Xtate.DataTypes;

namespace Xtate.DataModel.EcmaScript.Internal;

public sealed class DataModelArrayWrapper : ObjectInstance, IObjectWrapper
{
    private static readonly JsString Length = new(@"length");

    private readonly DataModelList _list;

    public DataModelArrayWrapper(Engine engine, DataModelList list) : base(engine)
    {
        _list = list;

        Prototype = engine.Intrinsics.Array.PrototypeObject;
        
        if (list.Access != DataModelAccess.Writable)
        {
            PreventExtensions();
        }
    }

#region Interface IObjectWrapper

    public object Target => _list;

#endregion

    public override object ToObject() => _list;

    public override JsValue Get(JsValue property, JsValue receiver)
    {
        if (EcmaScriptHelper.TryGetArrayIndex(property, out var index))
        {
            return index < _list.Count ? EcmaScriptHelper.ConvertToJsValue(Engine, _list[index]) : Undefined;
        }

        if (property == Length)
        {
            return _list.Count;
        }

        return base.Get(property, receiver);
    }

    public override bool HasProperty(JsValue property)
    {
        if (EcmaScriptHelper.TryGetArrayIndex(property, out var index))
        {
            return index < _list.Count;
        }

        return property == Length || base.HasProperty(property);
    }

    public override bool Set(JsValue property, JsValue value, JsValue receiver)
    {
        if (!ReferenceEquals(receiver, this))
        {
            return base.Set(property, value, receiver);
        }

        if (EcmaScriptHelper.TryGetArrayIndex(property, out var index))
        {
            if (_list.Access != DataModelAccess.Writable || !Extensible)
            {
                return false;
            }

            _list[index] = EcmaScriptHelper.ConvertFromJsValue(value);

            return true;
        }

        if (property == Length)
        {
            return SetLength(value);
        }

        return base.Set(property, value, receiver);
    }

    public override bool Delete(JsValue property)
    {
        if (EcmaScriptHelper.TryGetArrayIndex(property, out var index))
        {
            if (index >= _list.Count)
            {
                return true;
            }

            if (_list.Access == DataModelAccess.Writable)
            {
                _list[index] = default;

                return true;
            }

            return false;
        }

        return base.Delete(property);
    }

    public override PropertyDescriptor GetOwnProperty(JsValue property)
    {
        if (EcmaScriptHelper.TryGetArrayIndex(property, out var index))
        {
            return index < _list.Count
                ? new PropertyDescriptor(
                    EcmaScriptHelper.ConvertToJsValue(Engine, _list[index]),
                    writable: _list.Access == DataModelAccess.Writable && Extensible,
                    enumerable: true,
                    configurable: _list.Access == DataModelAccess.Writable && Extensible)
                : PropertyDescriptor.Undefined;
        }

        if (property == Length)
        {
            return new PropertyDescriptor(
                _list.Count,
                writable: _list.Access == DataModelAccess.Writable && Extensible,
                enumerable: false,
                configurable: false);
        }

        return base.GetOwnProperty(property);
    }

    public override bool DefineOwnProperty(JsValue property, PropertyDescriptor descriptor)
    {
        if (EcmaScriptHelper.TryGetArrayIndex(property, out var index))
        {
            if (_list.Access != DataModelAccess.Writable || (index >= _list.Count && !Extensible))
            {
                return false;
            }

            if (descriptor.IsAccessorDescriptor())
            {
                return false;
            }

            if (descriptor.Value is { } descriptorValue)
            {
                if (!Extensible)
                {
                    return false;
                }

                _list[index] = EcmaScriptHelper.ConvertFromJsValue(descriptorValue);
            }

            return true;
        }

        if (property == Length)
        {
            if (descriptor.IsAccessorDescriptor())
            {
                return false;
            }

            return descriptor.Value is not { } descriptorValue || SetLength(descriptorValue);
        }

        return base.DefineOwnProperty(property, descriptor);
    }

    public override IEnumerable<KeyValuePair<JsValue, PropertyDescriptor>> GetOwnProperties()
    {
        for (var index = 0; index < _list.Count; index ++)
        {
            var property = index.ToString(NumberFormatInfo.InvariantInfo);

            yield return new KeyValuePair<JsValue, PropertyDescriptor>(property, GetOwnProperty(property));
        }

        yield return new KeyValuePair<JsValue, PropertyDescriptor>(Length, GetOwnProperty(Length));

        foreach (var property in base.GetOwnProperties())
        {
            if (property.Key != Length && !EcmaScriptHelper.TryGetArrayIndex(property.Key, out _))
            {
                yield return property;
            }
        }
    }

    public override List<JsValue> GetOwnPropertyKeys(Types types = Types.Empty | Types.String | Types.Symbol)
    {
        var keys = new List<JsValue>();

        if ((types & Types.String) != Types.Empty)
        {
            keys.Capacity = _list.Count + 1;

            for (var index = 0; index < _list.Count; index ++)
            {
                keys.Add(index.ToString(NumberFormatInfo.InvariantInfo));
            }

            keys.Add(Length);
        }

        foreach (var key in base.GetOwnPropertyKeys(types))
        {
            if (key != Length && !EcmaScriptHelper.TryGetArrayIndex(key, out _))
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    private bool SetLength(JsValue value)
    {
        if (_list.Access != DataModelAccess.Writable || !Extensible || !value.IsNumber())
        {
            return false;
        }

        var length = value.AsNumber();

        if (length < 0 || length > int.MaxValue || length - Math.Truncate(length) != 0)
        {
            return false;
        }

        _list.SetLength((int)length);

        return true;
    }
}
