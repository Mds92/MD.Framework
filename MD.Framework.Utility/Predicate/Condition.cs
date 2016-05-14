using System;
using System.Runtime.Serialization;

namespace MD.Framework.Utility
{
	[Serializable]
	[DataContract]
	public class Condition
	{
		[DataMember]
		public long Id { get; internal set; }

		[DataMember]
		public string EntityTypeName { get; set; }

		[DataMember]
		public ConditionTree Tree { get; set; }
	}
}
