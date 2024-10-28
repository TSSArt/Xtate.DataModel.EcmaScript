﻿// Copyright © 2019-2024 Sergii Artemenko
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

using Jint.Parser.Ast;
using Xtate.DataModel;
using Xtate.DataModel.EcmaScript;
using Xtate.IoC;

namespace Xtate;

public class EcmaScriptDataModelHandlerModule : Module
{
	protected override void AddServices()
	{
		Services.AddTypeSync<EcmaScriptForEachEvaluator, IForEach>();
		Services.AddTypeSync<EcmaScriptCustomActionEvaluator, ICustomAction>();
		Services.AddTypeSync<EcmaScriptExternalScriptExpressionEvaluator, IExternalScriptExpression>();
		Services.AddTypeSync<EcmaScriptExternalDataExpressionEvaluator, IExternalDataExpression>();
		Services.AddTypeSync<EcmaScriptValueExpressionEvaluator, IValueExpression, Program>();
		Services.AddTypeSync<EcmaScriptConditionExpressionEvaluator, IConditionExpression, Program>();
		Services.AddTypeSync<EcmaScriptScriptExpressionEvaluator, IScriptExpression, Program>();
		Services.AddTypeSync<EcmaScriptLocationExpressionEvaluator, ILocationExpression, (Program, Expression?)>();
		Services.AddTypeSync<EcmaScriptInlineContentEvaluator, IInlineContent>();
		Services.AddTypeSync<EcmaScriptContentBodyEvaluator, IContentBody>();

		//TODO:delete
		/*


		   public required Func<, , > EcmaScriptValueExpressionEvaluatorFactory { private get; [UsedImplicitly] init; }
		   public required Func<, Program, > EcmaScriptConditionExpressionEvaluatorFactory { private get; [UsedImplicitly] init; }
		   public required Func<, Program, > EcmaScriptScriptExpressionEvaluatorFactory { private get; [UsedImplicitly] init; }
		   public required Func<, Program, >  { private get; [UsedImplicitly] init; }
*/
		/*
		services.AddModule<DataModelHandlerBaseModule>();
		services.AddModule<ErrorProcessorModule>();
		services.AddModule<NameTableModule>();

		services.AddTypeSync<XPathValueExpressionEvaluator, IValueExpression, XPathCompiledExpression>();
		services.AddTypeSync<XPathConditionExpressionEvaluator, IConditionExpression, XPathCompiledExpression>();
		services.AddTypeSync<XPathLocationExpressionEvaluator, ILocationExpression, XPathCompiledExpression>();
		services.AddTypeSync<XPathLocationExpression, ILocationExpression, (XPathAssignType, string?)>();
		services.AddTypeSync<XPathContentBodyEvaluator, IContentBody>();
		services.AddTypeSync<XPathExternalDataExpressionEvaluator, IExternalDataExpression>();
		services.AddTypeSync<XPathInlineContentEvaluator, IInlineContent>();

		//services.AddType<XPathExpressionContextOld, IXmlNamespacesInfo?>();  //TODO:
		//services.AddType<XPathVarDescriptorOld, string>();

		services.AddTypeSync<XPathExpressionContext, IXmlNamespacesInfo?>();
		services.AddTypeSync<XPathVarDescriptor, string>();
		services.AddTypeSync<XPathCompiledExpression, string, IXmlNamespacesInfo?>();
		services.AddTypeSync<XPathXmlParserContextFactory>();
		services.AddSharedType<XPathEngine>(SharedWithin.Scope);

		services.AddImplementationSync<InFunctionProvider>().For<IXPathFunctionProvider>();
		services.AddTypeSync<InFunction>();*/

		Services.AddSharedType<EcmaScriptEngine>(SharedWithin.Scope);
		Services.AddImplementation<EcmaScriptDataModelHandlerProvider>().For<IDataModelHandlerProvider>();
		Services.AddImplementation<EcmaScriptDataModelHandler>().For<EcmaScriptDataModelHandler>().For<IDataModelHandler>(Option.IfNotRegistered);
	}
}