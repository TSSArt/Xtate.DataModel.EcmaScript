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

using System;
using System.Threading;
using System.Threading.Tasks;
using Jint.Native.Array;
using Jint.Parser.Ast;
using Xtate.Core;
using IServiceProvider = Xtate.IoC.IServiceProvider;

namespace Xtate.DataModel.EcmaScript
{
	public class EcmaScriptValueExpressionEvaluator : IValueExpression, IObjectEvaluator, IStringEvaluator, IIntegerEvaluator, IArrayEvaluator, IAncestorProvider, IDebugEntityId
	{
		private readonly Program          _program;
		private readonly IValueExpression _valueExpression;

		public required Func<ValueTask<EcmaScriptEngine>> EngineFactory { private get; init; }

		public EcmaScriptValueExpressionEvaluator(IValueExpression valueExpression, Program program)
		{
			_valueExpression = valueExpression;
			_program = program;
		}

	#region Interface IAncestorProvider

		object IAncestorProvider.Ancestor => _valueExpression;

	#endregion

	#region Interface IArrayEvaluator

		public async ValueTask<IObject[]> EvaluateArray()
		{
			var engine = await EngineFactory().ConfigureAwait(false);

			var array = engine.Eval(_program, startNewScope: true).AsArray();

			var result = new IObject[array.GetLength()];

			foreach (var pair in array.GetOwnProperties())
			{
				if (ArrayInstance.IsArrayIndex(pair.Key, out var index))
				{
					result[index] = new EcmaScriptObject(array.Get(pair.Key));
				}
			}

			return result;
		}

	#endregion

	#region Interface IDebugEntityId

		FormattableString? IDebugEntityId.EntityId => null;

	#endregion

	#region Interface IIntegerEvaluator

		async ValueTask<int> IIntegerEvaluator.EvaluateInteger()
		{
			var engine = await EngineFactory().ConfigureAwait(false);

			return (int) engine.Eval(_program, startNewScope: true).AsNumber();
		}

	#endregion

	#region Interface IObjectEvaluator

		async ValueTask<IObject> IObjectEvaluator.EvaluateObject()
		{
			var engine = await EngineFactory().ConfigureAwait(false);

			return new EcmaScriptObject(engine.Eval(_program, startNewScope: true));
		}

	#endregion

	#region Interface IStringEvaluator

		async ValueTask<string> IStringEvaluator.EvaluateString()
		{
			var engine = await EngineFactory().ConfigureAwait(false);

			return engine.Eval(_program, startNewScope: true).ToString() ?? string.Empty;
		}

	#endregion

	#region Interface IValueExpression

		public string? Expression => _valueExpression.Expression;

	#endregion
	}
}