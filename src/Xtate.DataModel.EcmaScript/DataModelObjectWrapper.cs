using System.Collections.Generic;
using System.Linq;
using Jint;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Xtate.DataModel.EcmaScript
{
	internal class DataModelObjectWrapper : ObjectInstance, IObjectWrapper
	{
		private readonly DataModelObject _obj;

		public DataModelObjectWrapper(Engine engine, DataModelObject obj) : base(engine)
		{
			_obj = obj;

			Extensible = obj.Access == DataModelAccess.Writable;
		}

	#region Interface IObjectWrapper

		public object Target => _obj;

	#endregion

		public override void RemoveOwnProperty(string property)
		{
			_obj.Remove(property);

			base.RemoveOwnProperty(property);
		}

		public override IEnumerable<KeyValuePair<string, PropertyDescriptor>> GetOwnProperties()
		{
			return _obj.Properties.Select(p => new KeyValuePair<string, PropertyDescriptor>(p, GetOwnProperty(p)));
		}

		public override PropertyDescriptor GetOwnProperty(string property)
		{
			var descriptor = base.GetOwnProperty(property);

			if (descriptor != PropertyDescriptor.Undefined)
			{
				return descriptor;
			}

			descriptor = EcmaScriptHelper.CreatePropertyAccessor(Engine, _obj, property);

			base.SetOwnProperty(property, descriptor);

			return descriptor;
		}

		public override bool HasOwnProperty(string property) => _obj.Contains(property);
	}
}