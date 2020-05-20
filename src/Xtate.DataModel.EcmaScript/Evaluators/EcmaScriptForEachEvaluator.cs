using System.Threading;
using System.Threading.Tasks;

namespace Xtate.EcmaScript
{
	internal class EcmaScriptForEachEvaluator : DefaultForEachEvaluator
	{
		public EcmaScriptForEachEvaluator(in ForEachEntity forEach) : base(forEach) { }

		public override async ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			var engine = executionContext.Engine();

			engine.EnterExecutionContext();

			try
			{
				ItemEvaluator.DeclareLocalVariable(executionContext);
				IndexEvaluator?.DeclareLocalVariable(executionContext);

				await base.Execute(executionContext, token).ConfigureAwait(false);
			}
			finally
			{
				engine.LeaveExecutionContext();
			}
		}
	}
}