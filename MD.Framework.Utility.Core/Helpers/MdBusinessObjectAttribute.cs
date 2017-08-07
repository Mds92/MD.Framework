using System;

namespace MD.Framework.Utility.Core.Helpers
{
	[AttributeUsage(AttributeTargets.Class)]
	public class MdBusinessObjectFlagAttribute : Attribute
	{
		public Type EntityType { get; set; }
	}
}