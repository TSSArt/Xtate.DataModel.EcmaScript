using System.Threading;
using System.Threading.Tasks;

namespace Xtate.EcmaScript
{
	internal class EcmaScriptCustomActionEvaluator : DefaultCustomActionEvaluator
	{
		public EcmaScriptCustomActionEvaluator(in CustomAction customAction) : base(customAction) { }

		public override async ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			var engine = executionContext.Engine();

			engine.EnterExecutionContext();

			try
			{
				await base.Execute(executionContext, token).ConfigureAwait(false);
			}
			finally
			{
				engine.LeaveExecutionContext();
			}
		}
	}
}