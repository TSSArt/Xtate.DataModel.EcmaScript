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
using Xtate.DataModel.EcmaScript.Properties;
using Xtate.DataTypes;
using Xtate.StateMachine;
using JintIdentifier = Acornima.Ast.Identifier;

namespace Xtate.DataModel.EcmaScript.Services;

public class EcmaScriptLocationExpressionEvaluator : ILocationEvaluator, ILocationExpression, IAncestorProvider
{
	private readonly Prepared<Script> _assignment;

	private readonly string? _localVariableName;

	private readonly ILocationExpression _locationExpression;

	private readonly string? _name;

	private readonly Prepared<Script> _program;

	public EcmaScriptLocationExpressionEvaluator(ILocationExpression locationExpression, Prepared<Script> program, Expression? leftExpression)
	{
		_locationExpression = locationExpression;
		_program = program;
		_assignment = Engine.PrepareScript(@$"{locationExpression.Expression} = {EcmaScriptEngine.LocationValueProperty}");

		switch (leftExpression)
		{
			case null:
				break;

			case JintIdentifier identifier:
				_name = identifier.Name;
				_localVariableName = identifier.Name;

				break;

			case MemberExpression memberExpression:
				_name = ((JintIdentifier)memberExpression.Property).Name;

				break;

			default:
				throw new InvalidOperationException();
		}
	}

	public required Func<ValueTask<EcmaScriptEngine>> EngineFactory { private get; [SetByIoC] init; }

#region Interface IAncestorProvider

	object IAncestorProvider.Ancestor => _locationExpression;

#endregion

#region Interface ILocationEvaluator

	public async ValueTask<IObject> GetValue()
	{
		var engine = await EngineFactory().ConfigureAwait(false);

		return new EcmaScriptObject(engine.Eval(_program, startNewScope: true));
	}

	public ValueTask<string> GetName() => new(_name ?? throw new ExecutionException(Resources.Exception_NameOfLocationExpressionCantBeEvaluated));

	public async ValueTask SetValue(IObject value)
	{
		var rightValue = value is EcmaScriptObject ecmaScriptObject ? ecmaScriptObject.JsValue : value.ToObject();
		var engine = await EngineFactory().ConfigureAwait(false);
		engine.SetLocationValue(_assignment, _localVariableName, rightValue);
	}

#endregion

#region Interface ILocationExpression

	public string? Expression => _locationExpression.Expression;

#endregion

	public async ValueTask DeclareLocalVariable()
	{
		if (_localVariableName is null)
		{
			throw new ExecutionException(Resources.Exception_InvalidLocalVariableName);
		}

		var engine = await EngineFactory().ConfigureAwait(false);

		engine.DeclareLocalVariable(_localVariableName);
	}

	public static Expression? GetLeftExpression(Script program)
	{
		Expression? expression = default;

		foreach (var statement in program.Body)
		{
			expression = (statement as ExpressionStatement)?.Expression;

			break;
		}

		return expression switch
			   {
				   JintIdentifier identifier         => identifier,
				   MemberExpression memberExpression => memberExpression,
				   _                                 => null
			   };
	}
}