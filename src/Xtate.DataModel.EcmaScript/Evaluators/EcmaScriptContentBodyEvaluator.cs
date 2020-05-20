using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Xtate.EcmaScript
{
	internal class EcmaScriptContentBodyEvaluator : DefaultContentBodyEvaluator, IObjectEvaluator
	{
		public EcmaScriptContentBodyEvaluator(in ContentBody contentBody) : base(in contentBody) { }

	#region Interface IObjectEvaluator

		public ValueTask<IObject> EvaluateObject(IExecutionContext executionContext, CancellationToken token)
		{
			try
			{
				return new ValueTask<IObject>(DataModelConverter.FromJson(Value));
			}
			catch (JsonException) { }

			return new ValueTask<IObject>(new DataModelValue(Value));
		}

	#endregion
	}
}