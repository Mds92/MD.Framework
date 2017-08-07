using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MD.Framework.Utility.Core.Predicate
{
    public class SortCondition<TEntity>
    {
        private Type _entityType;
        private ParameterExpression _parameterExpression;
        private ParameterExpression _orderableParameterExpression;

        private SortCondition() { }
        
        private List<SortItem> _sortItems = new List<SortItem>();

        public static SortCondition<TEntity> None()
        {
            return new SortCondition<TEntity> { _entityType = typeof(TEntity) };
        }

        public static SortCondition<TEntity> OrderBy<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression)
        {
            if (selectorExpression == null)
                throw new ArgumentException("Selector string can not be null or empty", nameof(selectorExpression));

            var result = new SortCondition<TEntity> { _entityType = typeof(TEntity) };
            var item = new SortItem
            {
                PropertySelector = GetSelectorStringFromExpression(selectorExpression),
                SortDirection = SortDirection.Ascending
            };
            result._sortItems.Add(item);
            return result;
        }

        public static SortCondition<TEntity> OrderBy(string propertySelector)
        {
            if (string.IsNullOrEmpty(propertySelector))
                throw new ArgumentException("Selector string can not be null or empty", nameof(propertySelector));

            var result = new SortCondition<TEntity> { _entityType = typeof(TEntity) };
            var item = new SortItem
            {
                PropertySelector = propertySelector,
                SortDirection = SortDirection.Ascending
            };
            result._sortItems.Add(item);
            return result;
        }

        public static SortCondition<TEntity> OrderByDescending<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression)
        {
            if (selectorExpression == null)
                throw new ArgumentException("Selector string can not be null or empty", nameof(selectorExpression));

            var result = new SortCondition<TEntity> { _entityType = typeof(TEntity) };
            var item = new SortItem
            {
                PropertySelector = GetSelectorStringFromExpression(selectorExpression),
                SortDirection = SortDirection.Descending
            };
            result._sortItems.Add(item);
            return result;
        }

        public static SortCondition<TEntity> OrderByDescending(string propertySelector)
        {
            if (string.IsNullOrEmpty(propertySelector))
                throw new ArgumentException("Selector string can not be null or empty", nameof(propertySelector));

            var result = new SortCondition<TEntity> { _entityType = typeof(TEntity) };
            var item = new SortItem
            {
                PropertySelector = propertySelector,
                SortDirection = SortDirection.Descending
            };
            result._sortItems.Add(item);
            return result;
        }

        public SortCondition<TEntity> ThenBy<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression)
        {
            if (selectorExpression == null)
                throw new ArgumentException("Selector string can not be null or empty", nameof(selectorExpression));
            var item = new SortItem
            {
                PropertySelector = GetSelectorStringFromExpression(selectorExpression),
                SortDirection = SortDirection.Ascending
            };
            _sortItems.Add(item);
            return this;
        }

        public SortCondition<TEntity> ThenBy(string propertySelector)
        {
            if (string.IsNullOrEmpty(propertySelector))
                throw new ArgumentException("Selector string can not be null or empty", nameof(propertySelector));
            var item = new SortItem
            {
                PropertySelector = propertySelector,
                SortDirection = SortDirection.Ascending
            };
            _sortItems.Add(item);
            return this;
        }

        public SortCondition<TEntity> ThenByDescending<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression)
        {
            if (selectorExpression == null)
                throw new ArgumentException("Selector string can not be null or empty", nameof(selectorExpression));
            var item = new SortItem
            {
                PropertySelector = GetSelectorStringFromExpression(selectorExpression),
                SortDirection = SortDirection.Descending
            };
            _sortItems.Add(item);
            return this;
        }

        public SortCondition<TEntity> ThenByDescending(string propertySelector)
        {
            if (string.IsNullOrEmpty(propertySelector))
                throw new ArgumentException("Selector string can not be null or empty", nameof(propertySelector));
            var item = new SortItem
            {
                PropertySelector = propertySelector,
                SortDirection = SortDirection.Descending
            };
            _sortItems.Add(item);
            return this;
        }

        public SortCondition<TDestionation> Cast<TDestionation>() where TDestionation : class
        {
            var result = new SortCondition<TDestionation>
            {
                _entityType = typeof(TDestionation),
                _parameterExpression = _parameterExpression,
                _orderableParameterExpression = _orderableParameterExpression
            };
            foreach (var sortItem in _sortItems)
            {
                var newSortItem = new SortItem()
                {
                    PropertySelector = sortItem.PropertySelector,
                    SortDirection = sortItem.SortDirection
                };
                result._sortItems.Add(newSortItem);
            }
            return result;

        }

        public Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> GetIQueryableSortingExpression()
        {
            var sortExpression = GetSortingExpression(typeof(TEntity));
            if (sortExpression == null) return null;
            var funcLambdaExpression =
                Expression.Lambda<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>(sortExpression, _orderableParameterExpression);
            return funcLambdaExpression;
        }
        public Expression<Func<IEnumerable<TEntity>, IOrderedEnumerable<TEntity>>> GetIEnumerableSortingExpression()
        {
            var sortExpression = GetSortingExpression(typeof(TEntity), true);
            if (sortExpression == null) return null;
            var funcLambdaExpression =
                Expression.Lambda<Func<IEnumerable<TEntity>, IOrderedEnumerable<TEntity>>>(sortExpression, _orderableParameterExpression);
            return funcLambdaExpression;
        }

        private Expression GetSortingExpression(Type destinationType, bool isIEnumerable = false)
        {
            if (_sortItems == null || _sortItems.Count <= 0) return null;

            _entityType = destinationType;
            if (destinationType == null)
                _entityType = typeof(TEntity);

            _parameterExpression = Expression.Parameter(_entityType, "entity");
            _orderableParameterExpression = Expression.Parameter(isIEnumerable ? typeof(IEnumerable<TEntity>) : typeof(IQueryable<TEntity>), "f");

            var orderableType = typeof(Queryable);
            if (isIEnumerable)
                orderableType = typeof(Enumerable);

            MethodInfo orderByMethodInfo = null;
            Expression resultExpression = null;

            foreach (var sortingItem in _sortItems)
            {
                var memberExpression = GetSortMemberExpression(sortingItem.PropertySelector, _entityType, _parameterExpression);
                switch (sortingItem.SortDirection)
                {
                    case SortDirection.Ascending:
                        if (resultExpression != null)
                            orderByMethodInfo = orderableType.GetMethods().First(method => method.Name == "ThenBy" && method.GetParameters().Length == 2).MakeGenericMethod(_entityType, memberExpression.Type);
                        else
                            orderByMethodInfo = orderableType.GetMethods().First(method => method.Name == "OrderBy" && method.GetParameters().Length == 2).MakeGenericMethod(_entityType, memberExpression.Type);
                        break;

                    case SortDirection.Descending:
                        if (resultExpression != null)
                            orderByMethodInfo = orderableType.GetMethods().First(method => method.Name == "ThenByDescending" && method.GetParameters().Length == 2).MakeGenericMethod(_entityType, memberExpression.Type);
                        else
                            orderByMethodInfo = orderableType.GetMethods().First(method => method.Name == "OrderByDescending" && method.GetParameters().Length == 2).MakeGenericMethod(_entityType, memberExpression.Type);
                        break;
                }

                MethodCallExpression methodCallExpression;
                var lambdaExpression = Expression.Lambda(memberExpression, _parameterExpression);

                if (resultExpression != null)
                    methodCallExpression = Expression.Call(orderByMethodInfo, resultExpression, lambdaExpression);
                else
                    methodCallExpression = Expression.Call(orderByMethodInfo, _orderableParameterExpression, lambdaExpression);

                resultExpression = methodCallExpression;
            }
            return resultExpression;
        }

        private static MemberExpression GetSortMemberExpression(string selectorString, Type parameterExpressionType, ParameterExpression parameterExpression)
        {
            if (string.IsNullOrWhiteSpace(selectorString)) throw new ArgumentNullException(nameof(selectorString), "Selector string is not valid");
            var propertyParts = selectorString.Split('.');
            if (propertyParts.Any(string.IsNullOrWhiteSpace))
                throw new Exception($"Selector string \"{selectorString}\" format is not valid.");
            var firstPartOfSelector = propertyParts[0];

            var propertyInThisType = parameterExpressionType.GetProperty(firstPartOfSelector);
            if (propertyInThisType == null)
                throw new Exception($"Selector string \"{selectorString}\" is not exist in type \"{parameterExpressionType.Name}\".");

            var me = Expression.Property(parameterExpression, propertyInThisType);
            if (propertyParts.Length == 1)
                return me;

            return GetSortMemberExpression(string.Join(".", propertyParts, 1, propertyParts.Length - 1), me);
        }

        private static MemberExpression GetSortMemberExpression(string selectorString, MemberExpression memberExpression)
        {
            MemberExpression result;
            if (string.IsNullOrWhiteSpace(selectorString)) throw new ArgumentNullException(nameof(selectorString), "Selector string is not valid");
            var propertyParts = selectorString.Split('.');
            if (propertyParts.Any(string.IsNullOrWhiteSpace))
                throw new Exception($"Selector string \"{selectorString}\" format is not valid.");
            var firstPartOfSelector = propertyParts[0];

            var propertyInThisType = memberExpression.Type.GetProperty(firstPartOfSelector);
            if (propertyInThisType == null)
                throw new Exception($"Selector string \"{selectorString}\" is not exist in type \"{memberExpression.Type.Name}\".");

            var me = Expression.Property(memberExpression, propertyInThisType);

            if (propertyParts.Length == 1)
                result = Expression.Property(memberExpression, propertyInThisType);
            else
                result = GetSortMemberExpression(string.Join(".", propertyParts, 1, propertyParts.Length - 1), me);

            return result;
        }

        private static string GetSelectorStringFromExpression<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression)
        {
            var selectorString = selectorExpression.Body.ToString();
            return selectorString.Remove(0, selectorString.IndexOf('.') + 1);
        }

        public void SetSortItems(List<SortItem> sortItems)
        {
            _sortItems = new List<SortItem>();
            if (sortItems == null) return;
            foreach (var item in sortItems)
                _sortItems.Add(new SortItem { PropertySelector = item.PropertySelector, SortDirection = item.SortDirection });
        }

        public List<SortItem> GetSortItems()
        {
            return _sortItems;
        }
    }
}
