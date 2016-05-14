using System;
using System.Collections.Generic;
using System.Reflection;

namespace MD.Framework.Business
{
	/// <summary>
	/// مدل اینتتی ها برای کش  کردن
	/// </summary>
	internal class EntityCacheModel
	{
		public Type EntityType { get; set; }
		public PropertyInfo PrimaryKey { get; set; }
		public List<KeyValuePair<Type, PropertyInfo>> NavigationProperties { get; set; }
	}
}