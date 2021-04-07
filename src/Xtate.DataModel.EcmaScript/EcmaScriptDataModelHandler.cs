#region Copyright © 2019-2021 Sergii Artemenko

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

#endregion

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Jint.Parser;
using Jint.Parser.Ast;
using Xtate.Core;

namespace Xtate.DataModel.EcmaScript
{
	public class EcmaScriptDataModelHandler : DataModelHandlerBase
	{
		private const string DataModelType = "ecmascript";

		private static readonly ParserOptions ParserOptions = new() { Tolerant = true };

		private readonly JavaScriptParser _parser = new();

		public EcmaScriptDataModelHandler() : base(DefaultErrorProcessor.Instance) { }

		private EcmaScriptDataModelHandler(IErrorProcessor? errorProcessor) : base(errorProcessor) { }

		public static IDataModelHandlerFactory Factory { get; } = new DataModelHandlerFactory();

		public override ITypeInfo TypeInfo => TypeInfo<EcmaScriptDataModelHandler>.Instance;

		public override string ConvertToText(DataModelValue value) =>
			DataModelConverter.ToJson(value, DataModelConverterJsonOptions.WriteIndented | DataModelConverterJsonOptions.UndefinedToSkipOrNull);

		public override void ExecutionContextCreated(IExecutionContext executionContext, out ImmutableDictionary<string, string> dataModelVars)
		{
			if (executionContext is null) throw new ArgumentNullException(nameof(executionContext));

			base.ExecutionContextCreated(executionContext, out dataModelVars);

			executionContext.RuntimeItems[EcmaScriptEngine.Key] = new EcmaScriptEngine(executionContext);

			dataModelVars = dataModelVars.SetItem(EcmaScriptHelper.JintVersionPropertyName, EcmaScriptHelper.JintVersionValue);
		}

		private Program Parse(string source) => _parser.Parse(source, ParserOptions);

		private static string GetErrorMessage(ParserException ex) => @$"{ex.Message} ({ex.Description}). Ln: {ex.LineNumber}. Col: {ex.Column}.";

		protected override void Visit(ref IForEach forEach)
		{
			base.Visit(ref forEach);

			forEach = new EcmaScriptForEachEvaluator(forEach);
		}

		protected override void Visit(ref ICustomAction customAction)
		{
			base.Visit(ref customAction);

			customAction = new EcmaScriptCustomActionEvaluator(customAction);
		}

		protected override void Visit(ref IValueExpression valueExpression)
		{
			base.Visit(ref valueExpression);

			if (valueExpression.Expression is { } expression)
			{
				var program = Parse(expression);

				foreach (var parserException in program.Errors)
				{
					AddErrorMessage(valueExpression, GetErrorMessage(parserException));
				}

				valueExpression = new EcmaScriptValueExpressionEvaluator(valueExpression, program);
			}
			else
			{
				AddErrorMessage(valueExpression, Resources.ErrorMessage_ValueExpressionMustBePresent);
			}
		}

		protected override void Visit(ref IConditionExpression conditionExpression)
		{
			base.Visit(ref conditionExpression);

			if (conditionExpression.Expression is { } expression)
			{
				var program = Parse(expression);

				foreach (var parserException in program.Errors)
				{
					AddErrorMessage(conditionExpression, GetErrorMessage(parserException));
				}

				conditionExpression = new EcmaScriptConditionExpressionEvaluator(conditionExpression, program);
			}
			else
			{
				AddErrorMessage(conditionExpression, Resources.ErrorMessage_ConditionExpressionMustBePresent);
			}
		}

		protected override void Visit(ref ILocationExpression locationExpression)
		{
			base.Visit(ref locationExpression);

			if (locationExpression.Expression is { } expression)
			{
				var program = Parse(expression);

				foreach (var parserException in program.Errors)
				{
					AddErrorMessage(locationExpression, GetErrorMessage(parserException));
				}

				var leftExpression = EcmaScriptLocationExpressionEvaluator.GetLeftExpression(program);

				if (leftExpression is not null)
				{
					locationExpression = new EcmaScriptLocationExpressionEvaluator(locationExpression, program, leftExpression);
				}
				else
				{
					AddErrorMessage(locationExpression, Resources.ErrorMessage_InvalidLocationExpression);
				}
			}
			else
			{
				AddErrorMessage(locationExpression, Resources.ErrorMessage_LocationExpressionMustBePresent);
			}
		}

		protected override void Visit(ref IScriptExpression scriptExpression)
		{
			base.Visit(ref scriptExpression);

			if (scriptExpression.Expression is { } expression)
			{
				var program = Parse(expression);

				foreach (var parserException in program.Errors)
				{
					AddErrorMessage(scriptExpression, GetErrorMessage(parserException));
				}

				scriptExpression = new EcmaScriptScriptExpressionEvaluator(scriptExpression, program);
			}
			else
			{
				AddErrorMessage(scriptExpression, Resources.ErrorMessage_ScriptExpressionMustBePresent);
			}
		}

		protected override void Visit(ref IExternalScriptExpression externalScriptExpression)
		{
			base.Visit(ref externalScriptExpression);

			externalScriptExpression = new EcmaScriptExternalScriptExpressionEvaluator(externalScriptExpression);
		}

		protected override void Visit(ref IInlineContent inlineContent)
		{
			base.Visit(ref inlineContent);

			inlineContent = new EcmaScriptInlineContentEvaluator(inlineContent);
		}

		protected override void Visit(ref IContentBody contentBody)
		{
			base.Visit(ref contentBody);

			contentBody = new EcmaScriptContentBodyEvaluator(contentBody);
		}

		protected override void Visit(ref IExternalDataExpression externalDataExpression)
		{
			base.Visit(ref externalDataExpression);

			externalDataExpression = new EcmaScriptExternalDataExpressionEvaluator(externalDataExpression);
		}

		private static bool CanHandle(string dataModelType) => dataModelType == DataModelType;

		private class DataModelHandlerFactory : IDataModelHandlerFactory
		{
		#region Interface IDataModelHandlerFactory

			public ValueTask<IDataModelHandlerFactoryActivator?> TryGetActivator(IFactoryContext factoryContext, string dataModelType, CancellationToken token) =>
				new(CanHandle(dataModelType) ? DataModelHandlerFactoryActivator.Instance : null);

		#endregion
		}

		private class DataModelHandlerFactoryActivator : IDataModelHandlerFactoryActivator
		{
			public static IDataModelHandlerFactoryActivator Instance { get; } = new DataModelHandlerFactoryActivator();

		#region Interface IDataModelHandlerFactoryActivator

			public ValueTask<IDataModelHandler> CreateHandler(IFactoryContext factoryContext,
															  string dataModelType,
															  IErrorProcessor? errorProcessor,
															  CancellationToken token)
			{
				Infra.Assert(CanHandle(dataModelType));

				return new ValueTask<IDataModelHandler>(new EcmaScriptDataModelHandler(errorProcessor));
			}

		#endregion
		}
	}
}