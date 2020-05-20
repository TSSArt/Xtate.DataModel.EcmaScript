using System;
using System.Threading;
using System.Threading.Tasks;
using Jint.Parser;
using Jint.Parser.Ast;

namespace Xtate.EcmaScript
{
	internal class EcmaScriptExternalScriptExpressionEvaluator : IExternalScriptExpression, IExecEvaluator, IExternalScriptConsumer, IAncestorProvider
	{
		private readonly ExternalScriptExpression _externalScriptExpression;
		private          Program?                 _program;

		public EcmaScriptExternalScriptExpressionEvaluator(in ExternalScriptExpression externalScriptExpression) => _externalScriptExpression = externalScriptExpression;

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => EcmaScriptHelper.GetAncestor(_externalScriptExpression);

	#endregion

	#region Interface IExecEvaluator

		public ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			Infrastructure.Assert(_program != null, Resources.Exception_ExternalScriptMissed);

			executionContext.Engine().Exec(_program, startNewScope: true);

			return default;
		}

	#endregion

	#region Interface IExternalScriptConsumer

		public void SetContent(string content)
		{
			if (content == null) throw new ArgumentNullException(nameof(content));

			_program = new JavaScriptParser().Parse(content);
		}

	#endregion

	#region Interface IExternalScriptExpression

		public Uri? Uri => _externalScriptExpression.Uri;

	#endregion
	}
}