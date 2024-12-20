﻿// Copyright © 2019-2024 Sergii Artemenko
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
using Jint.Runtime.Environments;
using Jint.Runtime.Interop;

namespace Xtate.DataModel.EcmaScript;

public class EcmaScriptEngine
{
	private readonly Engine _jintEngine;

	private readonly HashSet<string> _variableSet = [];

	public EcmaScriptEngine()
	{
		_jintEngine = new Engine(options => options.Culture(CultureInfo.InvariantCulture).LimitRecursion(1024).Strict());

		var global = _jintEngine.Global;

		global.FastAddProperty(name: @"In", new DelegateWrapper(_jintEngine, InState), writable: false, enumerable: false, configurable: false);
	}

	public required IDataModelController DataModelController { private get; [UsedImplicitly] init; }

	public required IInStateController InStateController { private get; [UsedImplicitly] init; }

	private bool InState(string state) => InStateController.InState((Identifier) state);

	private void SyncRootVariables(DataModelList dataModel)
	{
		var global = _jintEngine.Global;
		List<string>? toRemove = default;

		foreach (var name in _variableSet)
		{
			if (!dataModel.TryGet(name, caseInsensitive: false, out _))
			{
				toRemove ??= [];
				toRemove.Add(name);
			}
		}

		if (toRemove is not null)
		{
			foreach (var property in toRemove)
			{
				_variableSet.Remove(property);
				global.RemoveOwnProperty(property);
			}
		}

		foreach (var keyValue in dataModel.KeyValues)
		{
			if (!string.IsNullOrEmpty(keyValue.Key) && global.GetOwnProperty(keyValue.Key) == PropertyDescriptor.Undefined)
			{
				var descriptor = EcmaScriptHelper.CreatePropertyAccessor(_jintEngine, dataModel, keyValue.Key);
				global.FastSetProperty(keyValue.Key, descriptor);
				_variableSet.Add(keyValue.Key);
			}
		}
	}

	public void EnterExecutionContext()
	{
		var lexicalEnvironment = LexicalEnvironment.NewDeclarativeEnvironment(_jintEngine, _jintEngine.ExecutionContext.LexicalEnvironment);
		_jintEngine.EnterExecutionContext(lexicalEnvironment, lexicalEnvironment, _jintEngine.ExecutionContext.ThisBinding);
	}

	public void LeaveExecutionContext()
	{
		_jintEngine.LeaveExecutionContext();
	}

	public JsValue Eval(Program program, bool startNewScope)
	{
		SyncRootVariables(DataModelController.DataModel);

		if (!startNewScope)
		{
			return _jintEngine.Execute(program).GetCompletionValue();
		}

		EnterExecutionContext();

		try
		{
			return _jintEngine.Execute(program).GetCompletionValue();
		}
		finally
		{
			LeaveExecutionContext();
		}
	}

	public JsValue Eval(Expression expression, bool startNewScope)
	{
		SyncRootVariables(DataModelController.DataModel);

		if (!startNewScope)
		{
			return JsValue.FromObject(_jintEngine, _jintEngine.EvaluateExpression(expression));
		}

		EnterExecutionContext();

		try
		{
			return JsValue.FromObject(_jintEngine, _jintEngine.EvaluateExpression(expression));
		}
		finally
		{
			LeaveExecutionContext();
		}
	}

	public void Exec(Program program, bool startNewScope)
	{
		SyncRootVariables(DataModelController.DataModel);

		if (!startNewScope)
		{
			_jintEngine.Execute(program);

			return;
		}

		EnterExecutionContext();

		try
		{
			_jintEngine.Execute(program);
		}
		finally
		{
			LeaveExecutionContext();
		}
	}

	public void Exec(Expression expression, bool startNewScope)
	{
		SyncRootVariables(DataModelController.DataModel);

		if (!startNewScope)
		{
			_jintEngine.EvaluateExpression(expression);

			return;
		}

		EnterExecutionContext();

		try
		{
			_jintEngine.EvaluateExpression(expression);
		}
		finally
		{
			LeaveExecutionContext();
		}
	}
}