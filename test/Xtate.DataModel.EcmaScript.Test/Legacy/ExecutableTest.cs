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
<<<<<<< Updated upstream
using System.IO;
=======
>>>>>>> Stashed changes
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.Core;
using Xtate.IoC;
<<<<<<< Updated upstream
using Xtate.CustomAction;
using Xtate.Scxml;
using IServiceProvider = Xtate.IoC.IServiceProvider;
=======
>>>>>>> Stashed changes

namespace Xtate.DataModel.EcmaScript.Test;

[TestClass]
public class ExecutableTest
{
	//private Mock<ICustomActionExecutor>         _customActionExecutor          = default!;
	//private Mock<ICustomActionFactory>          _customActionProvider          = default!;
	//private Mock<ICustomActionFactoryActivator> _customActionProviderActivator = default!;
	private ChannelReader<IEvent>        _eventChannel          = default!;
	private Mock<IEventController>       _eventController       = default!;
	private Mock<IEventQueueReader>      _eventQueueReader      = default!;
	private Mock<IExternalCommunication> _externalCommunication = default!;
	private Mock<ILogger>                _logger                = default!;
	private Mock<ILogWriter>             _logWriter             = default!;
	private InterpreterOptions           _options               = default!;

	private static async ValueTask<IStateMachine> GetStateMachine(string scxml)
	{
<<<<<<< Updated upstream
		private Mock<CustomActionBase>       _customActionBase              = default!;
		private Mock<ICustomActionExecutor>  _customActionExecutor          = default!;
		private Mock<ICustomActionProvider>  _customActionProvider          = default!;
		private Mock<ICustomActionActivator> _customActionProviderActivator = default!;
		private ChannelReader<IEvent>        _eventChannel                  = default!;
		private Mock<IExternalCommunication> _externalCommunication         = default!;
		private Mock<ILogWriter>             _logger                        = default!;
		private InterpreterOptions           _options                       = default!;
		private IServiceProvider             _serviceProvider;
		private IScxmlStateMachine           _scxmlStateMachine;
=======
		var services = new ServiceCollection();
		services.RegisterStateMachineFactory();
		services.AddForwarding<IScxmlStateMachine>(_ => new ScxmlStateMachine(scxml));
		var provider = services.BuildProvider();
>>>>>>> Stashed changes

		//using var textReader = new StringReader(scxml);
		//using var reader = XmlReader.Create(textReader);
		//var scxmlDirector = new ScxmlDirector(reader, BuilderFactory.Instance, new ScxmlDirectorOptions { StateMachineValidator = StateMachineValidator.Instance });

		//return scxmlDirector.ConstructStateMachine().AsTask().GetAwaiter().GetResult();

		return await provider.GetRequiredService<IStateMachine>();
	}

	private static ValueTask<IStateMachine> NoneDataModel(string xml) => GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='null'>" + xml + "</scxml>");
	private static ValueTask<IStateMachine> EcmaDataModel(string xml) => GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + xml + "</scxml>");

	private async Task RunStateMachine(Func<string, ValueTask<IStateMachine>> getter, string innerXml)
	{
		var stateMachine = getter(innerXml);

		var services = new ServiceCollection();
		services.RegisterStateMachineInterpreter();
		services.RegisterEcmaScriptDataModelHandler();
		services.AddForwarding(_ => stateMachine);
		services.AddForwarding(_ => _logWriter.Object);
		services.AddForwarding(_ => _eventController.Object);
		services.AddForwarding(_ => _eventQueueReader.Object);
		var provider = services.BuildProvider();

		var stateMachineInterpreter = await provider.GetRequiredService<IStateMachineInterpreter>();

		try
		{
<<<<<<< Updated upstream
			using var textReader = new StringReader(scxml);
			using var reader = XmlReader.Create(textReader);
			var serviceLocator = ServiceLocator.Create(s => s.AddForwarding<IStateMachineValidator, StateMachineValidator>());
			var scxmlDirector = serviceLocator.GetService<ScxmlDirector, XmlReader>(reader);
			//var scxmlDirector = new ScxmlDirector(reader, serviceLocator.GetService<IBuilderFactory>(), new ScxmlDirectorOptions(serviceLocator));
			return scxmlDirector.ConstructStateMachine().AsTask().GetAwaiter().GetResult();
		}

		private static string NoneDataModel(string xml) => "<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='null'>" + xml + "</scxml>";
		private static string EcmaDataModel(string xml) => "<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + xml + "</scxml>";

		private async Task RunStateMachine(Func<string, string> getter, string innerXml)
		{
			//var stateMachine = getter(innerXml);
			_scxmlStateMachine = new ScxmlStateMachine(getter(innerXml));
			try
			{
				
				var stateMachineInterpreter = await _serviceProvider.GetRequiredService<IStateMachineInterpreter>();
				var eventQueueWriter = await _serviceProvider.GetRequiredService<IEventQueueWriter>();
				eventQueueWriter.Complete();


				await stateMachineInterpreter.RunAsync();

				Assert.Fail("StateMachineQueueClosedException should be raised");
			}
			catch (StateMachineQueueClosedException)
			{
				//ignore
			}
=======
			//await stateMachineInterpreter.RunAsync(SessionId.New(), stateMachine, _eventChannel, _options);
			await stateMachineInterpreter.RunAsync();

			Assert.Fail("StateMachineQueueClosedException should be raised");
		}
		catch (StateMachineQueueClosedException)
		{
			//ignore
>>>>>>> Stashed changes
		}
	}

