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

<<<<<<< Updated upstream
using System;
using System.Threading.Tasks;

namespace Xtate.DataModel.EcmaScript
{
=======
	using System;
	using System.Threading.Tasks;
	using Xtate.Core;

	namespace Xtate.DataModel.EcmaScript;

>>>>>>> Stashed changes
	public class EcmaScriptForEachEvaluator : DefaultForEachEvaluator
	{
		private readonly EcmaScriptLocationExpressionEvaluator? _indexEvaluator;
		private readonly EcmaScriptLocationExpressionEvaluator  _itemEvaluator;

<<<<<<< Updated upstream
		public required Func<ValueTask<EcmaScriptEngine>> EngineFactory { private get; init; }

		public override async ValueTask Execute()
		{
=======
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
>>>>>>> Stashed changes
			var engine = await EngineFactory().ConfigureAwait(false);

			engine.EnterExecutionContext();

			try
			{
<<<<<<< Updated upstream
				await ItemEvaluator.DeclareLocalVariable().ConfigureAwait(false);
				
				if (IndexEvaluator is not null)
				{
					await IndexEvaluator.DeclareLocalVariable().ConfigureAwait(false);
				}

=======
				await _itemEvaluator.DeclareLocalVariable().ConfigureAwait(false);

				if (_indexEvaluator is not null)
				{
					await _indexEvaluator.DeclareLocalVariable().ConfigureAwait(false);
				}

>>>>>>> Stashed changes
				await base.Execute().ConfigureAwait(false);
			}
			finally
			{
				engine.LeaveExecutionContext();
			}
		}
	}