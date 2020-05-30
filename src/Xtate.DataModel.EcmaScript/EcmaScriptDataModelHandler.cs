using System;
using System.Collections.Immutable;
using Jint.Parser;
using Jint.Parser.Ast;

namespace Xtate.DataModel.EcmaScript
{
	public class EcmaScriptDataModelHandler : DataModelHandlerBase
	{
		private const string DataModelType      = "http://xtate.net/scxml/datamodel/#ECMAScript";
		private const string DataModelTypeAlias = "ecmascript";

		public static readonly  IDataModelHandlerFactory Factory       = new DataModelHandlerFactory();
		private static readonly ParserOptions            ParserOptions = new ParserOptions { Tolerant = true };

		private readonly JavaScriptParser _parser = new JavaScriptParser();

		private EcmaScriptDataModelHandler(IErrorProcessor errorProcessor) : base(errorProcessor) { }

		public override void ExecutionContextCreated(IExecutionContext executionContext, out ImmutableDictionary<string, string> dataModelVars)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			base.ExecutionContextCreated(executionContext, out dataModelVars);

			executionContext.RuntimeItems[EcmaScriptEngine.Key] = new EcmaScriptEngine(executionContext);

			dataModelVars = dataModelVars.SetItem(EcmaScriptHelper.JintVersionPropertyName, EcmaScriptHelper.JintVersionValue);
		}

		private Program Parse(string source) => _parser.Parse(source, ParserOptions);

		private static string GetErrorMessage(ParserException ex) => @$"{ex.Message} ({ex.Description}). Ln: {ex.LineNumber}. Col: {ex.Column}.";

		protected override void Build(ref IForEach forEach, ref ForEachEntity forEachProperties)
		{
			base.Build(ref forEach, ref forEachProperties);

			forEach = new EcmaScriptForEachEvaluator(forEachProperties);
		}

		protected override void Build(ref ICustomAction customAction, ref CustomAction customActionProperties)
		{
			base.Build(ref customAction, ref customActionProperties);

			customAction = new EcmaScriptCustomActionEvaluator(customActionProperties);
		}

		protected override void Build(ref IValueExpression valueExpression, ref ValueExpression valueExpressionProperties)
		{
			if (valueExpressionProperties.Expression != null)
			{
				var program = Parse(valueExpressionProperties.Expression);

				foreach (var parserException in program.Errors)
				{
					AddErrorMessage(valueExpression, GetErrorMessage(parserException));
				}

				valueExpression = new EcmaScriptValueExpressionEvaluator(valueExpressionProperties, program);
			}
			else
			{
				AddErrorMessage(valueExpression, Resources.ErrorMessage_Value_Expression_must_be_present);
			}

			base.Build(ref valueExpression, ref valueExpressionProperties);
		}

		protected override void Build(ref IConditionExpression conditionExpression, ref ConditionExpression conditionExpressionProperties)
		{
			if (conditionExpressionProperties.Expression != null)
			{
				var program = Parse(conditionExpressionProperties.Expression);

				foreach (var parserException in program.Errors)
				{
					AddErrorMessage(conditionExpression, GetErrorMessage(parserException));
				}

				conditionExpression = new EcmaScriptConditionExpressionEvaluator(conditionExpressionProperties, program);
			}
			else
			{
				AddErrorMessage(conditionExpression, Resources.ErrorMessage_Condition_Expression_must_be_present);
			}

			base.Build(ref conditionExpression, ref conditionExpressionProperties);
		}

		protected override void Build(ref ILocationExpression locationExpression, ref LocationExpression locationExpressionProperties)
		{
			if (locationExpressionProperties.Expression != null)
			{
				var program = Parse(locationExpressionProperties.Expression);

				foreach (var parserException in program.Errors)
				{
					AddErrorMessage(locationExpression, GetErrorMessage(parserException));
				}

				var leftExpression = EcmaScriptLocationExpressionEvaluator.GetLeftExpression(program);

				if (leftExpression != null)
				{
					locationExpression = new EcmaScriptLocationExpressionEvaluator(locationExpressionProperties, program, leftExpression);
				}
				else
				{
					AddErrorMessage(locationExpression, Resources.ErrorMessage_InvalidLocationExpression);
				}
			}
			else
			{
				AddErrorMessage(locationExpression, Resources.ErrorMessage_Location_Expression_must_be_present);
			}

			base.Build(ref locationExpression, ref locationExpressionProperties);
		}

		protected override void Build(ref IScriptExpression scriptExpression, ref ScriptExpression scriptExpressionProperties)
		{
			if (scriptExpressionProperties.Expression != null)
			{
				var program = Parse(scriptExpressionProperties.Expression);

				foreach (var parserException in program.Errors)
				{
					AddErrorMessage(scriptExpression, GetErrorMessage(parserException));
				}

				scriptExpression = new EcmaScriptScriptExpressionEvaluator(scriptExpressionProperties, program);
			}
			else
			{
				AddErrorMessage(scriptExpression, Resources.ErrorMessage_Script_Expression_must_be_present);
			}

			base.Build(ref scriptExpression, ref scriptExpressionProperties);
		}

		protected override void Build(ref IExternalScriptExpression externalScriptExpression, ref ExternalScriptExpression externalScriptExpressionProperties)
		{
			var _ = externalScriptExpression;

			externalScriptExpression = new EcmaScriptExternalScriptExpressionEvaluator(externalScriptExpressionProperties);

			base.Build(ref externalScriptExpression, ref externalScriptExpressionProperties);
		}

		protected override void Build(ref IContentBody contentBody, ref ContentBody contentBodyProperties)
		{
			var _ = contentBody;

			contentBody = new EcmaScriptContentBodyEvaluator(contentBodyProperties);

			base.Build(ref contentBody, ref contentBodyProperties);
		}

		private class DataModelHandlerFactory : IDataModelHandlerFactory
		{
		#region Interface IDataModelHandlerFactory

			public bool CanHandle(string dataModelType) => dataModelType == DataModelType || dataModelType == DataModelTypeAlias;

			public IDataModelHandler CreateHandler(IErrorProcessor errorProcessor) => new EcmaScriptDataModelHandler(errorProcessor);

		#endregion
		}
	}
}