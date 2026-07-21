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

using System.Diagnostics.CodeAnalysis;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Xtate.DataTypes;

namespace Xtate.DataModel.EcmaScript.Internal;

public sealed class DataModelObjectWrapper : ObjectInstance, IObjectWrapper
{
    private readonly DataModelList _list;

    private readonly List<JsString> _properties;

    public DataModelObjectWrapper(Engine engine, DataModelList list) : base(engine)
    {
        _list = list;
        _properties = [with(list.Count)];

        foreach (var key in list.Keys)
        {
            TryRegisterProperty((JsString)key, out _);
        }

        if (list.Access != DataModelAccess.Writable)
        {
            PreventExtensions();
        }
    }

#region Interface IObjectWrapper

    public object Target => _list;

#endregion

    private bool TryRegisterProperty(JsString property, [MaybeNullWhen(false)] out PropertyDescriptor propertyDescriptor)
    {
        if (!_properties.Contains(property))
        {
            _properties.Add(property);

            propertyDescriptor = EcmaScriptHelper.CreatePropertyAccessor(Engine, _list, property.ToString());

            SetOwnProperty(property, propertyDescriptor);

            return true;
        }

        propertyDescriptor = null;

        return false;
    }

    private bool TryUnregisterProperty(JsString property)
    {
        if (_properties.Remove(property))
        {
            _list.RemoveFirst(property.AsString(), caseInsensitive: false);

            base.RemoveOwnProperty(property);

            return true;
        }

        return false;
    }

    public override void RemoveOwnProperty(JsValue property)
    {
        if (property is JsString jsProperty)
        {
            TryUnregisterProperty(jsProperty);
        }

        base.RemoveOwnProperty(property);
    }

    public override IEnumerable<KeyValuePair<JsValue, PropertyDescriptor>> GetOwnProperties()
    {
        foreach (var property in _properties)
        {
            yield return new KeyValuePair<JsValue, PropertyDescriptor>(property, GetOwnProperty(property));
        }
    }

    public override PropertyDescriptor GetOwnProperty(JsValue property)
    {
        var descriptor = base.GetOwnProperty(property);

        if (descriptor != PropertyDescriptor.Undefined || !property.IsString())
        {
            return descriptor;
        }

        return TryRegisterProperty((JsString)property, out descriptor) ? descriptor : PropertyDescriptor.Undefined;
    }

    public override bool DefineOwnProperty(JsValue property, PropertyDescriptor descriptor)
    {
        if (property is JsString jsProperty && descriptor.IsDataDescriptor())
        {
            var key = jsProperty.ToString();
            _list[key, caseInsensitive: false] = EcmaScriptHelper.ConvertFromJsValue(descriptor.Value);
            descriptor = EcmaScriptHelper.CreatePropertyAccessor(Engine, _list, key);
        }

        return base.DefineOwnProperty(property, descriptor);
    }
}