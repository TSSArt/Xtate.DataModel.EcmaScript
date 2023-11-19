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
using System.Threading.Tasks;
using Jint.Parser.Ast;
using Xtate.Core;
using Xtate.DataModel;
using Xtate.IoC;
using Xtate.DataModel.EcmaScript;
using Xtate.DataModel.XPath;

namespace Xtate
{
	[PublicAPI]
	public static class EcmaScriptExtensions
	{
		public static void RegisterEcmaScriptDataModelHandler(this IServiceCollection services)
		{
			if (services.IsRegistered<EcmaScriptDataModelHandlerProvider>())
			{
				return;
			}

			services.RegisterDataModelHandlerBase();
			services.RegisterErrorProcessor();
			services.RegisterNameTable();

			services.AddTypeSync<EcmaScriptValueExpressionEvaluator, IValueExpression, Program>();
			services.AddTypeSync<EcmaScriptConditionExpressionEvaluator, IConditionExpression, Program>();
			services.AddTypeSync<EcmaScriptLocationExpressionEvaluator, ILocationExpression, (Program, Expression?)>();
			services.AddTypeSync<EcmaScriptContentBodyEvaluator, IContentBody>();
			services.AddTypeSync<EcmaScriptExternalDataExpressionEvaluator, IExternalDataExpression>();
			services.AddTypeSync<EcmaScriptForEachEvaluator, IForEach>();
			services.AddTypeSync<EcmaScriptInlineContentEvaluator, IInlineContent>();
			services.AddTypeSync<EcmaScriptCustomActionEvaluator, ICustomAction>();
			services.AddTypeSync<EcmaScriptScriptExpressionEvaluator, IScriptExpression, Program>();
			services.AddTypeSync<EcmaScriptExternalScriptExpressionEvaluator, IExternalScriptExpression>();

			services.AddSharedType<EcmaScriptEngine>(SharedWithin.Scope);

			services.AddImplementation<EcmaScriptDataModelHandler>().For<EcmaScriptDataModelHandler>().For<IDataModelHandler>();
			services.AddImplementation<EcmaScriptDataModelHandlerProvider>().For<IDataModelHandlerProvider>();
		}

		public static IServiceCollection AddEcmaScript(this IServiceCollection services)
		{
			if (services is null) throw new ArgumentNullException(nameof(services));

			//services.AddIErrorProcessorService<EcmaScriptDataModelHandler>();

			//services.AddTransient(
			//	async provider => new EcmaScriptDataModelHandler(
			//		await provider.GetRequiredService<IErrorProcessorService<EcmaScriptDataModelHandler>>().ConfigureAwait(false),
			//		await provider.GetRequiredService<IExecutionContext>().ConfigureAwait(false)));

			services.AddType<EcmaScriptDataModelHandler>();

			//TODO:delete
			/*	services.AddForwarding<IDataModelHandler?, string?>(
					async (sp, dataModel) => dataModel == EcmaScriptDataModelHandler.DataModelType
						? await sp.GetRequiredService<EcmaScriptDataModelHandler>().ConfigureAwait(false)
						: default);*/

			//services.AddShared<IDataModelHandlerProvider>(SharedWithin.Container, sp => new EcmaScriptDataModelHandlerProvider(sp.GetRequiredFactory<EcmaScriptDataModelHandler>()));
			services.AddSharedImplementation<EcmaScriptDataModelHandlerProvider>(SharedWithin.Container).For<IDataModelHandlerProvider>();

			return services;
		}
	}

	//TODO:moveout
	public class EcmaScriptDataModelHandlerProvider : DataModelHandlerProviderBase<EcmaScriptDataModelHandler>
	{
		protected override bool CanHandle(string dataModelType) => dataModelType == @"ecmascript";
	}

}