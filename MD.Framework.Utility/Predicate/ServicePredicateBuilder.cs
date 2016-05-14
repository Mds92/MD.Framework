using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;

namespace MD.Framework.Utility
{
	[Serializable]
	[DataContract]
	public class ServicePredicateBuilder<TEntity> where TEntity : class
	{
		/// <summary>
		/// شروط
		/// </summary>
		[DataMember]
		public Criteria<TEntity> Criteria { get; set; }

		/// <summary>
		/// مرتب سازی
		/// </summary>
		[DataMember]
		public SortCondition<TEntity> SortCondition { get; set; }

		[DataMember]
		public List<string> IncludedNavigationProperties { get; set; }

		private List<Expression<Func<TEntity, object>>> _includedNavigationPropertiesExpression;
		public List<Expression<Func<TEntity, object>>> IncludedNavigationPropertiesExpression
		{
			get { return _includedNavigationPropertiesExpression; }
			set
			{
				if (value == null)
				{
					IncludedNavigationProperties = new List<string>();
				}
				else
				{
					if (IncludedNavigationProperties == null)
						IncludedNavigationProperties = new List<string>();
					foreach (Expression<Func<TEntity, object>> expression in value)
					{
						string selectorString = expression.Body.ToString();
						IncludedNavigationProperties.Add(selectorString.Remove(0, selectorString.IndexOf('.') + 1));
					}
				}
				_includedNavigationPropertiesExpression = value;
			}
		}

		[DataMember]
		public PaginationData PaginationData { get; set; }

		public ServicePredicateBuilder<TDestination> Cast<TDestination>() where TDestination : class
		{
			ServicePredicateBuilder<TDestination> result = new ServicePredicateBuilder<TDestination>();

			if (this.IncludedNavigationProperties != null && this.IncludedNavigationProperties.Count > 0)
			{
				result.IncludedNavigationProperties = new List<string>();
				foreach (string includedNavigationProperty in this.IncludedNavigationProperties)
				{
					StringBuilder stringBuilder = new StringBuilder(includedNavigationProperty);
					result.IncludedNavigationProperties.Add(stringBuilder.ToString());
				}
			}

			if (PaginationData != null)
			{
				result.PaginationData = new PaginationData
				{
					ItemsPerPage = this.PaginationData.ItemsPerPage,
					PageNumber = this.PaginationData.PageNumber
				};
			}

			result.Criteria = this.Criteria.Cast<TDestination>();
			if (this.SortCondition != null)
				result.SortCondition = this.SortCondition.Cast<TDestination>();

			return result;
		}
	}
}
