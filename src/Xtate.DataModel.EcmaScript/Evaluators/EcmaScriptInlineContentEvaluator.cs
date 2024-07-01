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
namespace Xtate.DataModel.EcmaScript
{
	public class EcmaScriptInlineContentEvaluator : DefaultInlineContentEvaluator
	{
		public EcmaScriptInlineContentEvaluator(IInlineContent inlineContent) : base(inlineContent) { }

		protected override DataModelValue ParseToDataModel() => Value is not null ? DataModelConverter.FromJson(Value) : DataModelValue.Null;
	}
}
=======
	namespace Xtate.DataModel.EcmaScript;

	internal class EcmaScriptInlineContentEvaluator(IInlineContent inlineContent) : DefaultInlineContentEvaluator(inlineContent)
	{
		protected override DataModelValue ParseToDataModel() => Value is not null ? DataModelConverter.FromJson(Value) : DataModelValue.Null;
	}
>>>>>>> Stashed changes