<<<<<<< Updated upstream
		[TestInitialize]
		public void Init()
		{
			var services = new ServiceCollection();
			services.RegisterStateMachineInterpreter();
			services.RegisterStateMachineFactory();
			services.RegisterEcmaScriptDataModelHandler();
			services.AddForwarding(_ => _scxmlStateMachine);
			services.AddForwarding(_ => _logger.Object);
			services.AddForwarding(_ => _customActionProvider.Object);
			services.AddForwarding(_ => _externalCommunication.Object);

			//services.AddSharedImplementationSync<MyActionProvider>(SharedWithin.Scope).For<ICustomActionProvider>();
			//services.AddTypeSync<MyAction, XmlReader>();

				
			_serviceProvider = services.BuildProvider();

			_logger = new Mock<ILogWriter>();
			_logger.Setup(writer => writer.IsEnabled(Level.Info)).Returns(true);

			_customActionBase = new Mock<CustomActionBase>();
			_customActionBase.Setup(s => s.Execute())
							 .Callback(() => _serviceProvider.GetRequiredService<ILogger<ILog>>().Result.Write(Level.Info, "Custom"));

			_customActionProviderActivator = new Mock<ICustomActionActivator>();
			_customActionProviderActivator.Setup(x => x.Activate(It.IsAny<string>())).Returns(_customActionBase.Object);

			_customActionProvider = new Mock<ICustomActionProvider>();
			_customActionProvider.Setup(x => x.TryGetActivator(It.IsAny<string>(), It.IsAny<string>())).Returns(_customActionProviderActivator.Object);

			_externalCommunication = new Mock<IExternalCommunication>();

			/*

			var channel = Channel.CreateUnbounded<IEvent>();
			channel.Writer.Complete();
			_eventChannel = channel.Reader;

=======
	[TestInitialize]
	public void Init()
	{
		var channel = Channel.CreateUnbounded<IEvent>();
		channel.Writer.Complete();
		_eventChannel = channel.Reader;
		/*
>>>>>>> Stashed changes
			_customActionExecutor = new Mock<ICustomActionExecutor>();

			_customActionExecutor.Setup(e => e.Execute())
								 .Callback((IExecutionContext ctx, CancellationToken tk) => ctx.LogOld(LogLevel.Info, message: "Custom", arguments: default).AsTask().Wait(tk));

			_customActionProviderActivator = new Mock<ICustomActionFactoryActivator>();
			_customActionProviderActivator.Setup(x => x.CreateExecutor(It.IsAny<ServiceLocator>(), It.IsAny<ICustomActionContext>(), default))
										  .Returns(new ValueTask<ICustomActionExecutor>(_customActionExecutor.Object));

			_customActionProvider = new Mock<ICustomActionFactory>();
			_customActionProvider.Setup(x => x.TryGetActivator(It.IsAny<ServiceLocator>(), It.IsAny<string>(), It.IsAny<string>(), default))
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
		_logWriter = new Mock<ILogWriter>();
		_eventController = new Mock<IEventController>();
		_eventQueueReader = new Mock<IEventQueueReader>();

<<<<<<< Updated upstream
			_logger = new Mock<ILoggerOld>();
			_options = new InterpreterOptions(ServiceLocator.Create(
												  delegate(IServiceCollection s)
												  {
													  s.AddXPath();
													  s.AddEcmaScript();
												  }))
					   {
						   CustomActionProviders = ImmutableArray.Create(_customActionProvider.Object),
						   Logger = _logger.Object,
						   ExternalCommunication = _externalCommunication.Object
					   };*/
		}
=======
		//_eventQueueReader.Setup(x => x.)
	}
>>>>>>> Stashed changes

	[TestMethod]
	public async Task ContentJsonTest()
	{
		await RunStateMachine(
			EcmaDataModel,
			innerXml:
			"<state id='s1'><onentry><send><content>{ \"key\":\"value\" }</content></send></onentry></state>");

<<<<<<< Updated upstream
			_logger.Verify(l => l.Write(Level.Info, "ILog", "Hello", It.IsAny<IEnumerable<LoggingParameter>>()), Times.Once);
		}
