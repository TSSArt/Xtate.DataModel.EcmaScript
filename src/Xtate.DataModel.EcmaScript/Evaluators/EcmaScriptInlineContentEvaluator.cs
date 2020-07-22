using System;
using System.Text.Json;

namespace Xtate.DataModel.EcmaScript
{
	internal class EcmaScriptInlineContentEvaluator : DefaultInlineContentEvaluator
	{
		public EcmaScriptInlineContentEvaluator(in InlineContent inlineContent) : base(inlineContent) { }

		protected override DataModelValue ParseToDataModel(ref Exception? parseException)
		{
			try
			{
				return DataModelConverter.FromJson(Value);
			}
			catch (JsonException ex)
			{
				parseException = ex;

				return Value.NormalizeSpaces();
			}
		}
	}
}