#region Copyright © 2019-2020 Sergii Artemenko

// 
// This file is part of the Xtate project. <https://xtate.net/>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
// 

#endregion

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Jint.Parser;
using Jint.Parser.Ast;

namespace Xtate.DataModel.EcmaScript
{
	public class EcmaScriptDataModelHandler : DataModelHandlerBase
	{
		private const string DataModelType = "ecmascript";

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

		protected override void Build(ref ICustomAction customAction, ref CustomActionEntity customActionProperties)
		{
			base.Build(ref customAction, ref customActionProperties);

			customAction = new EcmaScriptCustomActionEvaluator(customActionProperties);
		}

		protected override void Build(ref IValueExpression valueExpression, ref ValueExpression valueExpressionProperties)
		{
			base.Build(ref valueExpression, ref valueExpressionProperties);

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
		}

		protected override void Build(ref IConditionExpression conditionExpression, ref ConditionExpression conditionExpressionProperties)
		{
			base.Build(ref conditionExpression, ref conditionExpressionProperties);

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
		}

		protected override void Build(ref ILocationExpression locationExpression, ref LocationExpression locationExpressionProperties)
		{
			base.Build(ref locationExpression, ref locationExpressionProperties);

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
		}

		protected override void Build(ref IScriptExpression scriptExpression, ref ScriptExpression scriptExpressionProperties)
		{
			base.Build(ref scriptExpression, ref scriptExpressionProperties);

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
		}

		protected override void Build(ref IExternalScriptExpression externalScriptExpression, ref ExternalScriptExpression externalScriptExpressionProperties)
		{
			base.Build(ref externalScriptExpression, ref externalScriptExpressionProperties);

			externalScriptExpression = new EcmaScriptExternalScriptExpressionEvaluator(externalScriptExpressionProperties);
		}

		protected override void Build(ref IInlineContent inlineContent, ref InlineContent inlineContentProperties)
		{
			base.Build(ref inlineContent, ref inlineContentProperties);

			inlineContent = new EcmaScriptInlineContentEvaluator(inlineContentProperties);
		}

		protected override void Build(ref IContentBody contentBody, ref ContentBody contentBodyProperties)
		{
			base.Build(ref contentBody, ref contentBodyProperties);

			contentBody = new EcmaScriptContentBodyEvaluator(contentBodyProperties);
		}

		private class DataModelHandlerFactory : IDataModelHandlerFactory
		{
		#region Interface IDataModelHandlerFactory

			public ValueTask<IDataModelHandler?> TryCreateHandler(string dataModelType, IErrorProcessor errorProcessor) =>
					dataModelType == DataModelType ? new ValueTask<IDataModelHandler?>(new EcmaScriptDataModelHandler(errorProcessor)) : default;

		#endregion
		}
	}
}