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

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.DataModel.EcmaScript.Services;
using Xtate.DataModel.Services;
using Xtate.DataTypes;
using Xtate.StateMachine;
using Xtate.StateMachine.Validator;

namespace Xtate.DataModel.EcmaScript.Test.UnitTests;

[TestClass]
public class DataModelHandlerCoverageTest
{
	[TestMethod]
	public void HandlerPublishesVersionAndConvertsValuesToIndentedJson()
	{
		var handler = CreateHandler(Mock.Of<IErrorProcessorService<EcmaScriptDataModelHandler>>());
		var value = new DataModelList { ["text"] = "value", ["number"] = 7 };

		Assert.AreEqual(EcmaScriptDataModelHandler.JintVersionValue, handler.DataModelVars["JintVersion"]);
		var text = handler.ConvertToText(value);
		StringAssert.Contains(text, substring: "\n");
		StringAssert.Contains(text, substring: "\"text\": \"value\"");
	}

	[TestMethod]
	public void HandlerWrapsEveryExpressionAndContentKind()
	{
		var handler = CreateHandler(Mock.Of<IErrorProcessorService<EcmaScriptDataModelHandler>>());
		var value = Mock.Of<IValueExpression>(expression => expression.Expression == "1 + 1");
		var condition = Mock.Of<IConditionExpression>(expression => expression.Expression == "true");
		var location = Mock.Of<ILocationExpression>(expression => expression.Expression == "target");
		var location2 = Mock.Of<ILocationExpression>(expression => expression.Expression == "target.nested");
		var script = Mock.Of<IScriptExpression>(expression => expression.Expression == "var x = 1");
		var externalScript = Mock.Of<IExternalScriptExpression>();
		var externalData = Mock.Of<IExternalDataExpression>();
		var inline = Mock.Of<IInlineContent>();
		var body = Mock.Of<IContentBody>();

		handler.Process(ref value);
		handler.Process(ref condition);
		handler.Process(ref location);
		handler.Process(ref location2);
		handler.Process(ref script);
		handler.Process(ref externalScript);
		handler.Process(ref externalData);
		handler.Process(ref inline);
		handler.Process(ref body);

		Assert.IsInstanceOfType<EcmaScriptValueExpressionEvaluator>(value);
		Assert.IsInstanceOfType<EcmaScriptConditionExpressionEvaluator>(condition);
		Assert.IsInstanceOfType<EcmaScriptLocationExpressionEvaluator>(location);
		Assert.IsInstanceOfType<EcmaScriptLocationExpressionEvaluator>(location2);
		Assert.IsInstanceOfType<EcmaScriptScriptExpressionEvaluator>(script);
		Assert.IsInstanceOfType<EcmaScriptExternalScriptExpressionEvaluator>(externalScript);
		Assert.IsInstanceOfType<EcmaScriptExternalDataExpressionEvaluator>(externalData);
		Assert.IsInstanceOfType<EcmaScriptInlineContentEvaluator>(inline);
		Assert.IsInstanceOfType<EcmaScriptContentBodyEvaluator>(body);
	}

	[TestMethod]
	public void HandlerReportsMissingExpressions()
	{
		var errors = new Mock<IErrorProcessorService<EcmaScriptDataModelHandler>>();
		var handler = CreateHandler(errors.Object);
		var value = Mock.Of<IValueExpression>();
		var condition = Mock.Of<IConditionExpression>();
		var location = Mock.Of<ILocationExpression>();
		var script = Mock.Of<IScriptExpression>();

		handler.Process(ref value);
		handler.Process(ref condition);
		handler.Process(ref location);
		handler.Process(ref script);

		errors.Verify(processor => processor.AddError(It.IsAny<object>(), It.IsAny<string>(), null), Times.Exactly(4));
	}

	[TestMethod]
	public void HandlerReportsInvalidLocations()
	{
		var errors = new Mock<IErrorProcessorService<EcmaScriptDataModelHandler>>();
		var handler = CreateHandler(errors.Object);
		var binary = Mock.Of<ILocationExpression>(expression => expression.Expression == "1 + 2");

		handler.Process(ref binary);

		errors.Verify(processor => processor.AddError(binary, It.IsAny<string>(), null), Times.Once);
	}

	[TestMethod]
	public void HandlerReportsParserErrorsAndClearsThemBetweenParses()
	{
		var errors = new Mock<IErrorProcessorService<EcmaScriptDataModelHandler>>();
		var handler = CreateHandler(errors.Object);
		var invalid = Mock.Of<IValueExpression>(expression => expression.Expression == "let = ;");
		var valid = Mock.Of<IValueExpression>(expression => expression.Expression == "1");

		handler.Process(ref invalid);
		var countAfterInvalid = errors.Invocations.Count;
		Assert.IsGreaterThan(lowerBound: 0, countAfterInvalid);
		handler.Process(ref valid);
		Assert.AreEqual(countAfterInvalid, errors.Invocations.Count);
	}

