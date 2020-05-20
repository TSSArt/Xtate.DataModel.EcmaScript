using System.Threading;
using System.Threading.Tasks;
using Jint.Parser.Ast;

namespace Xtate.EcmaScript
{
	internal class EcmaScriptScriptExpressionEvaluator : IScriptExpression, IExecEvaluator, IAncestorProvider
	{
		private readonly Program           _program;
		private readonly IScriptExpression _scriptExpression;

		public EcmaScriptScriptExpressionEvaluator(IScriptExpression scriptExpression, Program program)
		{
			_scriptExpression = scriptExpression;
			_program = program;
		}

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => _scriptExpression;

	#endregion

	#region Interface IExecEvaluator

		public ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			executionContext.Engine().Exec(_program, startNewScope: true);

			return default;
		}

	#endregion

	#region Interface IScriptExpression

		public string? Expression => _scriptExpression.Expression;

	#endregion
	}
}