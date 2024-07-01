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
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.Core;
using Xtate.IoC;

namespace Xtate.DataModel.EcmaScript.Test
{
	[TestClass]
	public class DataModelTest
	{
		private ChannelReader<IEvent> _eventChannel = default!;
<<<<<<< Updated upstream
		private Mock<ILoggerOld>         _logger       = default!;
		private Mock<ILogWriter>         _logger2       = default!;
		//private InterpreterOptions    _options      = default!;

		private static string NullDataModel(string xml) => "<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='null'>" + xml + "</scxml>";

		private static string EcmaScriptDataModel(string xml) => "<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + xml + "</scxml>";

		private static string NoNameOnEntry(string xml) =>
			"<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'><datamodel><data id='my'/></datamodel><state><onentry>" + xml +
							"</onentry></state></scxml>";

		private static string WithNameOnEntry(string xml) =>
			"<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript' name='MyName'><datamodel><data id='my'/></datamodel><state><onentry>" + xml +
							"</onentry></state></scxml>";

		private async Task RunStateMachine(Func<string, string> getter, string innerXml)
=======
		private Mock<ILogger>         _logger       = default!;
		private InterpreterOptions    _options      = default!;
		/*
		private static IStateMachine GetStateMachine(string scxml)
>>>>>>> Stashed changes
		{
			// Arrange
			var services = new ServiceCollection();
			services.RegisterEcmaScriptDataModelHandler();
			services.RegisterStateMachineFactory();
			services.RegisterStateMachineInterpreter();

			services.AddForwarding<IScxmlStateMachine>(_ => new ScxmlStateMachine(getter(innerXml)));
			services.AddForwarding(_ => _logger2.Object);

			var provider = services.BuildProvider();

			var stateMachineInterpreter = await provider.GetRequiredService<IStateMachineInterpreter>();
			var eventQueueWriter = await provider.GetRequiredService<IEventQueueWriter>();
			eventQueueWriter.Complete();

			// Act

			//var result = await stateMachineInterpreter.RunAsync();d

			/*


			// arrange
			var stateMachine = getter(innerXml);
			var options = new InterpreterOptions(ServiceLocator.Create(
												  delegate (IServiceCollection s)
												  {
													  s.AddXPath();
													  s.AddEcmaScript();
													  s.AddForwarding(_ => stateMachine);
												  }))
					   {
						   Logger = _logger.Object
					   };
			*/
			// act
			//await stateMachineInterpreter.RunAsync();
			async Task Action() => await stateMachineInterpreter.RunAsync();

			// assert
			await Assert.ThrowsExceptionAsync<StateMachineQueueClosedException>(Action);
		}

		private async Task RunStateMachineWithError(Func<string, string> getter, string innerXml)
		{
			// arrange
			var services = new ServiceCollection();
			services.RegisterEcmaScriptDataModelHandler();
			services.RegisterStateMachineFactory();
			services.RegisterStateMachineInterpreter();

			services.AddForwarding<IScxmlStateMachine>(_ => new ScxmlStateMachine(getter(innerXml)));
			services.AddForwarding(_ => _logger2.Object);

			var provider = services.BuildProvider();

			var stateMachineInterpreter = await provider.GetRequiredService<IStateMachineInterpreter>();
			var eventQueueWriter = await provider.GetRequiredService<IEventQueueWriter>();
			eventQueueWriter.Complete();

			/*
			// arrange
			var stateMachine = getter(innerXml);
			var options = new InterpreterOptions(ServiceLocator.Create(
													 delegate (IServiceCollection s)
													 {
														 s.AddXPath();
														 s.AddEcmaScript();
														 s.AddForwarding(_ => stateMachine);
													 }))
						  {
							  Logger = _logger.Object
						  };
			*/

			// act
			async Task Action() => await stateMachineInterpreter.RunAsync();

			// assert
			await Assert.ThrowsExceptionAsync<StateMachineDestroyedException>(Action);
		}

