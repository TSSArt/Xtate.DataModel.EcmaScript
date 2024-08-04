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
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.Core;
using Xtate.CustomAction;
using Xtate.IoC;

namespace Xtate.DataModel.EcmaScript.Test;

[TestClass]
public class ExecutableTest
{
	//private Mock<ICustomActionExecutor>         _customActionExecutor          = default!;
	private Mock<CustomActionBase>                     _customAction          = default!;
	private Mock<ICustomActionActivator>               _customActionActivator = default!;
	private Mock<ICustomActionProvider>                _customActionProvider  = default!;
	private ChannelReader<IEvent>                      _eventChannel          = default!;
	private Mock<IEventController>                     _eventController       = default!;
	private Mock<IEventQueueReader>                    _eventQueueReader      = default!;
	private Mock<IExternalCommunication>               _externalCommunication = default!;
	private Mock<ILogger>                              _logger                = default!;
	private Mock<ILogWriter<IEventController>>         _logWriterE            = default!;
	private Mock<ILogWriter<IStateMachineInterpreter>> _logWriterI            = default!;
	private Mock<ILogWriter<ILog>>                     _logWriterL            = default!;
	private InterpreterOptions                         _options               = default!;

	private static async ValueTask<IStateMachine> GetStateMachine(string scxml)
	{
		var services = new ServiceCollection();
		services.AddModule<StateMachineFactoryModule>();
		services.AddConstant<IScxmlStateMachine>(new ScxmlStateMachine(scxml));
		var provider = services.BuildProvider();

		//using var textReader = new StringReader(scxml);
		//using var reader = XmlReader.Create(textReader);
		//var scxmlDirector = new ScxmlDirector(reader, BuilderFactory.Instance, new ScxmlDirectorOptions { StateMachineValidator = StateMachineValidator.Instance });

		//return scxmlDirector.ConstructStateMachine().AsTask().GetAwaiter().GetResult();

		return await provider.GetRequiredService<IStateMachine>();
	}