	[TestMethod]
	public void HandlerCollectsRecoverableParserDiagnostics()
	{
		var errors = new Mock<IErrorProcessorService<EcmaScriptDataModelHandler>>();
		var handler = CreateHandler(errors.Object);
		var invalid = Mock.Of<IValueExpression>(expression => expression.Expression == "true ?? false || true");

		handler.Process(ref invalid);

		errors.Verify(processor => processor.AddError(It.IsAny<object>(), It.Is<string>(message => message.Contains("Line:", StringComparison.Ordinal)), null), Times.AtLeastOnce);
	}

	[TestMethod]
	public void HandlerReportsRecoverableDiagnosticsForEveryExpressionKind()
	{
		var errors = new Mock<IErrorProcessorService<EcmaScriptDataModelHandler>>();
		var handler = CreateHandler(errors.Object);
		var value = Mock.Of<IValueExpression>(expression => expression.Expression == "true ?? false || true");
		var condition = Mock.Of<IConditionExpression>(expression => expression.Expression == "true ?? false || true");
		var location = Mock.Of<ILocationExpression>(expression => expression.Expression == "true ?? false || true");
		var script = Mock.Of<IScriptExpression>(expression => expression.Expression == "true ?? false || true");

		handler.Process(ref value);
		handler.Process(ref condition);
		handler.Process(ref location);
		handler.Process(ref script);

		errors.Verify(processor => processor.AddError(It.IsAny<object>(), It.IsAny<string>(), null), Times.Exactly(4));
	}

	private static TestEcmaScriptDataModelHandler CreateHandler(IErrorProcessorService<EcmaScriptDataModelHandler> errorProcessor) =>
		new()
		{
			EcmaScriptErrorProcessorService = errorProcessor,
			EcmaScriptForEachEvaluatorFactory = entity => new EcmaScriptForEachEvaluator(entity),
			EcmaScriptExternalScriptExpressionEvaluatorFactory = entity => new EcmaScriptExternalScriptExpressionEvaluator(entity) { EngineFactory = null! },
			EcmaScriptExternalDataExpressionEvaluatorFactory = entity => new EcmaScriptExternalDataExpressionEvaluator(entity) { DataConverter = null!, ResourceLoader = null! },
			EcmaScriptValueExpressionEvaluatorFactory = (entity, program) => new EcmaScriptValueExpressionEvaluator(entity, program) { EngineFactory = null! },
			EcmaScriptConditionExpressionEvaluatorFactory = (entity, program) => new EcmaScriptConditionExpressionEvaluator(entity, program) { EngineFactory = null! },
			EcmaScriptScriptExpressionEvaluatorFactory = (entity, program) => new EcmaScriptScriptExpressionEvaluator(entity, program) { EngineFactory = null! },
			EcmaScriptLocationExpressionEvaluatorFactory = (entity, program) => new EcmaScriptLocationExpressionEvaluator(entity, program) { EngineFactory = null! },
			EcmaScriptInlineContentEvaluatorFactory = entity => new EcmaScriptInlineContentEvaluator(entity) { Logger = null! },
			EcmaScriptContentBodyEvaluatorFactory = entity => new EcmaScriptContentBodyEvaluator(entity) { Logger = null! },
			DefaultLogEvaluatorFactory = _ => null!,
			DefaultSendEvaluatorFactory = _ => null!,
			DefaultCancelEvaluatorFactory = _ => null!,
			DefaultIfEvaluatorFactory = _ => null!,
			DefaultRaiseEvaluatorFactory = _ => null!,
			DefaultForEachEvaluatorFactory = _ => null!,
			DefaultAssignEvaluatorFactory = _ => null!,
			DefaultScriptEvaluatorFactory = _ => null!,
			DefaultCustomActionEvaluatorFactory = entity => new DefaultCustomActionEvaluator(entity),
			DefaultContentBodyEvaluatorFactory = _ => null!,
			DefaultInlineContentEvaluatorFactory = _ => null!,
			DefaultExternalDataExpressionEvaluatorFactory = _ => null!,
			CustomActionContainerFactory = entity => new CustomActionContainer(entity, _ => CreateAction())
		};

	private static IAction CreateAction()
	{
		var action = new Mock<IAction>();
		action.Setup(static item => item.GetValues()).Returns([]);
		action.Setup(static item => item.GetLocations()).Returns([]);

		return action.Object;
	}

	private sealed class TestEcmaScriptDataModelHandler : EcmaScriptDataModelHandler
	{
		public void Process(ref IValueExpression expression) => Visit(ref expression);

		public void Process(ref IConditionExpression expression) => Visit(ref expression);

		public void Process(ref ILocationExpression expression) => Visit(ref expression);

		public void Process(ref IScriptExpression expression) => Visit(ref expression);

		public void Process(ref IExternalScriptExpression expression) => Visit(ref expression);

		public void Process(ref IExternalDataExpression expression) => Visit(ref expression);

		public void Process(ref IInlineContent content) => Visit(ref content);

		public void Process(ref IContentBody content) => Visit(ref content);
	}
}