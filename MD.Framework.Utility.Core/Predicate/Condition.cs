using System;

namespace MD.Framework.Utility.Core.Predicate
{
	public class Condition
	{
		public Guid Id { get; internal set; }
		public string EntityTypeName { get; set; }
		public ConditionTree Tree { get; set; }
	}
}
