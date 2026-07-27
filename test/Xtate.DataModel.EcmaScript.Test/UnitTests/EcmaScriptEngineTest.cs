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

using System.Reflection;
using Jint;
using Jint.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.DataModel.EcmaScript.Services;
using Xtate.DataTypes;
using Xtate.StateMachine;

namespace Xtate.DataModel.EcmaScript.Test.UnitTests;

[TestClass]
public class EcmaScriptEngineTest
{
	[TestMethod]
	public void ConstructorRegistersInPredicate()
	{
		var inStateController = new Mock<IInStateController>();
		inStateController
			.Setup(controller => controller.InState(It.Is<IIdentifier>(identifier => identifier.Value == "active")))
			.Returns(true);
		var engine = CreateEngine(inStateController: inStateController.Object);

		Assert.IsTrue(engine.JintEngine.Evaluate("In('active')").AsBoolean());
		Assert.IsFalse(engine.JintEngine.Evaluate("In('inactive')").AsBoolean());
		inStateController.Verify(controller => controller.InState(It.Is<IIdentifier>(identifier => identifier.Value == "active")), Times.Once);
		inStateController.Verify(controller => controller.InState(It.Is<IIdentifier>(identifier => identifier.Value == "inactive")), Times.Once);

		var descriptor = engine.JintEngine.Global.GetOwnProperty("In");
		Assert.IsFalse(descriptor.Writable);
		Assert.IsFalse(descriptor.Enumerable);
		Assert.IsFalse(descriptor.Configurable);
	}

	[TestMethod]
	public void EngineExecutesJavaScriptInStrictMode()
	{
		var engine = CreateEngine();

		Assert.ThrowsExactly<JavaScriptException>(() => engine.JintEngine.Execute("undeclaredVariable = 1"));
		Assert.AreEqual(expected: "undefined", engine.JintEngine.Evaluate("typeof undeclaredVariable").AsString());
	}

	[TestMethod]
	public void SyncRootVariablesExposesAndUpdatesDataModelEntries()
	{
		var dataModel = new DataModelList { ["value"] = 7, [string.Empty] = "ignored" };
		var engine = CreateEngine(dataModel);

		SyncRootVariables(engine, dataModel);

		Assert.AreEqual(expected: 7, engine.JintEngine.Evaluate("value").AsNumber());
		Assert.AreEqual(expected: "undefined", engine.JintEngine.Evaluate("typeof ignored").AsString());

		engine.JintEngine.Execute("value = 9");

		Assert.AreEqual(expected: 9, dataModel["value"].AsNumber().ToInt32());
		Assert.AreEqual(expected: "ignored", dataModel[string.Empty].AsString());
	}

	[TestMethod]
	public void SyncRootVariablesTracksEntriesAddedAndRemovedAfterInitialSync()
	{
		var dataModel = new DataModelList { ["first"] = 1 };
		var engine = CreateEngine(dataModel);
		SyncRootVariables(engine, dataModel);

		dataModel["second"] = "two";
		dataModel.RemoveFirst(key: "first", caseInsensitive: false);
		SyncRootVariables(engine, dataModel);

		Assert.AreEqual(expected: "undefined", engine.JintEngine.Evaluate("typeof first").AsString());
		Assert.AreEqual(expected: "two", engine.JintEngine.Evaluate("second").AsString());
	}

	[TestMethod]
	public void SyncRootVariablesUsesCaseSensitiveNames()
	{
		var dataModel = new DataModelList { ["Value"] = 3 };
		var engine = CreateEngine(dataModel);

		SyncRootVariables(engine, dataModel);

		Assert.AreEqual(expected: 3, engine.JintEngine.Evaluate("Value").AsNumber());
		Assert.AreEqual(expected: "undefined", engine.JintEngine.Evaluate("typeof value").AsString());
	}

	private static EcmaScriptEngine CreateEngine(DataModelList? dataModel = null, IInStateController? inStateController = null)
	{
		dataModel ??= [];

		return new EcmaScriptEngine
			   {
				   DataModelController = Mock.Of<IDataModelController>(controller => controller.DataModel == dataModel),
				   InStateController = inStateController ?? Mock.Of<IInStateController>()
			   };
	}

	private static void SyncRootVariables(EcmaScriptEngine engine, DataModelList dataModel)
	{
		var method = typeof(EcmaScriptEngine).GetMethod(name: "SyncRootVariables", BindingFlags.Instance | BindingFlags.NonPublic);

		Assert.IsNotNull(method);
		method.Invoke(engine, [dataModel]);
	}
}