		[TestInitialize]
		public void Init()
		{
			var channel = Channel.CreateUnbounded<IEvent>();
			channel.Writer.Complete();
			_eventChannel = channel.Reader;
			/*
			_logger = new Mock<ILogger>();
			_logger.Setup(e => e.ExecuteLogOld(LogLevel.Info, "MyName", It.IsAny<DataModelValue>(), default))
				   .Callback((ILoggerContext _,
							  LogLevel _,
							  string lbl,
							  object prm,
							  Exception? _,
							  CancellationToken _) => Console.WriteLine(lbl + @":" + prm));
			_logger.SetupGet(e => e.IsTracingEnabled).Returns(false);*/

			_logger2 = new Mock<ILogWriter>();
			_logger2.Setup(e => e.IsEnabled(Level.Info)).Returns(true);
			_logger2.Setup(e => e.IsEnabled(Level.Error)).Returns(true);
			_logger2.Setup(l => l.IsEnabled(Level.Trace)).Returns(false);
		}

		[TestMethod]
		public async Task LogWriteTest()
		{
			await RunStateMachine(NoNameOnEntry, innerXml: "<log label='output'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", "output", It.IsAny<IEnumerable<LoggingParameter>>()), Times.Once);
		}

		private static DataModelValue GetDMV(IEnumerable<LoggingParameter> parameters)
		{
			foreach (var loggingParameter in parameters)
			{
				if (loggingParameter.Name == "Parameter")
				{
					return (DataModelValue) loggingParameter.Value!;
				}
			}

			return default;
		}

		private static Exception GetException(IEnumerable<LoggingParameter> parameters)
		{
			foreach (var loggingParameter in parameters)
			{
				if (loggingParameter.Name == "Exception")
				{
					return (Exception) loggingParameter.Value!;
				}
			}

			return default!;
		}

		[TestMethod]
		public async Task LogExpressionWriteTest()
		{
			await RunStateMachine(NoNameOnEntry, innerXml: "<log expr=\"'output'\"/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v).AsString() == "output")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task LogSessionIdWriteTest()
		{
			await RunStateMachine(NoNameOnEntry, innerXml: "<log expr='_sessionid'/>");
						
			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v).AsString().Length > 0)), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task LogNameWriteTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_name'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "MyName")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task LogNullNameWriteTest()
		{
			await RunStateMachine(NoNameOnEntry, innerXml: "<log expr='_name'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == DataModelValue.Null)), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task LogNonExistedTest()
		{
			await RunStateMachineWithError(NoNameOnEntry, innerXml: "<log expr='_not_existed'/>");

			_logger2.Verify(l => l.Write(Level.Error, "IStateMachineInterpreter", "Execution error in entity '(#7)'.", It.Is<IEnumerable<LoggingParameter>>(v => GetException(v).Message == "_not_existed is not defined")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Error));
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ExecutionBlockWithErrorInsideTest()
		{
			await RunStateMachineWithError(WithNameOnEntry, innerXml: "<log expr='_name'/><log expr='_not_existed'/><log expr='_name'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v).AsString() == "MyName")), Times.Once);
			_logger2.Verify(l => l.Write(Level.Error, "IStateMachineInterpreter", "Execution error in entity '(#9)'.", It.Is<IEnumerable<LoggingParameter>>(v => GetException(v).Message == "_not_existed is not defined")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info));
			_logger2.Verify(l => l.IsEnabled(Level.Error));
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task InterpreterVersionVariableTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.interpreter.version'/>");

			var version = typeof(StateMachineInterpreter).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == version)), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task InterpreterNameVariableTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.interpreter.name'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "Xtate.Core.StateMachineInterpreter")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task DataModelNameVariableTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.datamodel.name'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "Xtate.DataModel.EcmaScript.EcmaScriptDataModelHandler")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task DataModelAssemblyVariableTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.datamodel.assembly'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "Xtate.DataModel.EcmaScript")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task DataModelVersionVariableTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.datamodel.version'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v).AsString().Length > 0)), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task JintVersionVariableTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.datamodel.vars.JintVersion'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "2.11.58")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ExecutionScriptTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<script>my='1'+'a';</script><log expr='my'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "1a")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task SimpleAssignTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<assign location='x' expr='\"Hello World\"'/><log expr='x'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "Hello World")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ComplexAssignTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<script>my=[]; my[3]={};</script><assign location='my[3].yy' expr=\"'Hello World'\"/><log expr='my[3].yy'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "Hello World")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task UserAssignTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<assign location='_name1' expr=\"'Hello World'\"/><log expr='_name1'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "Hello World")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task SystemAssignTest()
		{
			await RunStateMachineWithError(WithNameOnEntry, innerXml: "<assign location='_name' expr=\"'Hello World'\"/>");

			_logger2.Verify(l => l.Write(Level.Error, "IStateMachineInterpreter", "Execution error in entity '(#7)'.", It.Is<IEnumerable<LoggingParameter>>(v => GetException(v).Message == "Object can not be modified.")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Error));
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task IfTrueTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='1==1'><log expr=\"'Hello World'\"/></if>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "Hello World")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task IfFalseTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='1==0'><log expr=\"'Hello World'\"/></if>");

			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task IfElseTrueTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='true'><log expr=\"'Hello World'\"/><else/><log expr=\"'Bye World'\"/></if>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "Hello World")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task IfElseFalseTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='false'><log expr=\"'Hello World'\"/><else/><log expr=\"'Bye World'\"/></if>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "Bye World")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task SwitchTest()
		{
			await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='false'><log expr=\"'Hello World'\"/><elseif cond='true'/><log expr=\"'Maybe World'\"/><else/><log expr=\"'Bye World'\"/></if>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "Maybe World")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ForeachNoIndexTest()
		{
			await RunStateMachine(WithNameOnEntry, "<script>my=[]; my[0]='aaa'; my[1]='bbb'</script><foreach array='my' item='itm'>"
												   + "<log expr=\"itm\"/></foreach><log expr='typeof(itm)'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "aaa")), Times.Once);
			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "bbb")), Times.Once);
			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "undefined")), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Exactly(3));
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ForeachWithIndexTest()
		{
			await RunStateMachine(WithNameOnEntry, "<script>my=[]; my[0]='aaa'; my[1]='bbb'</script><foreach array='my' item='itm' index='idx'>"
												   + "<log expr=\"idx + '-' + itm\"/></foreach><log expr='typeof(itm)'/><log expr='typeof(idx)'/>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "0-aaa")), Times.Once);
			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "1-bbb")), Times.Once);
			_logger2.Verify(l => l.Write(Level.Info, "ILog", null, It.Is<IEnumerable<LoggingParameter>>(v => GetDMV(v) == "undefined")), Times.Exactly(2));
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Exactly(4));
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task NullDataModelTransitionWithConditionTrueTest()
		{
			await RunStateMachine(NullDataModel, innerXml: "<state id='s1'><transition cond='In(s1)' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", "Hello", It.IsAny<IEnumerable<LoggingParameter>>()), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task NullDataModelTransitionWithConditionFalseTest()
		{
			await RunStateMachine(NullDataModel, innerXml: "<state id='s1'><transition cond='In(s2)' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task EcmaScriptDataModelTransitionWithConditionTrueTest()
		{
			await RunStateMachine(EcmaScriptDataModel, innerXml: "<state id='s1'><transition cond=\"In('s1')\" target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger2.Verify(l => l.Write(Level.Info, "ILog", "Hello", It.IsAny<IEnumerable<LoggingParameter>>()), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Info), Times.Once);
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task EcmaScriptDataModelTransitionWithConditionFalseTest()
		{
			await RunStateMachine(EcmaScriptDataModel, innerXml: "<state id='s1'><transition cond=\"In('s2')\" target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

<<<<<<< Updated upstream
			_logger2.Verify(l => l.IsEnabled(Level.Trace));
			_logger2.VerifyNoOtherCalls();
		}
=======
			_logger.VerifyGet(l => l.IsTracingEnabled);
			_logger.VerifyNoOtherCalls();
		}*/
>>>>>>> Stashed changes
	}
}