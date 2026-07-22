// Copyright © 2019-2026 Sergii Artemenko

using System;
using System.Collections.Generic;
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
		StringAssert.Contains(text, "\n");
		StringAssert.Contains(text, "\"text\": \"value\"");
	}

	[TestMethod]
	public void HandlerWrapsEveryExpressionAndContentKind()
	{
		var handler = CreateHandler(Mock.Of<IErrorProcessorService<EcmaScriptDataModelHandler>>());
		IValueExpression value = Mock.Of<IValueExpression>(expression => expression.Expression == "1 + 1");
		IConditionExpression condition = Mock.Of<IConditionExpression>(expression => expression.Expression == "true");
		ILocationExpression location = Mock.Of<ILocationExpression>(expression => expression.Expression == "target");
		IScriptExpression script = Mock.Of<IScriptExpression>(expression => expression.Expression == "var x = 1");
		IExternalScriptExpression externalScript = Mock.Of<IExternalScriptExpression>();
		IExternalDataExpression externalData = Mock.Of<IExternalDataExpression>();
		IInlineContent inline = Mock.Of<IInlineContent>();
		IContentBody body = Mock.Of<IContentBody>();

		handler.Process(ref value);
		handler.Process(ref condition);
		handler.Process(ref location);
		handler.Process(ref script);
		handler.Process(ref externalScript);
		handler.Process(ref externalData);
		handler.Process(ref inline);
		handler.Process(ref body);

		Assert.IsInstanceOfType<EcmaScriptValueExpressionEvaluator>(value);
		Assert.IsInstanceOfType<EcmaScriptConditionExpressionEvaluator>(condition);
		Assert.IsInstanceOfType<EcmaScriptLocationExpressionEvaluator>(location);
		Assert.IsInstanceOfType<EcmaScriptScriptExpressionEvaluator>(script);
		Assert.IsInstanceOfType<EcmaScriptExternalScriptExpressionEvaluator>(externalScript);
		Assert.IsInstanceOfType<EcmaScriptExternalDataExpressionEvaluator>(externalData);
		Assert.IsInstanceOfType<EcmaScriptInlineContentEvaluator>(inline);
		Assert.IsInstanceOfType<EcmaScriptContentBodyEvaluator>(body);
	}

	[TestMethod]
	public void HandlerReportsMissingExpressionsAndInvalidLocations()
	{
		var errors = new Mock<IErrorProcessorService<EcmaScriptDataModelHandler>>();
		var handler = CreateHandler(errors.Object);
		IValueExpression value = Mock.Of<IValueExpression>();
		IConditionExpression condition = Mock.Of<IConditionExpression>();
		ILocationExpression location = Mock.Of<ILocationExpression>();
		IScriptExpression script = Mock.Of<IScriptExpression>();

		handler.Process(ref value);
		handler.Process(ref condition);
		handler.Process(ref location);
		handler.Process(ref script);

		errors.Verify(processor => processor.AddError(It.IsAny<object>(), It.IsAny<string>(), null), Times.Exactly(4));

		ILocationExpression binary = Mock.Of<ILocationExpression>(expression => expression.Expression == "1 + 2");
		handler.Process(ref binary);
		errors.Verify(processor => processor.AddError(binary, It.IsAny<string>(), null), Times.Once);
	}

	[TestMethod]
	public void HandlerReportsParserErrorsAndClearsThemBetweenParses()
	{
		var errors = new Mock<IErrorProcessorService<EcmaScriptDataModelHandler>>();
		var handler = CreateHandler(errors.Object);
		IValueExpression invalid = Mock.Of<IValueExpression>(expression => expression.Expression == "let = ;");
		IValueExpression valid = Mock.Of<IValueExpression>(expression => expression.Expression == "1");

		handler.Process(ref invalid);
		var countAfterInvalid = errors.Invocations.Count;
		Assert.IsGreaterThan(0, countAfterInvalid);
		handler.Process(ref valid);
		Assert.AreEqual(countAfterInvalid, errors.Invocations.Count);
	}

	[TestMethod]
	public void HandlerCollectsRecoverableParserDiagnostics()
	{
		var errors = new Mock<IErrorProcessorService<EcmaScriptDataModelHandler>>();
		var handler = CreateHandler(errors.Object);
		IValueExpression invalid = Mock.Of<IValueExpression>(expression => expression.Expression == "true ?? false || true");

		handler.Process(ref invalid);

		errors.Verify(processor => processor.AddError(It.IsAny<object>(), It.Is<string>(message => message.Contains("Ln:", StringComparison.Ordinal)), null), Times.AtLeastOnce);
	}

	[TestMethod]
	public void HandlerReportsRecoverableDiagnosticsForEveryExpressionKind()
	{
		var errors = new Mock<IErrorProcessorService<EcmaScriptDataModelHandler>>();
		var handler = CreateHandler(errors.Object);
		IValueExpression value = Mock.Of<IValueExpression>(expression => expression.Expression == "true ?? false || true");
		IConditionExpression condition = Mock.Of<IConditionExpression>(expression => expression.Expression == "true ?? false || true");
		ILocationExpression location = Mock.Of<ILocationExpression>(expression => expression.Expression == "true ?? false || true");
		IScriptExpression script = Mock.Of<IScriptExpression>(expression => expression.Expression == "true ?? false || true");

		handler.Process(ref value);
		handler.Process(ref condition);
		handler.Process(ref location);
		handler.Process(ref script);

		errors.Verify(processor => processor.AddError(It.IsAny<object>(), It.IsAny<string>(), null), Times.Exactly(4));
	}

	[TestMethod]
	public void HandlerWrapsCustomActionsAfterTheDefaultContainerAndEvaluator()
	{
		var handler = CreateHandler(Mock.Of<IErrorProcessorService<EcmaScriptDataModelHandler>>());
		ICustomAction customAction = Mock.Of<ICustomAction>();

		handler.Process(ref customAction);

		Assert.IsInstanceOfType<EcmaScriptCustomActionEvaluator>(customAction);
	}

	private static TestEcmaScriptDataModelHandler CreateHandler(IErrorProcessorService<EcmaScriptDataModelHandler> errorProcessor) =>
		new()
		{
			EcmaScriptErrorProcessorService = errorProcessor,
			EcmaScriptForEachEvaluatorFactory = entity => new EcmaScriptForEachEvaluator(entity) { EngineFactory = null! },
			EcmaScriptCustomActionEvaluatorFactory = entity => new EcmaScriptCustomActionEvaluator(entity) { EngineFactory = null! },
			EcmaScriptExternalScriptExpressionEvaluatorFactory = entity => new EcmaScriptExternalScriptExpressionEvaluator(entity) { EngineFactory = null! },
			EcmaScriptExternalDataExpressionEvaluatorFactory = entity => new EcmaScriptExternalDataExpressionEvaluator(entity) { DataConverter = null!, ResourceLoader = null! },
			EcmaScriptValueExpressionEvaluatorFactory = (entity, program) => new EcmaScriptValueExpressionEvaluator(entity, program) { EngineFactory = null! },
			EcmaScriptConditionExpressionEvaluatorFactory = (entity, program) => new EcmaScriptConditionExpressionEvaluator(entity, program) { EngineFactory = null! },
			EcmaScriptScriptExpressionEvaluatorFactory = (entity, program) => new EcmaScriptScriptExpressionEvaluator(entity, program) { EngineFactory = null! },
			EcmaScriptLocationExpressionEvaluatorFactory = (entity, args) => new EcmaScriptLocationExpressionEvaluator(entity, args.Item1, args.Item2) { EngineFactory = null! },
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
		action.Setup(static item => item.GetValues()).Returns(Array.Empty<IActionValue>());
		action.Setup(static item => item.GetLocations()).Returns(Array.Empty<IActionLocation>());

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

		public void Process(ref ICustomAction customAction) => Visit(ref customAction);
	}
}