	private static ValueTask<IStateMachine> NoneDataModel(string xml) => GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='null'>" + xml + "</scxml>");
	private static ValueTask<IStateMachine> EcmaDataModel(string xml) => GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + xml + "</scxml>");

	[TestInitialize]
	public void Init()
	{
		var channel = Channel.CreateUnbounded<IEvent>();
		channel.Writer.Complete();
		_eventChannel = channel.Reader;

		_logWriterL = new Mock<ILogWriter<ILog>>();
		_logWriterI = new Mock<ILogWriter<IStateMachineInterpreter>>();
		_logWriterE = new Mock<ILogWriter<IEventController>>();

		_customAction = new Mock<CustomActionBase>();
		_customAction.Setup(x => x.Execute()).Callback(() => _logWriterL.Object.Write(Level.Info, eventId: 0, message: "Custom"));

		_customActionActivator = new Mock<ICustomActionActivator>();
		_customActionActivator.Setup(x => x.Activate(It.IsAny<string>())).Returns(_customAction.Object);

		_customActionProvider = new Mock<ICustomActionProvider>();
		_customActionProvider.Setup(x => x.TryGetActivator("http://www.w3.org/2005/07/scxml", "custom")).Returns(_customActionActivator.Object);

		//		Xtate.IoC.DependencyInjectionException: Factory of [Xtate.Core.StateMachineInterpreter] raised exception. ---> Xtate.IoC.DependencyInjectionException: Factory of [Xtate.CustomAction.CustomActionContainer] raised exception. ---> Xtate.InfrastructureException: There is no any CustomActionProvider registered for processing custom action node: <>

		/*
			_customActionExecutor = new Mock<ICustomActionExecutor>();

			_customActionExecutor.Setup(e => e.Execute(It.IsAny<IExecutionContext>(), It.IsAny<CancellationToken>()))
								 .Callback((IExecutionContext ctx, CancellationToken tk) => ctx.Log(LogLevel.Info, message: "Custom", arguments: default, token: tk).AsTask().Wait(tk));

			_customActionProviderActivator = new Mock<ICustomActionFactoryActivator>();
			_customActionProviderActivator.Setup(x => x.CreateExecutor(It.IsAny<IFactoryContext>(), It.IsAny<ICustomActionContext>(), default))
										  .Returns(new ValueTask<ICustomActionExecutor>(_customActionExecutor.Object));

			_customActionProvider = new Mock<ICustomActionFactory>();
			_customActionProvider.Setup(x => x.TryGetActivator(It.IsAny<IFactoryContext>(), It.IsAny<string>(), It.IsAny<string>(), default))
								 .Returns(new ValueTask<ICustomActionFactoryActivator?>(_customActionProviderActivator.Object));
		*/
		_logger = new Mock<ILogger>();
		_externalCommunication = new Mock<IExternalCommunication>();
		/*_options = new InterpreterOptions
				   {
					   DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
					   CustomActionProviders = ImmutableArray.Create(_customActionProvider.Object),
					   Logger = _logger.Object,
					   ExternalCommunication = _externalCommunication.Object
				   };*/
		_eventController = new Mock<IEventController>();
		_eventQueueReader = new Mock<IEventQueueReader>();

		IEvent tmp = null!;

		//_eventQueueReader.Setup(x => x.TryReadEvent(out tmp)).Returns(false);
		//_eventQueueReader.Setup(x => x.WaitToEvent()).Returns(new ValueTask<bool>(false));
		_logWriterL.Setup(x => x.IsEnabled(It.IsAny<Level>())).Returns(true);
		_logWriterI.Setup(x => x.IsEnabled(It.IsAny<Level>())).Returns(true);
		_logWriterE.Setup(x => x.IsEnabled(It.IsAny<Level>())).Returns(true);
	}

	private async Task RunStateMachine(Func<string, ValueTask<IStateMachine>> getter, string innerXml)
	{
		var stateMachine = await getter(innerXml);

		var services = new ServiceCollection();
		services.AddModule<StateMachineInterpreterModule>();
		services.AddModule<EcmaScriptDataModelHandlerModule>();
		services.AddConstant(_customActionProvider.Object);
		services.AddConstant(stateMachine);
		services.AddConstant(_logWriterL.Object);
		services.AddConstant(_logWriterI.Object);
		services.AddConstant(_logWriterE.Object);

		//services.AddConstant(_ => _eventController.Object);
		services.AddConstant(_eventQueueReader.Object);
		var provider = services.BuildProvider();

		var stateMachineInterpreter = await provider.GetRequiredService<IStateMachineInterpreter>();

		try
		{
			//await stateMachineInterpreter.RunAsync(SessionId.New(), stateMachine, _eventChannel, _options);
			await stateMachineInterpreter.RunAsync();

			Assert.Fail("StateMachineQueueClosedException should be raised");
		}
		catch (StateMachineQueueClosedException)
		{
			//ignore
		}
	}

	[TestMethod]
	public async Task ContentJsonTest()
	{
		await RunStateMachine(
			EcmaDataModel,
			innerXml:
			"<state id='s1'><onentry><send><content>{ \"key\":\"value\" }</content></send></onentry></state>");

		//_externalCommunication.Verify(a => a.TrySendEvent(It.IsAny<IOutgoingEvent>(), It.IsAny<CancellationToken>()));
		//_eventController.Verify(a => a.Send(It.IsAny<IOutgoingEvent>()));
		_logWriterE.Verify(l => l.Write(Level.Trace, 1, "Send event: ''", It.IsAny<IAsyncEnumerable<LoggingParameter>>()));
	}

	[TestMethod]
	public async Task RaiseTest()
	{
		await RunStateMachine(
			NoneDataModel,
			innerXml:
			"<state id='s1'><onentry><raise event='my'/></onentry><transition event='my' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

		_logWriterL.Verify(l => l.Write(Level.Info, 1, "Hello", It.IsAny<IAsyncEnumerable<LoggingParameter>>()));

		//_logWriter.Verify(l => l.Write(It.IsAny<Level>(), It.IsAny<string>(), It.IsAny<IAsyncEnumerable<LoggingParameter>>()));

		//_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), LogLevel.Info, "Hello", default, default, default), Times.Once);
	}

	[TestMethod]
	public async Task SendInternalTest()
	{
		await RunStateMachine(
			NoneDataModel,
			innerXml:
			"<state id='s1'><onentry><send event='my' target='_internal'/></onentry><transition event='my' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

		//_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), LogLevel.Info, "Hello", default, default, default), Times.Once);
		_logWriterL.Verify(l => l.Write(Level.Info, 1, "Hello", It.IsAny<IAsyncEnumerable<LoggingParameter>>()));
	}

	[TestMethod]
	public async Task RaiseWithEventDescriptorTest()
	{
		await RunStateMachine(
			NoneDataModel,
			innerXml:
			"<state id='s1'><onentry><raise event='my.suffix'/></onentry><transition event='my' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

		//_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), LogLevel.Info, "Hello", default, default, default), Times.Once);
		_logWriterL.Verify(l => l.Write(Level.Info, 1, "Hello", It.IsAny<IAsyncEnumerable<LoggingParameter>>()));
	}

	[TestMethod]
	public async Task RaiseWithEventDescriptor2Test()
	{
		await RunStateMachine(
			NoneDataModel,
			innerXml:
			"<state id='s1'><onentry><raise event='my.suffix'/></onentry><transition event='my.*' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

		//_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), LogLevel.Info, "Hello", default, default, default), Times.Once);
		_logWriterL.Verify(l => l.Write(Level.Info, 1, "Hello", It.IsAny<IAsyncEnumerable<LoggingParameter>>()));
	}

	[TestMethod]
	public async Task CustomActionTest()
	{
		await RunStateMachine(
			NoneDataModel,
			innerXml:
			"<state id='s1'><onentry><custom my='name'/></onentry></state>");

		//_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), LogLevel.Info, "Custom", default, default, default), Times.Once);
		_logWriterL.Verify(l => l.Write(Level.Info, 0, "Custom", It.IsAny<IAsyncEnumerable<LoggingParameter>>()));
	}
}