#region Copyright © 2019-2020 Sergii Artemenko

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
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.Builder;
using Xtate.CustomAction;
using Xtate.Scxml;

namespace Xtate.DataModel.EcmaScript.Test
{
	[TestClass]
	public class ExecutableTest
	{
		private Mock<ICustomActionExecutor>         _customActionExecutor          = default!;
		private Mock<ICustomActionFactory>          _customActionProvider          = default!;
		private Mock<ICustomActionFactoryActivator> _customActionProviderActivator = default!;
		private ChannelReader<IEvent>               _eventChannel                  = default!;
		private Mock<IExternalCommunication>        _externalCommunication         = default!;
		private Mock<ILogger>                       _logger                        = default!;
		private InterpreterOptions                  _options;

		private static IStateMachine GetStateMachine(string scxml)
		{
			using var textReader = new StringReader(scxml);
			using var reader = XmlReader.Create(textReader);
			var scxmlDirector = new ScxmlDirector(reader, BuilderFactory.Instance, new ScxmlDirectorOptions { StateMachineValidator = StateMachineValidator.Instance });
			return scxmlDirector.ConstructStateMachine().AsTask().GetAwaiter().GetResult();
		}

		private static IStateMachine NoneDataModel(string xml) => GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='null'>" + xml + "</scxml>");
		private static IStateMachine EcmaDataModel(string xml) => GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + xml + "</scxml>");

		private async Task RunStateMachine(Func<string, IStateMachine> getter, string innerXml)
		{
			var stateMachine = getter(innerXml);

			try
			{
				await StateMachineInterpreter.RunAsync(SessionId.New(), stateMachine, _eventChannel, _options);

				Assert.Fail("StateMachineQueueClosedException should be raised");
			}
			catch (StateMachineQueueClosedException)
			{
				//ignore
			}
		}

		[TestInitialize]
		public void Init()
		{
			var channel = Channel.CreateUnbounded<IEvent>();
			channel.Writer.Complete();
			_eventChannel = channel.Reader;

			_customActionExecutor = new Mock<ICustomActionExecutor>();

			_customActionExecutor.Setup(e => e.Execute(It.IsAny<IExecutionContext>(), It.IsAny<CancellationToken>()))
								 .Callback((IExecutionContext ctx, CancellationToken tk) => ctx.Log(label: "Custom", arguments: default, tk).AsTask().Wait(tk));

			_customActionProviderActivator = new Mock<ICustomActionFactoryActivator>();
			_customActionProviderActivator.Setup(x => x.CreateExecutor(It.IsAny<IFactoryContext>(), It.IsAny<ICustomActionContext>(), default))
										  .Returns(new ValueTask<ICustomActionExecutor>(_customActionExecutor.Object));

			_customActionProvider = new Mock<ICustomActionFactory>();
			_customActionProvider.Setup(x => x.TryGetActivator(It.IsAny<IFactoryContext>(), It.IsAny<string>(), It.IsAny<string>(), default))
								 .Returns(new ValueTask<ICustomActionFactoryActivator?>(_customActionProviderActivator.Object));

			_options = new InterpreterOptions
					   {
							   DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
							   CustomActionProviders = ImmutableArray.Create(_customActionProvider.Object)
					   };
			_logger = new Mock<ILogger>();

			_options.Logger = _logger.Object;
			_externalCommunication = new Mock<IExternalCommunication>();
			_options.ExternalCommunication = _externalCommunication.Object;
		}

		[TestMethod]
		public async Task RaiseTest()
		{
			await RunStateMachine(NoneDataModel,
								  innerXml:
								  "<state id='s1'><onentry><raise event='my'/></onentry><transition event='my' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), "Hello", default, default), Times.Once);
		}

		[TestMethod]
		public async Task SendInternalTest()
		{
			await RunStateMachine(NoneDataModel,
								  innerXml:
								  "<state id='s1'><onentry><send event='my' target='_internal'/></onentry><transition event='my' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), "Hello", default, default), Times.Once);
		}

		[TestMethod]
		public async Task RaiseWithEventDescriptorTest()
		{
			await RunStateMachine(NoneDataModel,
								  innerXml:
								  "<state id='s1'><onentry><raise event='my.suffix'/></onentry><transition event='my' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), "Hello", default, default), Times.Once);
		}

		[TestMethod]
		public async Task RaiseWithEventDescriptor2Test()
		{
			await RunStateMachine(NoneDataModel,
								  innerXml:
								  "<state id='s1'><onentry><raise event='my.suffix'/></onentry><transition event='my.*' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), "Hello", default, default), Times.Once);
		}

		[TestMethod]
		public async Task CustomActionTest()
		{
			await RunStateMachine(NoneDataModel,
								  innerXml:
								  "<state id='s1'><onentry><custom my='name'/></onentry></state>");

			_logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), "Custom", default, default), Times.Once);
		}

		[TestMethod]
		public async Task ContentJsonTest()
		{
			await RunStateMachine(EcmaDataModel,
								  innerXml:
								  "<state id='s1'><onentry><send><content>{ 'key':'value' }</content></send></onentry></state>");

			_externalCommunication.Verify(a => a.TrySendEvent(It.IsAny<IOutgoingEvent>(), It.IsAny<CancellationToken>()));
		}
	}
}