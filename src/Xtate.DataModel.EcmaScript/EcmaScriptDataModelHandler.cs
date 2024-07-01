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

<<<<<<< Updated upstream
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Jint.Parser;
using Jint.Parser.Ast;
using Xtate.Core;

namespace Xtate.DataModel.EcmaScript
{

	public class EcmaScriptDataModelHandler : DataModelHandlerBase
	{
		public required Func<IForEach, EcmaScriptForEachEvaluator>                                               ForEachEvaluatorFactory                  { private get; init; }
		public required Func<ICustomAction, EcmaScriptCustomActionEvaluator>                                     CustomActionEvaluatorFactory             { private get; init; }
		public required Func<IInlineContent, EcmaScriptInlineContentEvaluator>                                   InlineContentEvaluatorFactory            { private get; init; }
		public required Func<IContentBody, EcmaScriptContentBodyEvaluator>                                       ContentBodyEvaluatorFactory              { private get; init; }
		public required Func<IExternalDataExpression, EcmaScriptExternalDataExpressionEvaluator>                 ExternalDataExpressionEvaluatorFactory   { private get; init; }

		public required Func<IValueExpression, Program, EcmaScriptValueExpressionEvaluator> ValueExpressionEvaluatorFactory
		{
			 get => _valueExpressionEvaluatorFactory;
			init => _valueExpressionEvaluatorFactory = value;
		}

		public required Func<IConditionExpression, Program, EcmaScriptConditionExpressionEvaluator>              ConditionExpressionEvaluatorFactory      { private get; init; }
		public required Func<ILocationExpression, (Program, Expression?), EcmaScriptLocationExpressionEvaluator> LocationExpressionEvaluatorFactory       { private get; init; }
		public required Func<IScriptExpression, Program, EcmaScriptScriptExpressionEvaluator>                    ScriptExpressionEvaluatorFactory         { private get; init; }
		public required Func<IExternalScriptExpression, EcmaScriptExternalScriptExpressionEvaluator>             ExternalScriptExpressionEvaluatorFactory { private get; init; }
		public required IErrorProcessorService<EcmaScriptDataModelHandler>                                       _errorProcessorService                   { private get; init; }

		private static readonly ParserOptions ParserOptions = new() { Tolerant = true };

		private readonly JavaScriptParser                                                    _parser = new();
		private readonly Func<IValueExpression, Program, EcmaScriptValueExpressionEvaluator> _valueExpressionEvaluatorFactory;

		protected override IExternalDataExpression GetEvaluator(IExternalDataExpression externalDataExpression) => ExternalDataExpressionEvaluatorFactory(externalDataExpression);

		protected override IForEach GetEvaluator(IForEach forEach) => ForEachEvaluatorFactory(forEach);

		protected override IInlineContent GetEvaluator(IInlineContent inlineContent) => InlineContentEvaluatorFactory(inlineContent);

		

		protected override ICustomAction GetEvaluator(ICustomAction customAction) => CustomActionEvaluatorFactory(customAction);

		protected override IContentBody GetEvaluator(IContentBody contentBody) => ContentBodyEvaluatorFactory(contentBody);



		public override string ConvertToText(DataModelValue value) =>
			DataModelConverter.ToJson(value, DataModelConverterJsonOptions.WriteIndented | DataModelConverterJsonOptions.UndefinedToSkipOrNull);

		public override ImmutableDictionary<string, string> DataModelVars => base.DataModelVars.SetItem(EcmaScriptHelper.JintVersionPropertyName, EcmaScriptHelper.JintVersionValue);

		private Program Parse(string source) => _parser.Parse(source, ParserOptions);

		private static string GetErrorMessage(ParserException ex) => @$"{ex.Message} ({ex.Description}). Ln: {ex.LineNumber}. Col: {ex.Column}.";
		
	

