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

using System.Collections;
using Xtate.DataTypes;

namespace Xtate.DataModel.EcmaScript.Internal;

internal class DataModelListWrapper(Engine engine, DataModelList list) : IList<JsValue?>
{
	public DataModelList List => list;

	private static DataModelValue ToDataModelValue(JsValue? value) => value is not null ? EcmaScriptHelper.JsValueToDataModelValue(value) : DataModelValue.Undefined;

	private JsValue ToJsValue(DataModelValue value) => EcmaScriptHelper.DataModelValueToJsValue(engine, value);

	#region Interface ICollection<JsValue>

	public void Add(JsValue? item) => list.Add(ToDataModelValue(item));

	public void Clear() => list.Clear();

	public bool Contains(JsValue? item) => list.Contains(ToDataModelValue(item));

	public void CopyTo(JsValue?[] array, int arrayIndex)
	{
		for (var i = 0; i < list.Count; i ++)
		{
			array[arrayIndex + i] = ToJsValue(list[i]);
		}
	}

	public bool Remove(JsValue? item) => list.Remove(ToDataModelValue(item));

	public int Count => list.Count;

	public bool IsReadOnly => list.IsReadOnly;

#endregion

#region Interface IEnumerable

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

#endregion

#region Interface IEnumerable<JsValue>

	public IEnumerator<JsValue?> GetEnumerator()
	{
		foreach (var item in list)
		{
			yield return ToJsValue(item);
		}
	}

#endregion

#region Interface IList<JsValue>

	public JsValue? this[int index]
	{
		get => ToJsValue(list[index]);
		set => list[index] = ToDataModelValue(value);
	}

	public int IndexOf(JsValue? item) => list.IndexOf(ToDataModelValue(item));

	public void Insert(int index, JsValue? item) => list.Insert(index, ToDataModelValue(item));

	public void RemoveAt(int index) => list.RemoveAt(index);

#endregion
}
