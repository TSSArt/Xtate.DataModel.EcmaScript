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

using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Xtate.DataTypes;

namespace Xtate.DataModel.EcmaScript.Internal;

internal class DataModelObjectWrapper(Engine engine, DataModelList list) : ObjectInstance(engine), IObjectWrapper
{
	public override bool Extensible => list.Access == DataModelAccess.Writable && base.Extensible;

#region Interface IObjectWrapper

	public object Target => list;

#endregion

	public override void RemoveOwnProperty(JsValue property)
	{
		if (property.IsString())
		{
			list.RemoveFirst(property.ToString(), caseInsensitive: false);
		}

		base.RemoveOwnProperty(property);
	}

	public override IEnumerable<KeyValuePair<JsValue, PropertyDescriptor>> GetOwnProperties()
	{
		var keys = new HashSet<string>(list.Keys);
		List<JsValue>? toRemove = null;

		foreach (var (property, descriptor) in base.GetOwnProperties())
		{
			if (!property.IsString())
			{
				yield return new KeyValuePair<JsValue, PropertyDescriptor>(property, descriptor);
			}

			if (descriptor is ProxyPropertyDescriptor { HasUnderlyingKey: true } propertyDescriptor)
			{
				keys.Remove(property.ToString());

				yield return new KeyValuePair<JsValue, PropertyDescriptor>(property, propertyDescriptor);
			}
			else
			{
				(toRemove ??= []).Add(property);
			}
		}

		if (toRemove is not null)
		{
			foreach (var property in toRemove)
			{
				base.RemoveOwnProperty(property);
			}
		}

		foreach (var key in keys)
		{
			var property = (JsValue)key;
			var descriptor = new ProxyPropertyDescriptor(Engine, list, key);

			base.SetOwnProperty(property, descriptor);

			yield return new KeyValuePair<JsValue, PropertyDescriptor>(property, descriptor);
		}
	}

	public override PropertyDescriptor GetOwnProperty(JsValue property)
	{
		var descriptor = base.GetOwnProperty(property);

		if (!property.IsString())
		{
			return descriptor;
		}

		if (descriptor is ProxyPropertyDescriptor propertyDescriptor)
		{
			if (propertyDescriptor.HasUnderlyingKey)
			{
				return propertyDescriptor;
			}

			base.RemoveOwnProperty(property);

			return PropertyDescriptor.Undefined;
		}

		var key = property.ToString();

		propertyDescriptor = new ProxyPropertyDescriptor(Engine, list, key);

		base.SetOwnProperty(property, propertyDescriptor);

		return propertyDescriptor;
	}

	protected override void SetOwnProperty(JsValue property, PropertyDescriptor descriptor)
	{
		if (!property.IsString())
		{
			base.SetOwnProperty(property, descriptor);

			return;
		}

		if (descriptor.IsDataDescriptor())
		{
			var key = property.ToString();

			list[key, caseInsensitive: false] = EcmaScriptHelper.JsValueToDataModelValue(descriptor.Value);

			base.SetOwnProperty(property, new ProxyPropertyDescriptor(Engine, list, key));
		}

		base.SetOwnProperty(property, descriptor);
	}
}