		protected override void Visit(ref IValueExpression valueExpression)
		{
			base.Visit(ref valueExpression);

			if (valueExpression.Expression is { } expression)
=======
	using System;
	using System.Collections.Immutable;
	using System.Threading;
	using System.Threading.Tasks;
	using Jint.Parser;
	using Jint.Parser.Ast;
	using Xtate.Core;
	using Xtate.DataModel.XPath;
	using Xtate.Scxml;

	namespace Xtate.DataModel.EcmaScript
	{
		public class EcmaScriptDataModelHandlerProvider : DataModelHandlerProviderBase<EcmaScriptDataModelHandler>
		{
			protected override bool CanHandle(string? dataModelType) => dataModelType == @"ecmascript";
		}

		public class EcmaScriptDataModelHandler : DataModelHandlerBase
		{
			public required Func<IForEach, EcmaScriptForEachEvaluator> EcmaScriptForEachEvaluatorFactory { private get; [UsedImplicitly] init; }
			public required Func<ICustomAction, EcmaScriptCustomActionEvaluator> EcmaScriptCustomActionEvaluatorFactory { private get; [UsedImplicitly] init; }
			public required Func<IExternalScriptExpression, EcmaScriptExternalScriptExpressionEvaluator> EcmaScriptExternalScriptExpressionEvaluatorFactory { private get; [UsedImplicitly] init; }
			public required Func<IExternalDataExpression, EcmaScriptExternalDataExpressionEvaluator> EcmaScriptExternalDataExpressionEvaluatorFactory { private get; [UsedImplicitly] init; }
			public required IErrorProcessorService<EcmaScriptDataModelHandler> EcmaScriptErrorProcessorService { private get; [UsedImplicitly] init; }
			public required Func<IValueExpression, Program, EcmaScriptValueExpressionEvaluator> EcmaScriptValueExpressionEvaluatorFactory { private get; [UsedImplicitly] init; }
			public required Func<IConditionExpression, Program, EcmaScriptConditionExpressionEvaluator> EcmaScriptConditionExpressionEvaluatorFactory { private get; [UsedImplicitly] init; }
			public required Func<IScriptExpression, Program, EcmaScriptScriptExpressionEvaluator> EcmaScriptScriptExpressionEvaluatorFactory { private get; [UsedImplicitly] init; }
			public required Func<ILocationExpression, (Program, Expression?), EcmaScriptLocationExpressionEvaluator> EcmaScriptLocationExpressionEvaluatorFactory { private get; [UsedImplicitly] init; }

			private static readonly ParserOptions ParserOptions = new() { Tolerant = true };

			private readonly JavaScriptParser _parser = new();

			public override string ConvertToText(DataModelValue value) =>
				DataModelConverter.ToJson(value, DataModelConverterJsonOptions.WriteIndented | DataModelConverterJsonOptions.UndefinedToSkipOrNull);
		
			//TODO: move it
			/*
			public override void ExecutionContextCreated(out ImmutableDictionary<string, string> dataModelVars)
>>>>>>> Stashed changes
			{
				if (executionContext is null) throw new ArgumentNullException(nameof(executionContext));

<<<<<<< Updated upstream
				foreach (var parserException in program.Errors)
				{
					_errorProcessorService.AddError(valueExpression, GetErrorMessage(parserException));
				}

				valueExpression = ValueExpressionEvaluatorFactory(valueExpression, program);
=======
				base.ExecutionContextCreated(executionContext, out dataModelVars);

				executionContext.RuntimeItems[EcmaScriptEngine.Key] = new EcmaScriptEngine(executionContext);

				dataModelVars = dataModelVars.SetItem(EcmaScriptHelper.JintVersionPropertyName, EcmaScriptHelper.JintVersionValue);
			}*/

			private Program Parse(string source) => _parser.Parse(source, ParserOptions);

			private static string GetErrorMessage(ParserException ex) => @$"{ex.Message} ({ex.Description}). Ln: {ex.LineNumber}. Col: {ex.Column}.";

			protected override void Visit(ref IForEach forEach)
			{
				base.Visit(ref forEach);

				forEach = EcmaScriptForEachEvaluatorFactory(forEach);
>>>>>>> Stashed changes
			}

			protected override void Visit(ref ICustomAction customAction)
			{
<<<<<<< Updated upstream
				_errorProcessorService.AddError(valueExpression, Resources.ErrorMessage_ValueExpressionMustBePresent);
=======
				base.Visit(ref customAction);

				customAction = EcmaScriptCustomActionEvaluatorFactory(customAction);
>>>>>>> Stashed changes
			}

			protected override void Visit(ref IValueExpression valueExpression)
			{
				base.Visit(ref valueExpression);

				if (valueExpression.Expression is { } expression)
				{
<<<<<<< Updated upstream
					_errorProcessorService.AddError(conditionExpression, GetErrorMessage(parserException));
				}

				conditionExpression = ConditionExpressionEvaluatorFactory(conditionExpression, program);
			}
			else
			{
				_errorProcessorService.AddError(conditionExpression, Resources.ErrorMessage_ConditionExpressionMustBePresent);
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
					_errorProcessorService.AddError(locationExpression, GetErrorMessage(parserException));
				}

				var leftExpression = EcmaScriptLocationExpressionEvaluator.GetLeftExpression(program);

				if (leftExpression is not null)
				{
					locationExpression = LocationExpressionEvaluatorFactory(locationExpression, (program, leftExpression));
				}
				else
				{
					_errorProcessorService.AddError(locationExpression, Resources.ErrorMessage_InvalidLocationExpression);
=======
					var program = Parse(expression);

					foreach (var parserException in program.Errors)
					{
						AddErrorMessage(valueExpression, GetErrorMessage(parserException));
					}

					valueExpression = EcmaScriptValueExpressionEvaluatorFactory(valueExpression, program);
				}
				else
				{
					AddErrorMessage(valueExpression, Resources.ErrorMessage_ValueExpressionMustBePresent);
>>>>>>> Stashed changes
				}
			}

			protected override void Visit(ref IConditionExpression conditionExpression)
			{
<<<<<<< Updated upstream
				_errorProcessorService.AddError(locationExpression, Resources.ErrorMessage_LocationExpressionMustBePresent);
			}
		}
=======
				base.Visit(ref conditionExpression);
>>>>>>> Stashed changes

				if (conditionExpression.Expression is { } expression)
				{
<<<<<<< Updated upstream
					_errorProcessorService.AddError(scriptExpression, GetErrorMessage(parserException));
				}

				scriptExpression = ScriptExpressionEvaluatorFactory(scriptExpression, program);
=======
					var program = Parse(expression);

					foreach (var parserException in program.Errors)
					{
						AddErrorMessage(conditionExpression, GetErrorMessage(parserException));
					}

					conditionExpression = EcmaScriptConditionExpressionEvaluatorFactory(conditionExpression, program);
				}
				else
				{
					AddErrorMessage(conditionExpression, Resources.ErrorMessage_ConditionExpressionMustBePresent);
				}
>>>>>>> Stashed changes
			}

