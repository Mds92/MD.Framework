﻿using System;
using System.Collections.Generic;
using System.Linq;
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

		public List<Expression<Func<TEntity, object>>> IncludedNavigationPropertiesExpression
		{
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
					foreach (var expression in value)
					{
						var selectorString = expression.Body.ToString();
						IncludedNavigationProperties.Add(selectorString.Remove(0, selectorString.IndexOf('.') + 1));
					}
				}
			}
		}

		public void AddIncludedNavigationProperty(Expression<Func<TEntity, object>> expression)
		{
			if (expression == null)
			{
				return;
			}
			if (IncludedNavigationProperties == null)
				IncludedNavigationProperties = new List<string>();

			var selectorString = expression.Body.ToString();
			IncludedNavigationProperties.Add(selectorString.Remove(0, selectorString.IndexOf('.') + 1));
		}

		public void AddRangeIncludedNavigationProperty(List<Expression<Func<TEntity, object>>> expressions)
		{
			if (expressions == null || !expressions.Any())
			{
				return;
			}

			if (IncludedNavigationProperties == null)
				IncludedNavigationProperties = new List<string>();
			foreach (var expression in expressions)
			{
				var selectorString = expression.Body.ToString();
				IncludedNavigationProperties.Add(selectorString.Remove(0, selectorString.IndexOf('.') + 1));
			}
		}

		public void RemoveIncludedNavigationProperty(Expression<Func<TEntity, object>> expression)
		{
			if (expression == null)
			{
				return;
			}
			if (IncludedNavigationProperties != null && IncludedNavigationProperties.Any())
			{
				var selectorString = expression.Body.ToString();
				IncludedNavigationProperties.Remove(selectorString.Remove(0, selectorString.IndexOf('.') + 1));
			}
		}

		[DataMember]
		public PaginationData PaginationData { get; set; }

		public ServicePredicateBuilder<TDestination> Cast<TDestination>() where TDestination : class
		{
			var result = new ServicePredicateBuilder<TDestination>();

			if (IncludedNavigationProperties != null && IncludedNavigationProperties.Count > 0)
			{
				result.IncludedNavigationProperties = new List<string>();
				foreach (var includedNavigationProperty in IncludedNavigationProperties)
				{
					var stringBuilder = new StringBuilder(includedNavigationProperty);
					result.IncludedNavigationProperties.Add(stringBuilder.ToString());
				}
			}

			if (PaginationData != null)
			{
				result.PaginationData = new PaginationData
				{
					ItemsPerPage = PaginationData.ItemsPerPage,
					PageNumber = PaginationData.PageNumber
				};
			}

			result.Criteria = Criteria.Cast<TDestination>();
			if (SortCondition != null)
				result.SortCondition = SortCondition.Cast<TDestination>();

			return result;
		}
	}
}
