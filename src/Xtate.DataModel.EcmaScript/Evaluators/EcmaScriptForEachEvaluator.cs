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

namespace Xtate.DataModel.EcmaScript;

public class EcmaScriptForEachEvaluator : DefaultForEachEvaluator
{
	private readonly EcmaScriptLocationExpressionEvaluator? _indexEvaluator;
	private readonly EcmaScriptLocationExpressionEvaluator  _itemEvaluator;

	public EcmaScriptForEachEvaluator(IForEach forEach) : base(forEach)
	{
		var itemEvaluator = base.Item?.As<EcmaScriptLocationExpressionEvaluator>();
		Infra.NotNull(itemEvaluator);
		_itemEvaluator = itemEvaluator;

		_indexEvaluator = base.Index?.As<EcmaScriptLocationExpressionEvaluator>();
	}

	public required Func<ValueTask<EcmaScriptEngine>> EngineFactory { private get; [UsedImplicitly] init; }

	public override async ValueTask Execute()
	{
		var engine = await EngineFactory().ConfigureAwait(false);

		engine.EnterExecutionContext();

		try
		{
			await _itemEvaluator.DeclareLocalVariable().ConfigureAwait(false);

			if (_indexEvaluator is not null)
			{
				await _indexEvaluator.DeclareLocalVariable().ConfigureAwait(false);
			}

			await base.Execute().ConfigureAwait(false);
		}
		finally
		{
			engine.LeaveExecutionContext();
		}
	}
}