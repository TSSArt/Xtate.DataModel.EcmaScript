using System.Threading;
using System.Threading.Tasks;
using Jint.Parser.Ast;

namespace Xtate.EcmaScript
{
	internal class EcmaScriptConditionExpressionEvaluator : IConditionExpression, IBooleanEvaluator, IAncestorProvider
	{
		private readonly ConditionExpression _conditionExpression;
		private readonly Program             _program;

		public EcmaScriptConditionExpressionEvaluator(in ConditionExpression conditionExpression, Program program)
		{
			_conditionExpression = conditionExpression;
			_program = program;
		}

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => EcmaScriptHelper.GetAncestor(_conditionExpression);

	#endregion

	#region Interface IBooleanEvaluator

		ValueTask<bool> IBooleanEvaluator.EvaluateBoolean(IExecutionContext executionContext, CancellationToken token) =>
				new ValueTask<bool>(executionContext.Engine().Eval(_program, startNewScope: true).AsBoolean());

	#endregion

	#region Interface IConditionExpression

		public string? Expression => _conditionExpression.Expression;

	#endregion
	}
}