using System;
using System.Runtime.Serialization;

namespace MD.Framework.Utility
{
	[Serializable]
	[DataContract]
	public class SortItem
	{
		[DataMember]
		public SortDirection SortDirection { get; set; }

		[DataMember]
		public string PropertySelector { get; set; }
	}
}
