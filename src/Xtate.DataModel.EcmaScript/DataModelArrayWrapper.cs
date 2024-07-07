// Copyright © 2019-2024 Sergii Artemenko
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
using Jint;
using Jint.Native.Array;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Xtate.DataModel.EcmaScript;

internal class DataModelArrayWrapper : ArrayInstance, IObjectWrapper
{
	private readonly DataModelList _list;

	public DataModelArrayWrapper(Engine engine, DataModelList list) : base(engine)
	{
		_list = list;

		var writable = list.Access == DataModelAccess.Writable;

		Extensible = writable;

		base.SetOwnProperty(propertyName: @"length", new PropertyDescriptor((uint) _list.Count, writable, enumerable: false, configurable: false));
	}

#region Interface IObjectWrapper

	public object Target => _list;

#endregion

	public override void RemoveOwnProperty(string property)
	{
		if (IsArrayIndex(property, out var index))
		{
			_list[(int) index] = default;
		}

		base.RemoveOwnProperty(property);
	}

	public override PropertyDescriptor GetOwnProperty(string property)
	{
		var descriptor = base.GetOwnProperty(property);

		if (descriptor == PropertyDescriptor.Undefined && IsArrayIndex(property, out var index))
		{
			descriptor = EcmaScriptHelper.CreateArrayIndexAccessor(Engine, _list, (int) index);
			base.SetOwnProperty(property, descriptor);
		}

		return descriptor;
	}

	protected override void SetOwnProperty(string property, PropertyDescriptor descriptor)
	{
		if (property == @"length" && descriptor.Value.IsNumber())
		{
			_list.SetLength((int) descriptor.Value.AsNumber());
		}

		base.SetOwnProperty(property, descriptor);
	}

	public override IEnumerable<KeyValuePair<string, PropertyDescriptor>> GetOwnProperties()
	{
		for (var i = 0; i < _list.Count; i ++)
		{
			var property = i.ToString(NumberFormatInfo.InvariantInfo);

			if (base.GetOwnProperty(property) == PropertyDescriptor.Undefined)
			{
				base.SetOwnProperty(property, EcmaScriptHelper.CreateArrayIndexAccessor(Engine, _list, i));
			}
		}

		return base.GetOwnProperties();
	}

	public override bool HasOwnProperty(string property)
	{
		if (IsArrayIndex(property, out var index) && index < _list.Count)
		{
			return true;
		}

		return base.HasOwnProperty(property);
	}
}