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

using Xtate.DataModel.DependencyInjection;
using Xtate.DataModel.EcmaScript.Services;
using Xtate.IoC;
using Xtate.StateMachine;

namespace Xtate.DataModel.EcmaScript.DependencyInjection;

public class EcmaScriptDataModelHandlerModule : Module<DataModelHandlerBaseModule>
{
    protected override void AddServices()
    {
        Services.AddTypeSync<EcmaScriptForEachEvaluator, IForEach>();
        Services.AddTypeSync<EcmaScriptCustomActionEvaluator, ICustomAction>();
        Services.AddTypeSync<EcmaScriptExternalScriptExpressionEvaluator, IExternalScriptExpression>();
        Services.AddTypeSync<EcmaScriptExternalDataExpressionEvaluator, IExternalDataExpression>();
        Services.AddTypeSync<EcmaScriptValueExpressionEvaluator, IValueExpression, Prepared<Script>>();
        Services.AddTypeSync<EcmaScriptConditionExpressionEvaluator, IConditionExpression, Prepared<Script>>();
        Services.AddTypeSync<EcmaScriptScriptExpressionEvaluator, IScriptExpression, Prepared<Script>>();
        Services.AddTypeSync<EcmaScriptLocationExpressionEvaluator, ILocationExpression, (Prepared<Script>, Expression?)>();
        Services.AddTypeSync<EcmaScriptInlineContentEvaluator, IInlineContent>();
        Services.AddTypeSync<EcmaScriptContentBodyEvaluator, IContentBody>();

        Services.AddSharedType<EcmaScriptEngine>(SharedWithin.Scope);
        Services.AddImplementation<EcmaScriptDataModelHandler.Provider>().For<IDataModelHandlerProvider>();
        Services.AddImplementation<EcmaScriptDataModelHandler>().For<EcmaScriptDataModelHandler>().For<IDataModelHandler>(Option.IfNotRegistered);
    }
}
