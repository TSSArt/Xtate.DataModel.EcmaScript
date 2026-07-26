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
using Xtate.DataTypes;

namespace Xtate.DataModel.EcmaScript.Internal;

internal class ProxyPropertyDescriptor : PropertyDescriptor
{
	private readonly Engine _engine;

	private readonly DataModelList _list;

	private readonly string _property;

	public ProxyPropertyDescriptor(Engine engine, DataModelList list, string property) : base(value: null, writable: null, enumerable: true, configurable: false)
	{
		_engine = engine;
		_list = list;
		_property = property;

		Get = JsValue.FromObject(engine, new Func<JsValue>(Getter));
		Set = JsValue.FromObject(engine, new Action<JsValue>(Setter));
	}

	public bool HasUnderlyingKey => _list.ContainsKey(_property, caseInsensitive: false);

	public override JsValue? Get { get; }

	public override JsValue? Set { get; }

	private JsValue Getter() => EcmaScriptHelper.DataModelValueToJsValue(_engine, _list[_property, caseInsensitive: false]);

	private void Setter(JsValue value) => _list[_property, caseInsensitive: false] = EcmaScriptHelper.JsValueToDataModelValue(value);
}