// Copyright © 2019-2026 Sergii Artemenko
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

using System.Collections.Immutable;
using System.Reflection;
using Acornima;
using Xtate.DataModel.EcmaScript.Properties;
using Xtate.DataModel.Services;
using Xtate.DataTypes;
using Xtate.StateMachine;
using Xtate.StateMachine.Validator;

namespace Xtate.DataModel.EcmaScript.Services;

[InstantiatedByIoC]
public class EcmaScriptDataModelHandler : DataModelHandlerBase
{
	[InstantiatedByIoC]
	public class Provider() : DataModelHandlerProviderBase<EcmaScriptDataModelHandler>(@"ecmascript");

	public static readonly string JintVersionValue = typeof(Engine).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? @"(unknown)";

	private readonly CollectingParseErrorHandler _errorHandler;

	private readonly Parser _parser;

	public EcmaScriptDataModelHandler()
	{
		_errorHandler = new CollectingParseErrorHandler();
		_parser = new Parser(new ParserOptions { Tolerant = true, ErrorHandler = _errorHandler });
	}

	public required Func<IForEach, EcmaScriptForEachEvaluator> EcmaScriptForEachEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<ICustomAction, EcmaScriptCustomActionEvaluator> EcmaScriptCustomActionEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<IExternalScriptExpression, EcmaScriptExternalScriptExpressionEvaluator> EcmaScriptExternalScriptExpressionEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<IExternalDataExpression, EcmaScriptExternalDataExpressionEvaluator> EcmaScriptExternalDataExpressionEvaluatorFactory { private get; [SetByIoC] init; }

	public required IErrorProcessorService<EcmaScriptDataModelHandler> EcmaScriptErrorProcessorService { private get; [SetByIoC] init; }

	public required Func<IValueExpression, Prepared<Script>, EcmaScriptValueExpressionEvaluator> EcmaScriptValueExpressionEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<IConditionExpression, Prepared<Script>, EcmaScriptConditionExpressionEvaluator> EcmaScriptConditionExpressionEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<IScriptExpression, Prepared<Script>, EcmaScriptScriptExpressionEvaluator> EcmaScriptScriptExpressionEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<ILocationExpression, (Prepared<Script>, Expression?), EcmaScriptLocationExpressionEvaluator> EcmaScriptLocationExpressionEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<IInlineContent, EcmaScriptInlineContentEvaluator> EcmaScriptInlineContentEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<IContentBody, EcmaScriptContentBodyEvaluator> EcmaScriptContentBodyEvaluatorFactory { private get; [SetByIoC] init; }

	public override ImmutableDictionary<string, string> DataModelVars { get; } = ImmutableDictionary<string, string>.Empty.Add(key: @"JintVersion", JintVersionValue);

	public override string ConvertToText(DataModelValue value) => DataModelConverter.ToJson(value, DataModelConverter.JsonOptions.WriteIndented | DataModelConverter.JsonOptions.UndefinedToSkipOrNull);

	private (Prepared<Script> Program, IReadOnlyList<ParseError> Errors) Parse(string source)
	{
		_ = _parser.ParseScript(source);

		return (Engine.PrepareScript(source), _errorHandler.Errors.ToArray());
	}

	private static string GetErrorMessage(ParseError error) => @$"{error} ({error.Description}). Ln: {error.LineNumber}. Col: {error.Column + 1}.";

	protected override void Visit(ref IForEach forEach)
	{
		base.Visit(ref forEach);

		forEach = EcmaScriptForEachEvaluatorFactory(forEach);
	}

	protected override void Visit(ref ICustomAction customAction)
	{
		base.Visit(ref customAction);

		customAction = EcmaScriptCustomActionEvaluatorFactory(customAction);
	}

	protected override void Visit(ref IValueExpression valueExpression)
	{
		base.Visit(ref valueExpression);

		if (valueExpression.Expression is { } expression)
		{
			var (program, errors) = Parse(expression);

			foreach (var parserException in errors)
			{
				AddErrorMessage(valueExpression, GetErrorMessage(parserException));
			}

			valueExpression = EcmaScriptValueExpressionEvaluatorFactory(valueExpression, program);
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
			var (program, errors) = Parse(expression);

			foreach (var parserException in errors)
			{
				AddErrorMessage(conditionExpression, GetErrorMessage(parserException));
			}

			conditionExpression = EcmaScriptConditionExpressionEvaluatorFactory(conditionExpression, program);
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
			var (program, errors) = Parse(expression);

			foreach (var parserException in errors)
			{
				AddErrorMessage(locationExpression, GetErrorMessage(parserException));
			}

			var leftExpression = EcmaScriptLocationExpressionEvaluator.GetLeftExpression(program.Program!);

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
	}

	protected override void Visit(ref IScriptExpression scriptExpression)
	{
		base.Visit(ref scriptExpression);

		if (scriptExpression.Expression is { } expression)
		{
			var (program, errors) = Parse(expression);

			foreach (var parserException in errors)
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

		inlineContent = EcmaScriptInlineContentEvaluatorFactory(inlineContent);
	}

	protected override void Visit(ref IContentBody contentBody)
	{
		base.Visit(ref contentBody);

		contentBody = EcmaScriptContentBodyEvaluatorFactory(contentBody);
	}

	protected override void Visit(ref IExternalDataExpression externalDataExpression)
	{
		base.Visit(ref externalDataExpression);

		externalDataExpression = EcmaScriptExternalDataExpressionEvaluatorFactory(externalDataExpression);
	}

	private void AddErrorMessage(object entity, string message, Exception? exception = default) => EcmaScriptErrorProcessorService.AddError(entity, message, exception);

	private sealed class CollectingParseErrorHandler : ParseErrorHandler
	{
		public List<ParseError> Errors { get; } = [];

		protected override void RecordError(ParseError error) => Errors.Add(error);

		protected override void Reset() => Errors.Clear();
	}
}