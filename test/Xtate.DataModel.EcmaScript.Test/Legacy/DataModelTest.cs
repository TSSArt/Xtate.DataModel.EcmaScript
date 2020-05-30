using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Xtate.DataModel.EcmaScript.Test
{
	[TestClass]
	[SuppressMessage(category: "ReSharper", checkId: "RedundantCapturedContext")]
	public class DataModelTest
	{
		private ChannelReader<IEvent> _eventChannel = default!;
		private Mock<ILogger>         _logger       = default!;
		private InterpreterOptions    _options;

		private static IStateMachine GetStateMachine(string scxml)
		{
			using var textReader = new StringReader(scxml);
			using var reader = XmlReader.Create(textReader);
			return new ScxmlDirector(reader, BuilderFactory.Instance, DefaultErrorProcessor.Instance).ConstructStateMachine(StateMachineValidator.Instance);
		}

		private static IStateMachine NoneDataModel(string xml) => GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='none'>" + xml + "</scxml>");

		private static IStateMachine EcmaScriptDataModel(string xml) => GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + xml + "</scxml>");

		private static IStateMachine NoNameOnEntry(string xml) =>
				GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'><datamodel><data id='my'/></datamodel><state><onentry>" + xml +
								"</onentry></state></scxml>");

		private static IStateMachine WithNameOnEntry(string xml) =>
				GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript' name='MyName'><datamodel><data id='my'/></datamodel><state><onentry>" + xml +
								"</onentry></state></scxml>");

		private Task RunStateMachine(Func<string, IStateMachine> getter, string innerXml)
		{
			// arrange
			var stateMachine = getter(innerXml);

			// act
			async Task Action() => await StateMachineInterpreter.RunAsync(stateMachine, _eventChannel, _options);

			// assert
			return Assert.ThrowsExceptionAsync<StateMachineQueueClosedException>(Action);
		}

		private Task RunStateMachineWithError(Func<string, IStateMachine> getter, string innerXml)
		{
			// arrange
			var stateMachine = getter(innerXml);

			// act
			async Task Action() => await StateMachineInterpreter.RunAsync(stateMachine, _eventChannel, _options);

			// assert
			return Assert.ThrowsExceptionAsync<StateMachineDestroyedException>(Action);
		}

		[TestInitialize]
		public void Init()
		{
			var channel = Channel.CreateUnbounded<IEvent>();
			channel.Writer.Complete();
			_eventChannel = channel.Reader;

			_options = new InterpreterOptions { DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory) };
			_logger = new Mock<ILogger>();
			_logger.Setup(e => e.ExecuteLog(It.IsAny<ILoggerContext>(), "MyName", It.IsAny<DataModelValue>(), It.IsAny<CancellationToken>()))
				   .Callback((ILoggerContext context, string lbl, object prm, CancellationToken _) => Console.WriteLine(lbl + @":" + prm));
			_logger.SetupGet(e => e.IsTracingEnabled).Returns(false);
			_options.Logger = _logger.Object;
		}

		[TestMethod]
		public async Task LogWriteTest()
		{
			await RunStateMachine(NoNameOnEntry, innerXml: "<log label='output'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), "output", default, default), Times.Once);
		}

		[TestMethod]
		public async Task LogExpressionWriteTest()
		{
			await RunStateMachine(NoNameOnEntry, innerXml: "<log expr=\"'output'\"/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("output"), default), Times.Once);
		}

		[TestMethod]
		public async Task LogSessionIdWriteTest()
		{
			await RunStateMachine(NoNameOnEntry, innerXml: "<log expr='_sessionid'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, It.Is<DataModelValue>(v => v.AsString() != null), default), Times.Once);
		}

		[TestMethod]
		public async Task LogNameWriteTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_name'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("MyName"), default), Times.Once);
		}

		[TestMethod]
		public async Task LogNullNameWriteTest()
		{
			await RunStateMachine(NoNameOnEntry, innerXml: "<log expr='_name'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, DataModelValue.Null, default), Times.Once);
		}

		[TestMethod]
		public async Task LogNonExistedTest()
		{
			await RunStateMachineWithError(NoNameOnEntry, innerXml: "<log expr='_not_existed'/>");

			_logger.Verify(l => l.LogError(It.IsAny<ILoggerContext>(), ErrorType.Execution, It.IsAny<Exception>(), "(#7)", It.IsAny<CancellationToken>()), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ExecutionBlockWithErrorInsideTest()
		{
			await RunStateMachineWithError(WithNameOnEntry, innerXml: "<log expr='_name'/><log expr='_not_existed'/><log expr='_name'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("MyName"), default), Times.Once);
			_logger.Verify(l => l.LogError(It.IsAny<ILoggerContext>(), ErrorType.Execution, It.IsNotNull<Exception>(), "(#9)", default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task InterpreterVersionVariableTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.interpreter.version'/>");

			var version = typeof(StateMachineInterpreter).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue(version), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task InterpreterNameVariableTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.interpreter.name'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("Xtate.StateMachineInterpreter"), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task DataModelNameVariableTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.datamodel.name'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("Xtate.DataModel.EcmaScript.EcmaScriptDataModelHandler"), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task DataModelAssemblyVariableTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.datamodel.assembly'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("Xtate.DataModel.EcmaScript"), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task DataModelVersionVariableTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.datamodel.version'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, It.IsAny<DataModelValue>(), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task JintVersionVariableTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.datamodel.vars.JintVersion'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("2.11.58"), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ExecutionScriptTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<script>my='1'+'a';</script><log expr='my'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("1a"), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task SimpleAssignTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<assign location='x' expr='\"Hello World\"'/><log expr='x'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("Hello World"), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ComplexAssignTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<script>my=[]; my[3]={};</script><assign location='my[3].yy' expr=\"'Hello World'\"/><log expr='my[3].yy'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("Hello World"), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task UserAssignTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<assign location='_name1' expr=\"'Hello World'\"/><log expr='_name1'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("Hello World"), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task SystemAssignTest()
		{
			await RunStateMachineWithError(WithNameOnEntry, innerXml: "<assign location='_name' expr=\"'Hello World'\"/>");

			_logger.Verify(l => l.LogError(It.IsAny<ILoggerContext>(), ErrorType.Execution, It.IsNotNull<InvalidOperationException>(), "(#7)", default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task IfTrueTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='1==1'><log expr=\"'Hello World'\"/></if>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("Hello World"), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task IfFalseTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='1==0'><log expr=\"'Hello World'\"/></if>");

			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task IfElseTrueTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='true'><log expr=\"'Hello World'\"/><else/><log expr=\"'Bye World'\"/></if>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("Hello World"), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task IfElseFalseTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='false'><log expr=\"'Hello World'\"/><else/><log expr=\"'Bye World'\"/></if>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("Bye World"), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task SwitchTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='false'><log expr=\"'Hello World'\"/><elseif cond='true'/><log expr=\"'Maybe World'\"/><else/><log expr=\"'Bye World'\"/></if>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("Maybe World"), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ForeachNoIndexTest()
		{
			await RunStateMachine(WithNameOnEntry, "<script>my=[]; my[0]='aaa'; my[1]='bbb'</script><foreach array='my' item='itm'>"
												   + "<log expr=\"itm\"/></foreach><log expr='typeof(itm)'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("aaa"), default), Times.Once);
			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("bbb"), default), Times.Once);
			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("undefined"), default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ForeachWithIndexTest()
		{
			await RunStateMachine(WithNameOnEntry, "<script>my=[]; my[0]='aaa'; my[1]='bbb'</script><foreach array='my' item='itm' index='idx'>"
												   + "<log expr=\"idx + '-' + itm\"/></foreach><log expr='typeof(itm)'/><log expr='typeof(idx)'/>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("0-aaa"), default), Times.Once);
			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("1-bbb"), default), Times.Once);
			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), null, new DataModelValue("undefined"), default), Times.Exactly(2));
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task NoneDataModelTransitionWithConditionTrueTest()
		{
			await RunStateMachine(NoneDataModel, innerXml: "<state id='s1'><transition cond='In(s1)' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), "Hello", default, default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task NoneDataModelTransitionWithConditionFalseTest()
		{
			await RunStateMachine(NoneDataModel, innerXml: "<state id='s1'><transition cond='In(s2)' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task EcmaScriptDataModelTransitionWithConditionTrueTest()
		{
			await RunStateMachine(EcmaScriptDataModel, innerXml: "<state id='s1'><transition cond=\"In('s1')\" target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), "Hello", default, default), Times.Once);
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task EcmaScriptDataModelTransitionWithConditionFalseTest()
		{
			await RunStateMachine(EcmaScriptDataModel, innerXml: "<state id='s1'><transition cond=\"In('s2')\" target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}
	}
}