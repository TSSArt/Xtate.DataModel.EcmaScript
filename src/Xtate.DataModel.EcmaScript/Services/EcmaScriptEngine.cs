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
using Jint.Runtime.Descriptors;
using Xtate.DataModel.EcmaScript.Internal;
using Xtate.DataTypes;
using Identifier = Xtate.StateMachine.Identifier;

namespace Xtate.DataModel.EcmaScript.Services;

public class EcmaScriptEngine
{
	private static readonly JsValue In = @"In";

	private readonly Dictionary<string, JsValue> _variableSet = [];

	public EcmaScriptEngine()
	{
		JintEngine = new Engine(SetEngineOptions);

		var global = JintEngine.Global;

		global.DefineOwnProperty(In, new PropertyDescriptor(JsValue.FromObject(JintEngine, new Func<string, bool>(InState)), writable: false, enumerable: false, configurable: false));
	}

	internal Engine JintEngine { get; }

	public required IDataModelController DataModelController { private get; [SetByIoC] init; }

	public required IInStateController InStateController { private get; [SetByIoC] init; }

	private static void SetEngineOptions(Options options) =>
		options
			.Culture(CultureInfo.InvariantCulture)
			.LimitRecursion(1024)
			.Strict();

	private bool InState(string state) => InStateController.InState((Identifier)state);

	private void SyncRootVariables(DataModelList dataModel)
	{
		var global = JintEngine.Global;
		List<JsValue>? toRemove = null;

		foreach (var (name, jsValue) in _variableSet)
		{
			if (!dataModel.TryGet(name, caseInsensitive: false, out _))
			{
				toRemove ??= [];
				toRemove.Add(jsValue);
			}
		}

		if (toRemove is not null)
		{
			foreach (var property in toRemove)
			{
				_variableSet.Remove(property.ToString());
				global.RemoveOwnProperty(property);
			}
		}

		foreach (var (key, _) in dataModel.KeyValues)
		{
			if (string.IsNullOrEmpty(key))
			{
				continue;
			}

			if (!_variableSet.TryGetValue(key, out var jsValue))
			{
				jsValue = key;

				_variableSet.Add(key, jsValue);
			}

			if (global.GetOwnProperty(jsValue) is not ProxyPropertyDescriptor)
			{
				global.DefineOwnProperty(jsValue, new ProxyPropertyDescriptor(JintEngine, dataModel, key));
			}
		}
	}

	public ValueTask<JsValue> Eval(EcmaScriptProgram program, bool startNewScope)
	{
		SyncRootVariables(DataModelController.DataModel);

	   return new ValueTask<JsValue>(JintEngine.EvaluateAsync(program.Script));
	}

	public ValueTask Exec(EcmaScriptProgram program, bool startNewScope)
	{
		SyncRootVariables(DataModelController.DataModel);

		return new ValueTask(JintEngine.EvaluateAsync(program.Script));
	}

	public void Call(JsValue callable, params JsValue[] jsValues)
	{
		SyncRootVariables(DataModelController.DataModel);

		JintEngine.Call(callable, jsValues);
	}
}
