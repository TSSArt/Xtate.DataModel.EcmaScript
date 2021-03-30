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

using System.Threading;
using System.Threading.Tasks;
using Jint.Parser.Ast;
using Xtate.Core;

namespace Xtate.DataModel.EcmaScript
{
	internal class EcmaScriptConditionExpressionEvaluator : IConditionExpression, IBooleanEvaluator, IAncestorProvider
	{
		private readonly IConditionExpression _conditionExpression;
		private readonly Program              _program;

		public EcmaScriptConditionExpressionEvaluator(IConditionExpression conditionExpression, Program program)
		{
			_conditionExpression = conditionExpression;
			_program = program;
		}

	#region Interface IAncestorProvider

		object IAncestorProvider.Ancestor => _conditionExpression;

	#endregion

	#region Interface IBooleanEvaluator

		ValueTask<bool> IBooleanEvaluator.EvaluateBoolean(IExecutionContext executionContext, CancellationToken token) =>
			new(executionContext.Engine().Eval(_program, startNewScope: true).AsBoolean());

	#endregion

	#region Interface IConditionExpression

		public string? Expression => _conditionExpression.Expression;

	#endregion
	}
}