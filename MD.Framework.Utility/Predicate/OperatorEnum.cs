using System;
using System.Runtime.Serialization;

namespace MD.Framework.Utility
{
	[Serializable]
	[DataContract]
	public enum OperatorEnum
	{
		[EnumMember]
		Equal = 1,

		[EnumMember]
		NotEqual = 2,

		[EnumMember]
		Contain = 3,

		[EnumMember]
		NotContain = 4,

		[EnumMember]
		Like = 5,

		[EnumMember]
		NotLike = 6,

		[EnumMember]
		StartsWith = 7,

		[EnumMember]
		NotStartsWith = 8,

		[EnumMember]
		EndsWith = 9,

		[EnumMember]
		NotEndsWith = 10,

		[EnumMember]
		GreaterThan = 11,

		[EnumMember]
		GreaterThanOrEqual = 12,

		[EnumMember]
		LessThan = 13,

		[EnumMember]
		LessThanOrEqual = 14,

		[EnumMember]
		IsNull = 15,

		[EnumMember]
		IsNotNull = 16,

		[EnumMember]
		None = 17
	}
}
