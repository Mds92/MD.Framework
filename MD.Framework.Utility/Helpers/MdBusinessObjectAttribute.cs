using System;

namespace MD.Framework.Utility
{
	[AttributeUsage(AttributeTargets.Class)]
	public class MdBusinessObjectFlagAttribute : Attribute
	{
		public Type EntityType { get; set; }
	}
}