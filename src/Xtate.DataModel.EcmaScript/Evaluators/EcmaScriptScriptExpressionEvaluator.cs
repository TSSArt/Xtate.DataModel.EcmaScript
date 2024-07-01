﻿#region Copyright © 2019-2021 Sergii Artemenko

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

<<<<<<< Updated upstream
using System;
using System.Threading;
using System.Threading.Tasks;
using Jint.Parser.Ast;
using Xtate.Core;

namespace Xtate.DataModel.EcmaScript
{
	public class EcmaScriptScriptExpressionEvaluator : IScriptExpression, IExecEvaluator, IAncestorProvider
	{
		private readonly Program           _program;
		private readonly IScriptExpression _scriptExpression;

		public required Func<ValueTask<EcmaScriptEngine>> EngineFactory { private get; init; }

		public EcmaScriptScriptExpressionEvaluator(IScriptExpression scriptExpression, Program program)
		{
			_scriptExpression = scriptExpression;
			_program = program;
		}
=======
	using System;
	using System.Threading.Tasks;
	using Jint.Parser.Ast;
	using Xtate.Core;

	namespace Xtate.DataModel.EcmaScript;

	public class EcmaScriptScriptExpressionEvaluator(IScriptExpression scriptExpression, Program program) : IScriptExpression, IExecEvaluator, IAncestorProvider
	{
		public required Func<ValueTask<EcmaScriptEngine>> EngineFactory { private get; [UsedImplicitly] init; }
>>>>>>> Stashed changes

	#region Interface IAncestorProvider

		object IAncestorProvider.Ancestor => scriptExpression;

	#endregion

	#region Interface IExecEvaluator

		public async ValueTask Execute()
		{
			var engine = await EngineFactory().ConfigureAwait(false);

<<<<<<< Updated upstream
			engine.Exec(_program, startNewScope: true);
=======
			engine.Exec(program, startNewScope: true);
>>>>>>> Stashed changes
		}

	#endregion

	#region Interface IScriptExpression

		public string? Expression => scriptExpression.Expression;

	#endregion
	}