// Copyright © 2019-2024 Sergii Artemenko
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
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.Core;
using Xtate.IoC;

namespace Xtate.DataModel.EcmaScript.Test;

[TestClass]
public class DataModelTest
{
	private Mock<IEventQueueReader>                    _eventQueueReader = default!;
	private Mock<ILogMethods>                          _logMethods       = default!;
	private Mock<ILogWriter<IStateMachineInterpreter>> _logWriterI       = default!;
	private Mock<ILogWriter<ILog>>                     _logWriterL       = default!;

	private static async ValueTask<IStateMachine> GetStateMachine(string scxml)
	{
		var services = new ServiceCollection();
		services.AddModule<StateMachineFactoryModule>();
		services.AddConstant<IScxmlStateMachine>(new ScxmlStateMachine(scxml));
		var provider = services.BuildProvider();

		return await provider.GetRequiredService<IStateMachine>();
	}

	private static ValueTask<IStateMachine> NoneDataModel(string xml) => GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='null'>" + xml + "</scxml>");

	private static ValueTask<IStateMachine> EcmaScriptDataModel(string xml) =>
		GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + xml + "</scxml>");

	private static ValueTask<IStateMachine> NoNameOnEntry(string xml) =>
		GetStateMachine(
			"<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'><datamodel><data id='my'/></datamodel><state><onentry>" + xml +
			"</onentry></state></scxml>");

	private static ValueTask<IStateMachine> WithNameOnEntry(string xml) =>
		GetStateMachine(
			"<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript' name='MyName'><datamodel><data id='my'/></datamodel><state><onentry>" + xml +
			"</onentry></state></scxml>");

	private Task RunStateMachine(Func<string, ValueTask<IStateMachine>> getter, string innerXml) => RunStateMachineBase<StateMachineQueueClosedException>(getter, innerXml);

	private Task RunStateMachineWithError(Func<string, ValueTask<IStateMachine>> getter, string innerXml) => RunStateMachineBase<StateMachineDestroyedException>(getter, innerXml);

	private async Task RunStateMachineBase<E>(Func<string, ValueTask<IStateMachine>> getter, string innerXml) where E : Exception
	{
		var stateMachine = await getter(innerXml);

		var services = new ServiceCollection();
		services.AddModule<EcmaScriptDataModelHandlerModule>();
		services.AddModule<StateMachineInterpreterModule>();
		services.AddConstant(stateMachine);
		services.AddConstant(_logMethods.Object);
		services.AddImplementation<LogWriter<Any>>().For<ILogWriter<Any>>();
		services.AddConstant(_eventQueueReader.Object);

		//services.AddConstant(_ => _eventController.Object);
		var provider = services.BuildProvider();

		var stateMachineInterpreter = await provider.GetRequiredService<IStateMachineInterpreter>();

		try
		{
			//await stateMachineInterpreter.RunAsync(SessionId.New(), stateMachine, _eventChannel, _options);
			await stateMachineInterpreter.RunAsync();

			Assert.Fail($"{typeof(E).Name} should be raised");
		}
		catch (E)
		{
			//ignore
		}
	}

	[TestInitialize]
	public void Init()
	{
		var channel = Channel.CreateUnbounded<IEvent>();
		channel.Writer.Complete();
		_logWriterL = new Mock<ILogWriter<ILog>>();
		_logWriterL.Setup(x => x.IsEnabled(It.IsAny<Level>())).Returns(true);
		_logWriterI = new Mock<ILogWriter<IStateMachineInterpreter>>();
		_logWriterI.Setup(x => x.IsEnabled(It.IsAny<Level>())).Returns(true);
		_eventQueueReader = new Mock<IEventQueueReader>();
		_logMethods = new Mock<ILogMethods>();
	}

	[TestMethod]
	public async Task LogWriteTest()
	{
		await RunStateMachine(NoNameOnEntry, innerXml: "<log label='output'/>");

		_logMethods.Verify(l => l.Info("ILog", "output", default));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task LogExpressionWriteTest()
	{
		await RunStateMachine(NoNameOnEntry, innerXml: "<log expr=\"'output'\"/>");

		_logMethods.Verify(l => l.Info("ILog", default, "output"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task LogSessionIdWriteTest()
	{
		await RunStateMachine(NoNameOnEntry, innerXml: "<log expr='_sessionid'/>");

		_logMethods.Verify(l => l.Info("ILog", default, It.Is<DataModelValue>(v => v.AsString().Length > 0)));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task LogNameWriteTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_name'/>");

		_logMethods.Verify(l => l.Info("ILog", default, "MyName"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task LogNullNameWriteTest()
	{
		await RunStateMachine(NoNameOnEntry, innerXml: "<log expr='_name'/>");

		_logMethods.Verify(l => l.Info("ILog", default, DataModelValue.Null));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task LogNonExistedTest()
	{
		await RunStateMachineWithError(NoNameOnEntry, innerXml: "<log expr='_not_existed'/>");

		_logMethods.Verify(l => l.Error("IStateMachineInterpreter", "Execution error in entity '(#-1)'.", It.Is<Exception>(e => e.Message == "_not_existed is not defined")));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task ExecutionBlockWithErrorInsideTest()
	{
		await RunStateMachineWithError(WithNameOnEntry, innerXml: "<log expr='_name'/><log expr='_not_existed'/><log expr='_name'/>");

		_logMethods.Verify(l => l.Info("ILog", default, "MyName"));
		_logMethods.Verify(l => l.Error("IStateMachineInterpreter", "Execution error in entity '(#-1)'.", It.Is<Exception>(e => e.Message == "_not_existed is not defined")));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task InterpreterVersionVariableTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.interpreter.version'/>");

		var version = typeof(StateMachineInterpreter).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

		_logMethods.Verify(l => l.Info("ILog", default, version));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task InterpreterNameVariableTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.interpreter.name'/>");

		_logMethods.Verify(l => l.Info("ILog", default, "Xtate.Core.StateMachineInterpreter"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task DataModelNameVariableTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.datamodel.name'/>");

		_logMethods.Verify(l => l.Info("ILog", default, "Xtate.DataModel.EcmaScript.EcmaScriptDataModelHandler"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task DataModelAssemblyVariableTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.datamodel.assembly'/>");

		_logMethods.Verify(l => l.Info("ILog", default, "Xtate.DataModel.EcmaScript"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task DataModelVersionVariableTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.datamodel.version'/>");

		_logMethods.Verify(l => l.Info("ILog", default, It.Is<DataModelValue>(v => v.AsString().Length > 0)));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task JintVersionVariableTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<log expr='_x.datamodel.vars.JintVersion'/>");

		_logMethods.Verify(l => l.Info("ILog", default, "2.11.58"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task ExecutionScriptTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<script>my='1'+'a';</script><log expr='my'/>");

		_logMethods.Verify(l => l.Info("ILog", default, "1a"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task SimpleAssignTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<assign location='x' expr='\"Hello World\"'/><log expr='x'/>");

		_logMethods.Verify(l => l.Info("ILog", default, "Hello World"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task ComplexAssignTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<script>my=[]; my[3]={};</script><assign location='my[3].yy' expr=\"'Hello World'\"/><log expr='my[3].yy'/>");

		_logMethods.Verify(l => l.Info("ILog", default, "Hello World"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task UserAssignTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<assign location='_name1' expr=\"'Hello World'\"/><log expr='_name1'/>");

		_logMethods.Verify(l => l.Info("ILog", default, "Hello World"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task SystemAssignTest()
	{
		await RunStateMachineWithError(WithNameOnEntry, innerXml: "<assign location='_name' expr=\"'Hello World'\"/>");

		_logMethods.Verify(l => l.Error("IStateMachineInterpreter", "Execution error in entity '(#-1)'.", It.IsAny<InvalidOperationException>()));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task IfTrueTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='1==1'><log expr=\"'Hello World'\"/></if>");

		_logMethods.Verify(l => l.Info("ILog", default, "Hello World"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task IfFalseTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='1==0'><log expr=\"'Hello World'\"/></if>");

		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task IfElseTrueTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='true'><log expr=\"'Hello World'\"/><else/><log expr=\"'Bye World'\"/></if>");

		_logMethods.Verify(l => l.Info("ILog", default, "Hello World"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task IfElseFalseTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='false'><log expr=\"'Hello World'\"/><else/><log expr=\"'Bye World'\"/></if>");

		_logMethods.Verify(l => l.Info("ILog", default, "Bye World"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task SwitchTest()
	{
		await RunStateMachine(WithNameOnEntry, innerXml: "<if cond='false'><log expr=\"'Hello World'\"/><elseif cond='true'/><log expr=\"'Maybe World'\"/><else/><log expr=\"'Bye World'\"/></if>");

		_logMethods.Verify(l => l.Info("ILog", default, "Maybe World"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task ForeachNoIndexTest()
	{
		await RunStateMachine(
			WithNameOnEntry, "<script>my=[]; my[0]='aaa'; my[1]='bbb'</script><foreach array='my' item='itm'>"
							 + "<log expr=\"itm\"/></foreach><log expr='typeof(itm)'/>");

		_logMethods.Verify(l => l.Info("ILog", default, "aaa"));
		_logMethods.Verify(l => l.Info("ILog", default, "bbb"));
		_logMethods.Verify(l => l.Info("ILog", default, "undefined"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task ForeachWithIndexTest()
	{
		await RunStateMachine(
			WithNameOnEntry, "<script>my=[]; my[0]='aaa'; my[1]='bbb'</script><foreach array='my' item='itm' index='idx'>"
							 + "<log expr=\"idx + '-' + itm\"/></foreach><log expr='typeof(itm)'/><log expr='typeof(idx)'/>");

		_logMethods.Verify(l => l.Info("ILog", default, "0-aaa"));
		_logMethods.Verify(l => l.Info("ILog", default, "1-bbb"));
		_logMethods.Verify(l => l.Info("ILog", default, "undefined"));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task NoneDataModelTransitionWithConditionTrueTest()
	{
		await RunStateMachine(NoneDataModel, innerXml: "<state id='s1'><transition cond='In(s1)' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

		_logMethods.Verify(l => l.Info("ILog", "Hello", default));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task NoneDataModelTransitionWithConditionFalseTest()
	{
		await RunStateMachine(NoneDataModel, innerXml: "<state id='s1'><transition cond='In(s2)' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task EcmaScriptDataModelTransitionWithConditionTrueTest()
	{
		await RunStateMachine(EcmaScriptDataModel, innerXml: "<state id='s1'><transition cond=\"In('s1')\" target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

		_logMethods.Verify(l => l.Info("ILog", "Hello", default));
		_logMethods.VerifyNoOtherCalls();
	}

	[TestMethod]
	public async Task EcmaScriptDataModelTransitionWithConditionFalseTest()
	{
		await RunStateMachine(EcmaScriptDataModel, innerXml: "<state id='s1'><transition cond=\"In('s2')\" target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

		_logMethods.VerifyNoOtherCalls();
	}

	public interface ILogMethods
	{
		void Info(string category, string? message, DataModelValue arg);
		void Error(string category, string? message, Exception? ex);
		void Trace(string category, string? message);
	}

	[UsedImplicitly]
	private class LogWriter<TSource>(ILogMethods logMethods) : ILogWriter<TSource>
	{
	#region Interface ILogWriter<TSource>

		public bool IsEnabled(Level level) => level is Level.Info or Level.Warning or Level.Error;

		public async ValueTask Write(Level level,
									 int eventId,
									 string? message,
									 IAsyncEnumerable<LoggingParameter>? parameters = default)
		{
			var prms = new Dictionary<string, LoggingParameter>();

			if (parameters is not null)
			{
				await foreach (var parameter in parameters)
				{
					prms[parameter.Name] = parameter;
				}
			}

			switch (level)
			{
				case Level.Info when prms.ContainsKey("Parameter"):
					logMethods.Info(typeof(TSource).Name, message, DataModelValue.FromObject(prms["Parameter"].Value));
					break;
				case Level.Info:
					logMethods.Info(typeof(TSource).Name, message, arg: default);
					break;
				case Level.Error when prms.ContainsKey("Exception"):
					logMethods.Error(typeof(TSource).Name, message, (Exception?) prms["Exception"].Value);
					break;
				case Level.Trace:
					logMethods.Trace(typeof(TSource).Name, message);
					break;
				default: throw new NotSupportedException();
			}
		}

	#endregion
	}
}