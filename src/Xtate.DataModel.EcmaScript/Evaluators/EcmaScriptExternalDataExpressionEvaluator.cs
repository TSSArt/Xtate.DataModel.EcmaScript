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

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Xtate.DataModel.EcmaScript
{
	internal class EcmaScriptExternalDataExpressionEvaluator : DefaultExternalDataExpressionEvaluator
	{
		public EcmaScriptExternalDataExpressionEvaluator(in ExternalDataExpression externalDataExpression) : base(externalDataExpression) { }

		protected override async ValueTask<DataModelValue> ParseToDataModel(Resource resource, CancellationToken token)
		{
			var content = await resource.GetContent(token).ConfigureAwait(false);

			if (content is null)
			{
				return DataModelValue.Null;
			}

			try
			{
				return DataModelConverter.FromJson(content);
			}
			catch (JsonException ex)
			{
				Infrastructure.IgnoredException(ex);

				return content.NormalizeSpaces();
			}
		}
	}
}