			protected override void Visit(ref ILocationExpression locationExpression)
			{
<<<<<<< Updated upstream
				_errorProcessorService.AddError(scriptExpression, Resources.ErrorMessage_ScriptExpressionMustBePresent);
=======
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
						locationExpression = EcmaScriptLocationExpressionEvaluatorFactory(locationExpression, (program, leftExpression));
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
>>>>>>> Stashed changes
			}

<<<<<<< Updated upstream
		protected override void Visit(ref IExternalScriptExpression externalScriptExpression)
		{
			base.Visit(ref externalScriptExpression);

			externalScriptExpression = ExternalScriptExpressionEvaluatorFactory(externalScriptExpression);
		}
		
	}
}
=======
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

					scriptExpression = EcmaScriptScriptExpressionEvaluatorFactory(scriptExpression, program);
				}
				else
				{
					AddErrorMessage(scriptExpression, Resources.ErrorMessage_ScriptExpressionMustBePresent);
				}
			}

			protected override void Visit(ref IExternalScriptExpression externalScriptExpression)
			{
				base.Visit(ref externalScriptExpression);

				externalScriptExpression = EcmaScriptExternalScriptExpressionEvaluatorFactory(externalScriptExpression);
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

				externalDataExpression = EcmaScriptExternalDataExpressionEvaluatorFactory(externalDataExpression);
			}

			private void AddErrorMessage(object entity, string message, Exception? exception = default) => EcmaScriptErrorProcessorService.AddError(entity, message, exception);
		}
	}
>>>>>>> Stashed changes
