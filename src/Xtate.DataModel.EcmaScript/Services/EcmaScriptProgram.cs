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

using System.Collections.Immutable;
using Acornima;
using Acornima.Ast;
using JintIdentifier = Acornima.Ast.Identifier;

namespace Xtate.DataModel.EcmaScript.Services;

public class EcmaScriptProgram
{
	private static readonly Prepared<Script> UndefinedScript = Engine.PrepareScript(@"undefined");

	public EcmaScriptProgram(Prepared<Script> script)
	{
		Script = script;
		Errors = [];
	}

	private EcmaScriptProgram(Prepared<Script> script, ImmutableArray<ParseError> errors)
	{
		Script = script;
		Errors = errors;
	}

	public Prepared<Script> Script { get; }

	public ImmutableArray<ParseError> Errors { get; }

	public bool HasErrors => !Errors.IsDefaultOrEmpty;

	public static EcmaScriptProgram ParseLocation(string location)
	{
		try
		{
			var preparedScript = Engine.PrepareScript(location);

			return preparedScript.Program?.Body is [ExpressionStatement { Expression: JintIdentifier or MemberExpression }]
				? new EcmaScriptProgram(preparedScript)
				: new EcmaScriptProgram(UndefinedScript, [new SyntaxError(location, Resources.ErrorMessage_InvalidLocationExpression)]);
		}
		catch (ScriptPreparationException ex) when (ex.InnerException is ParseErrorException)
		{
			return new EcmaScriptProgram(UndefinedScript, GetErrors(location));
		}
	}

	public static EcmaScriptProgram ParseScript(string source)
	{
		try
		{
			return new EcmaScriptProgram(Engine.PrepareScript(source));
		}
		catch (ScriptPreparationException ex) when (ex.InnerException is ParseErrorException)
		{
			return new EcmaScriptProgram(UndefinedScript, GetErrors(source));
		}
	}

	private static ImmutableArray<ParseError> GetErrors(string source)
	{
		var errorCollector = new ParseErrorCollector();
		var parser = new Parser(new ParserOptions { Tolerant = true, ErrorHandler = errorCollector });

		try
		{
			parser.ParseScript(source);

			return [.. errorCollector.Errors];
		}
		catch (ParseErrorException ex)
		{
			return [.. errorCollector.Errors, ex.Error];
		}
	}
}