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

using System.Collections.Generic;
using Jint;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Xtate.Core;

namespace Xtate.DataModel.EcmaScript
{
	internal class DataModelObjectWrapper : ObjectInstance, IObjectWrapper
	{
		private readonly DataModelList _list;

		public DataModelObjectWrapper(Engine engine, DataModelList list) : base(engine)
		{
			_list = list;

			Extensible = list.Access == DataModelAccess.Writable;
		}

	#region Interface IObjectWrapper

		public object Target => _list;

	#endregion

		public override void RemoveOwnProperty(string property)
		{
			_list.RemoveAll(property, caseInsensitive: false);

			base.RemoveOwnProperty(property);
		}

		public override IEnumerable<KeyValuePair<string, PropertyDescriptor>> GetOwnProperties()
		{
			foreach (var pair in _list.KeyValuePairs)
			{
				yield return new KeyValuePair<string, PropertyDescriptor>(pair.Key, GetOwnProperty(pair.Key));
			}
		}

		public override PropertyDescriptor GetOwnProperty(string property)
		{
			var descriptor = base.GetOwnProperty(property);

			if (descriptor != PropertyDescriptor.Undefined)
			{
				return descriptor;
			}

			descriptor = EcmaScriptHelper.CreatePropertyAccessor(Engine, _list, property);

			base.SetOwnProperty(property, descriptor);

			return descriptor;
		}

		public override bool HasOwnProperty(string property) => _list.ContainsKey(property, caseInsensitive: false);
	}
}