=======
		//_externalCommunication.Verify(a => a.TrySendEvent(It.IsAny<IOutgoingEvent>(), It.IsAny<CancellationToken>()));
		_eventController.Verify(a => a.Send(It.IsAny<IOutgoingEvent>()));
	}
	
	[TestMethod]
	[Ignore]
	public async Task RaiseTest()
	{
		await RunStateMachine(
			NoneDataModel,
			innerXml:
			"<state id='s1'><onentry><raise event='my'/></onentry><transition event='my' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");
>>>>>>> Stashed changes

		_logWriter.Verify(l => l.Write(Level.Info, "Hello", It.IsAny<IEnumerable<LoggingParameter>>()));
		//_logWriter.Verify(l => l.Write(It.IsAny<Level>(), It.IsAny<string>(), It.IsAny<IEnumerable<LoggingParameter>>()));

<<<<<<< Updated upstream
			//_logger.Verify(l => l.ExecuteLogOld(LogLevel.Info, "Hello", default, default), Times.Once);
			_logger.Verify(l => l.Write(Level.Info, "ILog", "Hello", It.IsAny<IEnumerable<LoggingParameter>>()), Times.Once);
		}
=======
		//_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), LogLevel.Info, "Hello", default, default, default), Times.Once);
	}
	
	[TestMethod]
	[Ignore]
	public async Task SendInternalTest()
	{
		await RunStateMachine(
			NoneDataModel,
			innerXml:
			"<state id='s1'><onentry><send event='my' target='_internal'/></onentry><transition event='my' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");
>>>>>>> Stashed changes

		//_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), LogLevel.Info, "Hello", default, default, default), Times.Once);
		_logWriter.Verify(l => l.Write(Level.Info, "Hello", It.IsAny<IEnumerable<LoggingParameter>>()));
	}

<<<<<<< Updated upstream
			_logger.Verify(l => l.Write(Level.Info, "ILog", "Hello", It.IsAny<IEnumerable<LoggingParameter>>()), Times.Once);
			//_logger.Verify(l => l.ExecuteLogOld(LogLevel.Info, "Hello", default, default), Times.Once);
		}
=======
	[TestMethod]
	[Ignore]
	public async Task RaiseWithEventDescriptorTest()
	{
		await RunStateMachine(
			NoneDataModel,
			innerXml:
			"<state id='s1'><onentry><raise event='my.suffix'/></onentry><transition event='my' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");
>>>>>>> Stashed changes

		//_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), LogLevel.Info, "Hello", default, default, default), Times.Once);
		_logWriter.Verify(l => l.Write(Level.Info, "Hello", It.IsAny<IEnumerable<LoggingParameter>>()));
	}

<<<<<<< Updated upstream
			_logger.Verify(l => l.Write(Level.Info, "ILog", "Hello", It.IsAny<IEnumerable<LoggingParameter>>()), Times.Once);
			//_logger.Verify(l => l.ExecuteLogOld(LogLevel.Info, "Hello", default, default), Times.Once);
		}
=======
	[TestMethod]
	[Ignore]
	public async Task RaiseWithEventDescriptor2Test()
	{
		await RunStateMachine(
			NoneDataModel,
			innerXml:
			"<state id='s1'><onentry><raise event='my.suffix'/></onentry><transition event='my.*' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");
>>>>>>> Stashed changes

		//_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), LogLevel.Info, "Hello", default, default, default), Times.Once);
		_logWriter.Verify(l => l.Write(Level.Info, "Hello", It.IsAny<IEnumerable<LoggingParameter>>()));
	}

<<<<<<< Updated upstream
			_logger.Verify(l => l.Write(Level.Info, "ILog", "Custom", It.IsAny<IEnumerable<LoggingParameter>>()), Times.Once);
			//_logger.Verify(l => l.ExecuteLogOld(LogLevel.Info, "Custom", default, default), Times.Once);
		}

		[TestMethod]
		public async Task ContentJsonTest()
		{
			await RunStateMachine(EcmaDataModel,
								  innerXml:
								  @"<state id='s1'><onentry><send><content>{ ""key"":""value"" }</content></send></onentry></state>");

			_externalCommunication.Verify(a => a.TrySendEvent(It.IsAny<IOutgoingEvent>()));
		}
=======
	[TestMethod]
	[Ignore]
	public async Task CustomActionTest()
	{
		await RunStateMachine(
			NoneDataModel,
			innerXml:
			"<state id='s1'><onentry><custom my='name'/></onentry></state>");

		//_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), LogLevel.Info, "Custom", default, default, default), Times.Once);
		_logWriter.Verify(l => l.Write(Level.Info, "Custom", It.IsAny<IEnumerable<LoggingParameter>>()));
>>>>>>> Stashed changes
	}
}