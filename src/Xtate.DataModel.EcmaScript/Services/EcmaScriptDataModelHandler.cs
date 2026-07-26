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

	public required Func<IForEach, EcmaScriptForEachEvaluator> EcmaScriptForEachEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<IExternalScriptExpression, EcmaScriptExternalScriptExpressionEvaluator> EcmaScriptExternalScriptExpressionEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<IExternalDataExpression, EcmaScriptExternalDataExpressionEvaluator> EcmaScriptExternalDataExpressionEvaluatorFactory { private get; [SetByIoC] init; }

	public required IErrorProcessorService<EcmaScriptDataModelHandler> EcmaScriptErrorProcessorService { private get; [SetByIoC] init; }

	public required Func<IValueExpression, EcmaScriptProgram, EcmaScriptValueExpressionEvaluator> EcmaScriptValueExpressionEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<IConditionExpression, EcmaScriptProgram, EcmaScriptConditionExpressionEvaluator> EcmaScriptConditionExpressionEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<IScriptExpression, EcmaScriptProgram, EcmaScriptScriptExpressionEvaluator> EcmaScriptScriptExpressionEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<ILocationExpression, EcmaScriptProgram, EcmaScriptLocationExpressionEvaluator> EcmaScriptLocationExpressionEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<IInlineContent, EcmaScriptInlineContentEvaluator> EcmaScriptInlineContentEvaluatorFactory { private get; [SetByIoC] init; }

	public required Func<IContentBody, EcmaScriptContentBodyEvaluator> EcmaScriptContentBodyEvaluatorFactory { private get; [SetByIoC] init; }

	public override ImmutableDictionary<string, string> DataModelVars { get; } = ImmutableDictionary<string, string>.Empty.Add(key: @"JintVersion", JintVersionValue);

	public override string ConvertToText(DataModelValue value) => DataModelConverter.ToJson(value, DataModelConverter.JsonOptions.WriteIndented | DataModelConverter.JsonOptions.UndefinedToSkipOrNull);

	private static string GetErrorMessage(ParseError error) => @$"{error} ({error.Description}). Line: {error.LineNumber}. Column: {error.Column + 1}.";

	protected override void Visit(ref IForEach forEach)
	{
		base.Visit(ref forEach);

		forEach = EcmaScriptForEachEvaluatorFactory(forEach);
	}

	protected override void Visit(ref IValueExpression valueExpression)
	{
		base.Visit(ref valueExpression);

		if (valueExpression.Expression is { } expression)
		{
			var program = EcmaScriptProgram.ParseScript(expression);

			if (!program.HasErrors)
			{
				valueExpression = EcmaScriptValueExpressionEvaluatorFactory(valueExpression, program);
			}
			else
			{
				foreach (var parserException in program.Errors)
				{
					AddErrorMessage(valueExpression, GetErrorMessage(parserException));
				}
			}
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
			var program = EcmaScriptProgram.ParseScript(expression);

			if (!program.HasErrors)
			{
				conditionExpression = EcmaScriptConditionExpressionEvaluatorFactory(conditionExpression, program);
			}
			else
			{
				foreach (var parserException in program.Errors)
				{
					AddErrorMessage(conditionExpression, GetErrorMessage(parserException));
				}
			}
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
			var program = EcmaScriptProgram.ParseLocation(expression);

			if (!program.HasErrors)
			{
				locationExpression = EcmaScriptLocationExpressionEvaluatorFactory(locationExpression, program);
			}
			else
			{
				foreach (var parserException in program.Errors)
				{
					AddErrorMessage(locationExpression, GetErrorMessage(parserException));
				}
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
			var program = EcmaScriptProgram.ParseScript(expression);

			if (!program.HasErrors)
			{
				scriptExpression = EcmaScriptScriptExpressionEvaluatorFactory(scriptExpression, program);
			}
			else
			{
				foreach (var parserException in program.Errors)
				{
					AddErrorMessage(scriptExpression, GetErrorMessage(parserException));
				}
			}
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

	private void AddErrorMessage(object entity, string message, Exception? exception = null) => EcmaScriptErrorProcessorService.AddError(entity, message, exception);
}