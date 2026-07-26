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

using Xtate.Ancestor;
using Xtate.DataModel.EcmaScript.Internal;
using Xtate.DataTypes;
using Xtate.StateMachine;

namespace Xtate.DataModel.EcmaScript.Services;

public class EcmaScriptValueExpressionEvaluator(IValueExpression valueExpression, EcmaScriptProgram program)
	: IValueExpression, IObjectEvaluator, IStringEvaluator, IIntegerEvaluator, IArrayEvaluator, IAncestorProvider
{
	public required Func<ValueTask<EcmaScriptEngine>> EngineFactory { private get; [SetByIoC] init; }

#region Interface IAncestorProvider

	object IAncestorProvider.Ancestor => valueExpression;

#endregion

#region Interface IArrayEvaluator

	public async ValueTask<IObject[]> EvaluateArray()
	{
		var engine = await EngineFactory().ConfigureAwait(false);

		var value = await engine.Eval(program, startNewScope: true).ConfigureAwait(false);

		var array = value.AsObject();

		var result = new IObject[(int)array.Get(@"length").AsNumber()];

		for (var index = 0; index < result.Length; index ++)
		{
			result[index] = new EcmaScriptObject(array.Get(index.ToString()));
		}

		return result;
	}

#endregion

#region Interface IIntegerEvaluator

	async ValueTask<int> IIntegerEvaluator.EvaluateInteger()
	{
		var engine = await EngineFactory().ConfigureAwait(false);

		var value = await engine.Eval(program, startNewScope: true).ConfigureAwait(false);

		return (int)value.AsNumber();
	}

#endregion

#region Interface IObjectEvaluator

	async ValueTask<IObject> IObjectEvaluator.EvaluateObject()
	{
		var engine = await EngineFactory().ConfigureAwait(false);

		var value = await engine.Eval(program, startNewScope: true).ConfigureAwait(false);

		return new EcmaScriptObject(value);
	}

#endregion

#region Interface IStringEvaluator

	async ValueTask<string> IStringEvaluator.EvaluateString()
	{
		var engine = await EngineFactory().ConfigureAwait(false);

		var value = await engine.Eval(program, startNewScope: true).ConfigureAwait(false);

		return value.ToString();
	}

#endregion

#region Interface IValueExpression

	public string? Expression => valueExpression.Expression;

#endregion
}