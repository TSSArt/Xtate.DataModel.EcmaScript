// Copyright © 2019-2025 Sergii Artemenko
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

using Jint.Parser;

namespace Xtate.DataModel.EcmaScript;

public class EcmaScriptExternalScriptExpressionEvaluator(IExternalScriptExpression externalScriptExpression)
    : IExternalScriptExpression, IExecEvaluator, IExternalScriptConsumer, IAncestorProvider
{
    private Program? _program;

    public required Func<ValueTask<EcmaScriptEngine>> EngineFactory { private get; [UsedImplicitly] init; }

#region Interface IAncestorProvider

    object IAncestorProvider.Ancestor => externalScriptExpression;

#endregion

#region Interface IExecEvaluator

    public async ValueTask Execute()
    {
        Debug.Assert(_program is not null, Resources.Exception_ExternalScriptMissed);

        var engine = await EngineFactory().ConfigureAwait(false);

        engine.Exec(_program, startNewScope: true);
    }

#endregion

#region Interface IExternalScriptConsumer

    public void SetContent(string content) => _program = new JavaScriptParser().Parse(content);

#endregion

#region Interface IExternalScriptExpression

    public Uri? Uri => externalScriptExpression.Uri;

#endregion
}