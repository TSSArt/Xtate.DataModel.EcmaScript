﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Jint.Native.Array;
using Jint.Parser.Ast;

namespace TSSArt.StateMachine.EcmaScript
{
	internal class EcmaScriptValueExpressionEvaluator : IValueExpression, IObjectEvaluator, IStringEvaluator, IIntegerEvaluator, IArrayEvaluator, IAncestorProvider, IDebugEntityId
	{
		private readonly Program         _program;
		private readonly ValueExpression _valueExpression;

		public EcmaScriptValueExpressionEvaluator(in ValueExpression valueExpression, Program program)
		{
			_valueExpression = valueExpression;
			_program = program;
		}

		object? IAncestorProvider.Ancestor => EcmaScriptHelper.GetAncestor(_valueExpression);

		public ValueTask<IObject[]> EvaluateArray(IExecutionContext executionContext, CancellationToken token)
		{
			var array = executionContext.Engine().Eval(_program, startNewScope: true).AsArray();

			var result = new IObject[array.GetLength()];

			foreach (var pair in array.GetOwnProperties())
			{
				if (ArrayInstance.IsArrayIndex(pair.Key, out var index))
				{
					result[index] = new EcmaScriptObject(array.Get(pair.Key));
				}
			}

			return new ValueTask<IObject[]>(result);
		}

		FormattableString? IDebugEntityId.EntityId => null;

		ValueTask<int> IIntegerEvaluator.EvaluateInteger(IExecutionContext executionContext, CancellationToken token) =>
				new ValueTask<int>((int) executionContext.Engine().Eval(_program, startNewScope: true).AsNumber());

		ValueTask<IObject> IObjectEvaluator.EvaluateObject(IExecutionContext executionContext, CancellationToken token) =>
				new ValueTask<IObject>(new EcmaScriptObject(executionContext.Engine().Eval(_program, startNewScope: true)));

		ValueTask<string> IStringEvaluator.EvaluateString(IExecutionContext executionContext, CancellationToken token) =>
				new ValueTask<string>(executionContext.Engine().Eval(_program, startNewScope: true).ToString());

		public string? Expression => _valueExpression.Expression;
	}
}