using System;
using System.Runtime.Serialization;

namespace MD.Framework.Utility
{
	[Serializable]
	[DataContract]
	public enum SortDirection
	{
		[EnumMember]
		Ascending = 1,

		[EnumMember]
		Descending = 2
	}

}
