using System.Threading;
using System.Threading.Tasks;

namespace Xtate.DataModel.EcmaScript
{
	internal class EcmaScriptCustomActionEvaluator : DefaultCustomActionEvaluator
	{
		public EcmaScriptCustomActionEvaluator(in CustomActionEntity customAction) : base(customAction) { }

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