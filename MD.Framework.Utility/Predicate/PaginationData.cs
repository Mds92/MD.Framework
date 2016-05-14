using System;
using System.Runtime.Serialization;

namespace MD.Framework.Utility
{
	[Serializable]
	[DataContract]
	public class PaginationData
	{
		/// <summary>
		/// شماره صفحه مورد نظر که باید بزرگتر از صفر باشد
		/// </summary>
		[DataMember]
		public int PageNumber { get; set; }

		/// <summary>
		/// تعداد آیتم مورد نظر در هر صفحه
		/// </summary>
		[DataMember]
		public int ItemsPerPage { get; set; }
	}
}
