using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.SqlServer;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MD.Framework.Utility
{
	[Serializable]
	[DataContract]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    public class Criteria<TEntity> where TEntity : class
	{
		[DataMember]
		private Condition ConditionContainer { get; set; }
		private Type _entityType;
		private static Guid GetNewGuid()
		{
			return Guid.NewGuid();
		}

		private Criteria() { }

		#region True False

		public static Criteria<TEntity> True()
		{
			var entityType = typeof(TEntity);
			var criteria = new Criteria<TEntity>
			{
				ConditionContainer = new Condition
				{
					Tree = new ConditionTree
					{
						OperationType = OperatorEnum.None,
						NextLogicalOperator = LogicalOperatorEnum.And,
						Value = TrueFalseEnum.True,
					},
					EntityTypeName = entityType.Name
				},
				_entityType = entityType
			};
			criteria.ConditionContainer.Id = GetNewGuid();
			criteria.ConditionContainer.Tree.Id = GetNewGuid();
			return criteria;
		}

		public static Criteria<TEntity> False()
		{
			var entityType = typeof(TEntity);
			var criteria = new Criteria<TEntity>
			{
				ConditionContainer = new Condition
				{
					Tree = new ConditionTree
					{
						OperationType = OperatorEnum.None,
						NextLogicalOperator = LogicalOperatorEnum.Or,
						Value = TrueFalseEnum.False,
					},
					EntityTypeName = entityType.Name

				},
				_entityType = entityType
			};
			criteria.ConditionContainer.Id = GetNewGuid();
			criteria.ConditionContainer.Tree.Id = GetNewGuid();
			return criteria;
		}

		#endregion

		#region Or

		public Criteria<TEntity> Or(Criteria<TEntity> criteria)
		{
			if (criteria == null) return this;
			if (_entityType != criteria._entityType)
				throw new Exception($"criteria must be from '{_entityType.Assembly.FullName}' type");
            ConditionContainer.Tree.NextLogicalOperator = LogicalOperatorEnum.Or;
            ConditionContainer.Tree.ChildrenConditions.Add(criteria.ConditionContainer.Tree);
			return this;
		}

		public Criteria<TEntity> Or(string selectorString, OperatorEnum operationType, object value)
		{
			if (string.IsNullOrWhiteSpace(selectorString))
				throw new ArgumentException("Selector string can not be null or empty", nameof(selectorString));

			var targetPropertyType = GetTargetPropertyType(_entityType, selectorString);
			if (targetPropertyType != value.GetType() && operationType != OperatorEnum.Contain && operationType != OperatorEnum.NotContain)
				value = ChangeValueType(targetPropertyType, value);

			var newConditionTree = new ConditionTree
			{
				OperationType = operationType,
				Value = value,
				NextLogicalOperator = LogicalOperatorEnum.Or,
				SelectorString = selectorString,
				Id = GetNewGuid()
			};
            ConditionContainer.Tree.ChildrenConditions.Add(newConditionTree);
			return this;
		}

		public Criteria<TEntity> Or<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression, OperatorEnum operationType, object value)
		{
			if (selectorExpression == null)
				throw new ArgumentException("Selector string can not be null or empty", nameof(selectorExpression));

			var newConditionTree = new ConditionTree
			{
				OperationType = operationType,
				Value = value,
				NextLogicalOperator = LogicalOperatorEnum.Or,
				SelectorString = GetSelectorStringFromExpression(selectorExpression),
				Id = GetNewGuid()
			};
            ConditionContainer.Tree.ChildrenConditions.Add(newConditionTree);
			return this;
		}

		#endregion

		#region And

		public Criteria<TEntity> And(Criteria<TEntity> criteria)
		{
			if (criteria == null) return this;
			if (_entityType != criteria._entityType)
				throw new Exception($"criteria must be from '{_entityType.Assembly.FullName}' type");
            ConditionContainer.Tree.NextLogicalOperator = LogicalOperatorEnum.And;
            ConditionContainer.Tree.ChildrenConditions.Add(criteria.ConditionContainer.Tree);
			return this;
		}

		public Criteria<TEntity> And<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression, OperatorEnum operationType, object value)
		{
			if (selectorExpression == null)
				throw new ArgumentException("Selector string can not be null or empty", nameof(selectorExpression));

			var newConditionTree = new ConditionTree
			{
				OperationType = operationType,
				Value = value,
				NextLogicalOperator = LogicalOperatorEnum.And,
				SelectorString = GetSelectorStringFromExpression(selectorExpression),
				Id = GetNewGuid()
			};
            ConditionContainer.Tree.ChildrenConditions.Add(newConditionTree);
			return this;
		}

		public Criteria<TEntity> And(string selectorString, OperatorEnum operationType, object value)
		{
			if (string.IsNullOrWhiteSpace(selectorString))
				throw new ArgumentException("Selector string can not be null or empty", nameof(selectorString));

			var targetPropertyType = GetTargetPropertyType(_entityType, selectorString);
			if (value != null && targetPropertyType != value.GetType() && operationType != OperatorEnum.Contain && operationType != OperatorEnum.NotContain)
				value = ChangeValueType(targetPropertyType, value);

			var newConditionTree = new ConditionTree
			{
				OperationType = operationType,
				Value = value,
				NextLogicalOperator = LogicalOperatorEnum.And,
				SelectorString = selectorString,
				Id = GetNewGuid()
			};
            ConditionContainer.Tree.ChildrenConditions.Add(newConditionTree);
			return this;
		}

		#endregion

		#region Methods

		public Expression<Func<TDestination, bool>> TypedGetExpression<TDestination>() where TDestination : class
		{
			_checkedIds = new List<Guid>();
			var entityType = typeof(TDestination);
			var parameterExpression = Expression.Parameter(entityType, "entity");
			var resultExpression = ConvertConditionToExpression(ConditionContainer.Tree, entityType, parameterExpression);
			return Expression.Lambda<Func<TDestination, bool>>(resultExpression, parameterExpression);
		}

		public Criteria<TDestination> Cast<TDestination>() where TDestination : class
		{
			var result = new Criteria<TDestination>
			{
				_entityType = typeof(TDestination),
				ConditionContainer = new Condition
				{
					EntityTypeName = ConditionContainer.EntityTypeName,
					Id = ConditionContainer.Id,
					Tree = CopyConditionTree(ConditionContainer.Tree)
				}
			};
			return result;
		}

		private static ConditionTree CopyConditionTree(ConditionTree sourceConditionTree)
		{
			if (sourceConditionTree == null) return null;
			var result = new ConditionTree
			{
				Id = sourceConditionTree.Id,
				NextLogicalOperator = sourceConditionTree.NextLogicalOperator,
				OperationType = sourceConditionTree.OperationType,
				SelectorString = sourceConditionTree.SelectorString,
				Value = sourceConditionTree.Value,
				SerializedValue = sourceConditionTree.SerializedValue,
			};

			if (sourceConditionTree.ChildrenConditions != null && sourceConditionTree.ChildrenConditions.Count > 0)
			{
				result.ChildrenConditions = new List<ConditionTree>();
				foreach (var childrenCondition in sourceConditionTree.ChildrenConditions)
				{
					var clonedObject = CopyConditionTree(childrenCondition);
					if (clonedObject != null)
					{
						result.ChildrenConditions.Add(clonedObject);
					}
				}
			}
			return result;
		}

		public Expression<Func<TEntity, bool>> GetExpression()
		{
			_checkedIds = new List<Guid>();
			var entityType = typeof(TEntity);
			var parameterExpression = Expression.Parameter(entityType, "entity");
			var resultExpression = ConvertConditionToExpression(ConditionContainer.Tree, entityType, parameterExpression);
			return Expression.Lambda<Func<TEntity, bool>>(resultExpression, parameterExpression);
		}

		private List<Guid> _checkedIds;

		private Expression ConvertConditionToExpression(ConditionTree conditionTree, Type parameterExpressionType, ParameterExpression parameterExpression)
		{
			var resultExpression = GetConditionExpression(conditionTree, parameterExpressionType, parameterExpression);

			foreach (var childrenConditionTree in conditionTree.ChildrenConditions)
			{
				if (_checkedIds.Contains(childrenConditionTree.Id)) continue;
				_checkedIds.Add(childrenConditionTree.Id);

				var logicalOperator = childrenConditionTree.ChildrenConditions.Any()
					? ConditionContainer.Tree.NextLogicalOperator
					: childrenConditionTree.NextLogicalOperator;

				switch (logicalOperator)
				{
					case LogicalOperatorEnum.And:
						resultExpression = Expression.AndAlso(resultExpression, ConvertConditionToExpression(childrenConditionTree, parameterExpressionType, parameterExpression));
						break;

					case LogicalOperatorEnum.Or:
						resultExpression = Expression.OrElse(resultExpression, ConvertConditionToExpression(childrenConditionTree, parameterExpressionType, parameterExpression));
						break;
				}
			}

			return resultExpression;
		}

		private static Expression GetConditionExpression(ConditionTree conditionForConvert, Type parameterExpressionType, ParameterExpression parameterExpression)
		{
			if (conditionForConvert == null)
				throw new ArgumentNullException(nameof(conditionForConvert), "Condition tree is null");

			Expression result;

			#region True or False

			// True
			if (conditionForConvert.OperationType == OperatorEnum.None
				&& string.Equals(conditionForConvert.SerializedValue, ((int)TrueFalseEnum.True).ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase))
			{
				var constantExpression = Expression.Constant(1, typeof(int));
				var binaryExpression = Expression.Equal(constantExpression, constantExpression);
				return binaryExpression;

			}
			// False
			if (conditionForConvert.OperationType == OperatorEnum.None
				&& string.Equals(conditionForConvert.SerializedValue, ((int)TrueFalseEnum.False).ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase))
			{
				var constantExpression = Expression.Constant(1, typeof(int));
				var binaryExpression = Expression.NotEqual(constantExpression, constantExpression);
				return binaryExpression;
			}

			#endregion

			object valueObject;
			ConstantExpression rightSide = null;
			Expression collection = null;

			// نوع پروپرتی اصلی در دیتابیس
			var leftSidePropertyType = GetTargetPropertyType(parameterExpressionType, conditionForConvert.SelectorString);
			var isNumericType = leftSidePropertyType.IsNumericType();
			var isString = leftSidePropertyType == typeof(string);

			MethodInfo trimMethodInfo = null;
			MethodInfo trimStartMethodInfo = null;
			MethodInfo trimEndMethodInfo = null;
			MethodInfo startsWithMethodInfo = null;
			MethodInfo endsWithMethodInfo = null;
			MethodInfo containsMethodInfo = null;
			MethodInfo stringCompareMethodInfo = null;
			Expression argumentsExpression = null;
			var leftSide = GetLeftSide(conditionForConvert.SelectorString, parameterExpressionType, parameterExpression);

			switch (conditionForConvert.OperationType)
			{
				case OperatorEnum.Contain:
				case OperatorEnum.NotContain:
					var listType = typeof(ICollection<>);
					var numericGenericType = listType.MakeGenericType(leftSidePropertyType);
					valueObject = JsonConvert.DeserializeObject(!string.IsNullOrEmpty(conditionForConvert.SerializedValue) ? conditionForConvert.SerializedValue : null, numericGenericType);
					containsMethodInfo = numericGenericType.GetMethod("Contains", new[] { leftSidePropertyType });
					collection = Expression.Constant(valueObject);
					break;

				case OperatorEnum.Like:
				case OperatorEnum.NotLike:
					containsMethodInfo = typeof(string).GetMethod("Contains", new[] { typeof(string) });
					trimMethodInfo = typeof(string).GetMethod("Trim", new Type[] { });
					valueObject = JsonConvert.DeserializeObject(!string.IsNullOrEmpty(conditionForConvert.SerializedValue) ? conditionForConvert.SerializedValue : null, leftSidePropertyType);
					rightSide = Expression.Constant(valueObject, leftSidePropertyType);
					break;

				case OperatorEnum.NotStartsWith:
				case OperatorEnum.StartsWith:
					startsWithMethodInfo = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
					trimStartMethodInfo = typeof(string).GetMethod("TrimStart", BindingFlags.Public | BindingFlags.Instance);
					argumentsExpression = Expression.NewArrayInit(typeof(char));
					valueObject = JsonConvert.DeserializeObject(!string.IsNullOrEmpty(conditionForConvert.SerializedValue) ? conditionForConvert.SerializedValue : null, leftSidePropertyType);
					rightSide = Expression.Constant(valueObject, leftSidePropertyType);
					break;

				case OperatorEnum.NotEndsWith:
				case OperatorEnum.EndsWith:
					endsWithMethodInfo = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
					trimEndMethodInfo = typeof(string).GetMethod("TrimEnd", BindingFlags.Public | BindingFlags.Instance);
					argumentsExpression = Expression.NewArrayInit(typeof(char));
					valueObject = JsonConvert.DeserializeObject(!string.IsNullOrEmpty(conditionForConvert.SerializedValue) ? conditionForConvert.SerializedValue : null, leftSidePropertyType);
					rightSide = Expression.Constant(valueObject, leftSidePropertyType);
					break;

				case OperatorEnum.IsNull:
				case OperatorEnum.IsNotNull:
					rightSide = Expression.Constant(null);
					valueObject = null;
					break;

				case OperatorEnum.GreaterThanOrEqual:
				case OperatorEnum.GreaterThan:
				case OperatorEnum.LessThanOrEqual:
				case OperatorEnum.LessThan:
					if (isString)
					{
						stringCompareMethodInfo = typeof(string).GetMethod("CompareTo", new[] { typeof(string) });
						valueObject = JsonConvert.DeserializeObject(!string.IsNullOrEmpty(conditionForConvert.SerializedValue) ? conditionForConvert.SerializedValue : null, leftSidePropertyType);
						rightSide = Expression.Constant(valueObject, leftSidePropertyType);
					}
					else
					{
						valueObject = JsonConvert.DeserializeObject(!string.IsNullOrEmpty(conditionForConvert.SerializedValue) ? conditionForConvert.SerializedValue : null, leftSidePropertyType);
						rightSide = Expression.Constant(valueObject, leftSidePropertyType);
					}
					break;
				default:
					valueObject = JsonConvert.DeserializeObject(!string.IsNullOrEmpty(conditionForConvert.SerializedValue) ? conditionForConvert.SerializedValue : null, leftSidePropertyType);
					rightSide = Expression.Constant(valueObject, leftSidePropertyType);
					break;
			}

			UnaryExpression unaryExpression = null;
			ConstantExpression convertedRightSideConstantExpression = null;
			var nullableDoubleType = typeof(double?);
			MethodInfo stringConvertMethodInfo = null;

			if (isNumericType)
			{
				stringConvertMethodInfo = typeof(SqlFunctions).GetMethod("StringConvert", new[] { nullableDoubleType });
				unaryExpression = Expression.Convert(leftSide, nullableDoubleType);
				convertedRightSideConstantExpression = Expression.Constant(valueObject?.ToString());
			}

			switch (conditionForConvert.OperationType)
			{
				case OperatorEnum.Equal:
				case OperatorEnum.IsNull:
					result = Expression.Equal(leftSide, rightSide);
					break;

				case OperatorEnum.NotEqual:
				case OperatorEnum.IsNotNull:
					result = Expression.NotEqual(leftSide, rightSide);
					break;

				case OperatorEnum.Contain:
					result = Expression.Call(collection, containsMethodInfo, leftSide);
					break;

				case OperatorEnum.NotContain:
					result = Expression.Not(Expression.Call(collection, containsMethodInfo, leftSide));
					break;

				case OperatorEnum.Like:
					if (isNumericType)
					{
						var methodCallExpression1 = Expression.Call(stringConvertMethodInfo, unaryExpression);
						var methodCallExpression2 = Expression.Call(methodCallExpression1, trimMethodInfo);
						result = Expression.Call(methodCallExpression2, containsMethodInfo, convertedRightSideConstantExpression);
					}
					else
					{
						result = Expression.Call(leftSide, containsMethodInfo, rightSide);
					}
					break;

				case OperatorEnum.NotLike:
					if (isNumericType)
					{
						var methodCallExpression1 = Expression.Call(stringConvertMethodInfo, unaryExpression);
						var methodCallExpression2 = Expression.Call(methodCallExpression1, trimMethodInfo);
						result = Expression.Call(methodCallExpression2, containsMethodInfo, convertedRightSideConstantExpression);
						result = Expression.Not(result);
					}
					else
					{
						result = Expression.Not(Expression.Call(leftSide, containsMethodInfo, rightSide));
					}
					break;

				case OperatorEnum.StartsWith:
					if (isNumericType)
					{
						var methodCallExpression1 = Expression.Call(stringConvertMethodInfo, unaryExpression);
						var methodCallExpression2 = Expression.Call(methodCallExpression1, trimStartMethodInfo, argumentsExpression);
						result = Expression.Call(methodCallExpression2, startsWithMethodInfo, convertedRightSideConstantExpression);
					}
					else
					{
						result = Expression.Call(leftSide, startsWithMethodInfo, rightSide);
					}
					break;

				case OperatorEnum.NotStartsWith:
					if (isNumericType)
					{
						var methodCallExpression1 = Expression.Call(stringConvertMethodInfo, unaryExpression);
						var methodCallExpression2 = Expression.Call(methodCallExpression1, trimStartMethodInfo, argumentsExpression);
						result = Expression.Call(methodCallExpression2, startsWithMethodInfo, convertedRightSideConstantExpression);
						result = Expression.Not(result);
					}
					else
					{
						result = Expression.Not(Expression.Call(leftSide, startsWithMethodInfo, rightSide));
					}
					break;

				case OperatorEnum.EndsWith:
					if (isNumericType)
					{
						var methodCallExpression1 = Expression.Call(stringConvertMethodInfo, unaryExpression);
						var methodCallExpression2 = Expression.Call(methodCallExpression1, trimEndMethodInfo, argumentsExpression);
						result = Expression.Call(methodCallExpression2, endsWithMethodInfo, convertedRightSideConstantExpression);
					}
					else
					{
						result = Expression.Call(leftSide, endsWithMethodInfo, rightSide);
					}
					break;

				case OperatorEnum.NotEndsWith:
					if (isNumericType)
					{
						var methodCallExpression1 = Expression.Call(stringConvertMethodInfo, unaryExpression);
						var methodCallExpression2 = Expression.Call(methodCallExpression1, trimEndMethodInfo, argumentsExpression);
						result = Expression.Call(methodCallExpression2, endsWithMethodInfo, convertedRightSideConstantExpression);
						result = Expression.Not(result);
					}
					else
					{
						result = Expression.Not(Expression.Call(leftSide, endsWithMethodInfo, rightSide));
					}
					break;

				case OperatorEnum.GreaterThan:
					if (isString)
					{
						result = Expression.GreaterThan(Expression.Call(leftSide, stringCompareMethodInfo, rightSide), Expression.Constant(0));
					}
					else
					{
						result = Expression.GreaterThan(leftSide, rightSide);
					}
					break;

				case OperatorEnum.GreaterThanOrEqual:
					if (isString)
					{
						result = Expression.GreaterThanOrEqual(Expression.Call(leftSide, stringCompareMethodInfo, rightSide), Expression.Constant(0));
					}
					else
					{
						result = Expression.GreaterThanOrEqual(leftSide, rightSide);
					}
					break;

				case OperatorEnum.LessThan:
					if (isString)
					{
						result = Expression.LessThan(Expression.Call(leftSide, stringCompareMethodInfo, rightSide), Expression.Constant(0));
					}
					else
					{
						result = Expression.LessThan(leftSide, rightSide);
					}
					break;

				case OperatorEnum.LessThanOrEqual:
					if (isString)
					{
						result = Expression.LessThanOrEqual(Expression.Call(leftSide, stringCompareMethodInfo, rightSide), Expression.Constant(0));
					}
					else
					{
						result = Expression.LessThanOrEqual(leftSide, rightSide);
					}
					break;

				default:
					throw new ArgumentException("Argument is not valid beacuse of operation type", nameof(conditionForConvert));
			}

			return result;
		}

		private static Expression GetLeftSide(string selectorString, Type parameterExpressionType, ParameterExpression parameterExpression)
		{
			if (string.IsNullOrWhiteSpace(selectorString)) throw new ArgumentNullException(nameof(selectorString), "Selector string is not valid");
			var propertyParts = selectorString.Split(new[] { '.' });
			if (propertyParts.Any(string.IsNullOrWhiteSpace))
				throw new Exception($"Selector string \"{selectorString}\" format is not valid.");
			var firstPartOfSelector = GetInvariantCultrueString(propertyParts[0]);

			var propertyInThisType = parameterExpressionType.GetProperty(firstPartOfSelector);
			if (propertyInThisType == null)
				throw new Exception($"Selector string \"{selectorString}\" is not exist in type \"{parameterExpressionType.Name}\".");

			Expression expression = Expression.Property(parameterExpression, propertyInThisType);
			if (propertyParts.Length == 1)
				return expression;
			return GetLeftSide(string.Join(".", propertyParts, 1, propertyParts.Length - 1), expression);
		}

		private static Expression GetLeftSide(string selectorString, Expression inputExpression)
		{
			Expression resultExpression;
			var inputExpressionType = inputExpression.Type;

			if (string.IsNullOrWhiteSpace(selectorString)) throw new ArgumentNullException(nameof(selectorString), "Selector string is not valid");
			var propertyParts = selectorString.Split(new[] { '.' });
			if (propertyParts.Any(string.IsNullOrWhiteSpace))
				throw new Exception($"Selector string \"{selectorString}\" format is not valid.");
			var firstPartOfSelector = GetInvariantCultrueString(propertyParts[0]);

			PropertyInfo selectedPropertyInfo = null;
			MethodInfo selectedMethodInfo = null;
			if (firstPartOfSelector.IndexOf('(') > 0)
			{
				var methodName = firstPartOfSelector.Remove(firstPartOfSelector.IndexOf('('));
				selectedMethodInfo = inputExpressionType.GetMethod(methodName, new Type[0]);
				resultExpression = Expression.Call(inputExpression, selectedMethodInfo);
			}
			else
			{
				selectedPropertyInfo = inputExpressionType.GetProperty(firstPartOfSelector);
				resultExpression = Expression.Property(inputExpression, selectedPropertyInfo);
			}

			if (selectedPropertyInfo == null && selectedMethodInfo == null)
				throw new Exception($"Selector string \"{selectorString}\" is not exist in type \"{inputExpression.Type.Name}\".");

			if (propertyParts.Length != 1)
				resultExpression = GetLeftSide(string.Join(".", propertyParts, 1, propertyParts.Length - 1), resultExpression);

			return resultExpression;
		}

		private static string GetInvariantCultrueString(string input)
		{
			if (string.IsNullOrEmpty(input))
				return string.Empty;
			return input.ToString(CultureInfo.InvariantCulture);
		}

		private static string GetSelectorStringFromExpression<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression)
		{
			var selectorString = selectorExpression.Body.ToString();
			return selectorString.Remove(0, selectorString.IndexOf('.') + 1);
		}

		private static Type GetTargetPropertyType(Type entityType, string selectorString)
		{
			if (string.IsNullOrWhiteSpace(selectorString)) return null;
			var propertyParts = selectorString.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(q => q.Trim())
				.Where(q => !string.IsNullOrEmpty(q))
				.ToArray();
			var firstPartOfSelector = propertyParts[0].ToString(CultureInfo.InvariantCulture);
			PropertyInfo selectedPropertyInfo = null;
			MethodInfo selectedMethodInfo = null;
			if (firstPartOfSelector.IndexOf('(') > 0)
			{
				var methodName = firstPartOfSelector.Remove(firstPartOfSelector.IndexOf('('));
				selectedMethodInfo = entityType.GetMethod(methodName, new Type[0]);
			}
			else
				selectedPropertyInfo = entityType.GetProperty(firstPartOfSelector);
			if (selectedPropertyInfo == null && selectedMethodInfo == null)
				throw new Exception($"Selector string \"{selectorString}\" is not exist in type \"{entityType.Name}\".");
			if (propertyParts.Length != 1)
				return GetTargetPropertyType(selectedPropertyInfo.PropertyType, string.Join(".", propertyParts, 1, propertyParts.Length - 1));
			if (selectedPropertyInfo != null)
				return selectedPropertyInfo.PropertyType;
			return selectedMethodInfo.ReturnType;
		}

		private static object ChangeValueType(Type targetPropertyType, object value)
		{
			var propertyValueInString = value.ToString().ToEnglishNumber();

			// Changing type

			object propertyValue;
			if (targetPropertyType == typeof(DateTime) || targetPropertyType == typeof(DateTime?))
			{
				if (PersianDateTime.PersianDateTime.IsChristianDate(propertyValueInString))
					propertyValue = DateTime.Parse(propertyValueInString);
				else
					propertyValue = PersianDateTime.PersianDateTime.Parse(propertyValueInString).ToDateTime();
			}
			else if (targetPropertyType.IsNumericType())
			{
				try
				{
					var typeConverter = TypeDescriptor.GetConverter(targetPropertyType);
					propertyValue = typeConverter.ConvertFrom(null, CultureInfo.InvariantCulture, propertyValueInString);
				}
				catch
				{
					var fieldInfo = targetPropertyType.GetField("MinValue");
					propertyValue = fieldInfo.GetRawConstantValue();
				}
			}
			else if (targetPropertyType.IsBoolean())
			{
				try
				{
					var typeConverter = TypeDescriptor.GetConverter(targetPropertyType);
					propertyValue = typeConverter.ConvertFrom(null, CultureInfo.InvariantCulture, propertyValueInString.Trim());
				}
				catch
				{ 
					propertyValue = false;
				}
				// نال کردن مقدار پروپرتی در صورتی که فاقد مقدار بود 
				// و قابل نال شدن بود
				if (propertyValue != null && propertyValue.Equals("") && Nullable.GetUnderlyingType(targetPropertyType) != null)
					propertyValue = null;
			}
			else
				propertyValue = Convert.ChangeType(propertyValueInString, targetPropertyType);

			return propertyValue;
		}

		#endregion
	}
}
