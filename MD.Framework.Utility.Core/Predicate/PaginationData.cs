namespace MD.Framework.Utility.Core.Predicate
{
	public class PaginationData
	{
		/// <summary>
		/// شماره صفحه مورد نظر که باید بزرگتر از صفر باشد
		/// </summary>
		public int PageNumber { get; set; }

		/// <summary>
		/// تعداد آیتم مورد نظر در هر صفحه
		/// </summary>
		public int ItemsPerPage { get; set; }
	}
}
