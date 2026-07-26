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

using System.Linq;
using Acornima.Ast;
using Xtate.Ancestor;
using Xtate.DataModel.EcmaScript.Internal;
using Xtate.DataTypes;
using Xtate.StateMachine;
using JintIdentifier = Acornima.Ast.Identifier;

namespace Xtate.DataModel.EcmaScript.Services;

public class EcmaScriptLocationExpressionEvaluator(ILocationExpression locationExpression, EcmaScriptProgram program) : ILocationEvaluator, ILocationExpression, IAncestorProvider
{
	private readonly EcmaScriptProgram? _declare = CreateDeclare(program);

	private readonly string? _name = CreateName(program);

	private readonly EcmaScriptProgram _setter = CreateSetter(locationExpression.Expression!);

	public required Func<ValueTask<EcmaScriptEngine>> EngineFactory { private get; [SetByIoC] init; }

#region Interface IAncestorProvider

	object IAncestorProvider.Ancestor => locationExpression;

#endregion

#region Interface ILocationEvaluator

	public async ValueTask<IObject> GetValue()
	{
		var engine = await EngineFactory().ConfigureAwait(false);

		return new EcmaScriptObject(await engine.Eval(program, startNewScope: true).ConfigureAwait(false));
	}

	public ValueTask<string> GetName() => new(_name ?? throw new ExecutionException(Resources.Exception_NameOfLocationExpressionCantBeEvaluated));

	public async ValueTask SetValue(IObject value)
	{
		var engine = await EngineFactory().ConfigureAwait(false);
		var rightValue = value is EcmaScriptObject ecmaScriptObject ? ecmaScriptObject.JsValue : JsValue.FromObject(engine.JintEngine, value.ToObject());
		var setter = await engine.Eval(_setter, startNewScope: false).ConfigureAwait(false);

		engine.Call(setter, rightValue);
	}

#endregion

#region Interface ILocationExpression

	public string? Expression => locationExpression.Expression;

#endregion

	public async ValueTask DeclareLocalVariable()
	{
		if (_declare is null)
		{
			throw new ExecutionException(Resources.Exception_InvalidLocalVariableName);
		}

		var engine = await EngineFactory().ConfigureAwait(false);

		await engine.Exec(_declare, startNewScope: false).ConfigureAwait(false);
	}

	private static EcmaScriptProgram? CreateDeclare(EcmaScriptProgram program)
	{
		var expression = ((ExpressionStatement)program.Script.Program!.Body.Single()).Expression;

		if (expression is not JintIdentifier identifier)
		{
			return null;
		}

		return new EcmaScriptProgram(Engine.PrepareScript($@"var {identifier.Name};"));
	}

	private static string? CreateName(EcmaScriptProgram program)
	{
		var expression = ((ExpressionStatement)program.Script.Program!.Body.Single()).Expression;

		return expression switch
			   {
				   null                              => null,
				   JintIdentifier identifier         => identifier.Name,
				   MemberExpression memberExpression => ((JintIdentifier)memberExpression.Property).Name,
				   _                                 => throw Infra.Unmatched(expression)
			   };
	}

	private static EcmaScriptProgram CreateSetter(string locationExpression)
	{
		var valueParameter = @"__xv";

		while (locationExpression.Contains(valueParameter, StringComparison.Ordinal))
		{
			valueParameter = @$"{valueParameter}_{valueParameter.GetHashCode():x8}";
		}

		return new EcmaScriptProgram(Engine.PrepareScript($@"{valueParameter} => ({locationExpression} = {valueParameter})"));
	}
}