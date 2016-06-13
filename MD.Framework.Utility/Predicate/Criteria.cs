using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.SqlServer;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace MD.Framework.Utility
{
	[Serializable]
	[DataContract]
	public class Criteria<TEntity> where TEntity : class
	{
		[DataMember]
		private Condition ConditionContainer { get; set; }
		private Type _entityType;
		private readonly static ObjectIDGenerator IdGenerator = new ObjectIDGenerator();
		private static bool _firstTime;
		private static readonly Type StringType = typeof(string);

		private Criteria() { }

		#region True False

		public static Criteria<TEntity> True()
		{
			Type entityType = typeof(TEntity);
			Criteria<TEntity> critaria = new Criteria<TEntity>
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
			critaria.ConditionContainer.Id = IdGenerator.GetId(critaria.ConditionContainer, out _firstTime);
			critaria.ConditionContainer.Tree.Id = IdGenerator.GetId(critaria.ConditionContainer.Tree, out _firstTime);
			return critaria;
		}

		public static Criteria<TEntity> False()
		{
			Type entityType = typeof(TEntity);
			Criteria<TEntity> critaria = new Criteria<TEntity>
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
			critaria.ConditionContainer.Id = IdGenerator.GetId(critaria.ConditionContainer, out _firstTime);
			critaria.ConditionContainer.Tree.Id = IdGenerator.GetId(critaria.ConditionContainer.Tree, out _firstTime);
			return critaria;
		}

		#endregion

		#region Or

		public Criteria<TEntity> Or(Criteria<TEntity> critaria)
		{
			if (critaria == null) return this;
			if (this._entityType != critaria._entityType)
				throw new Exception(string.Format("critaria must be from '{0}' type", this._entityType.Assembly.FullName));
			this.ConditionContainer.Tree.NextLogicalOperator = LogicalOperatorEnum.Or;
			this.ConditionContainer.Tree.ChildrenConditions.Add(critaria.ConditionContainer.Tree);
			return this;
		}

		public Criteria<TEntity> Or(string selectorString, OperatorEnum operationType, object value)
		{
			if (string.IsNullOrWhiteSpace(selectorString))
				throw new ArgumentException("Selector string can not be null or empty", "selectorString");

			Type targetPropertyType = GetTargetPropertyType(_entityType, selectorString);
			if (targetPropertyType != value.GetType() && operationType != OperatorEnum.Contain && operationType != OperatorEnum.NotContain)
				value = ChangeValueType(targetPropertyType, value);

			ConditionTree newConditionTree = new ConditionTree
			{
				OperationType = operationType,
				Value = value,
				NextLogicalOperator = LogicalOperatorEnum.Or,
				SelectorString = selectorString,
			};
			newConditionTree.Id = IdGenerator.GetId(newConditionTree, out _firstTime);
			this.ConditionContainer.Tree.ChildrenConditions.Add(newConditionTree);
			return this;
		}

		public Criteria<TEntity> Or<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression, OperatorEnum operationType, object value)
		{
			if (selectorExpression == null)
				throw new ArgumentException("Selector string can not be null or empty", "selectorExpression");

			ConditionTree newConditionTree = new ConditionTree
			{
				OperationType = operationType,
				Value = value,
				NextLogicalOperator = LogicalOperatorEnum.Or,
				SelectorString = GetSelectorStringFromExpression(selectorExpression)
			};
			newConditionTree.Id = IdGenerator.GetId(newConditionTree, out _firstTime);
			this.ConditionContainer.Tree.ChildrenConditions.Add(newConditionTree);
			return this;
		}

		#endregion

		#region And

		public Criteria<TEntity> And(Criteria<TEntity> critaria)
		{
			if (critaria == null) return this;
			if (this._entityType != critaria._entityType)
				throw new Exception(string.Format("critaria must be from '{0}' type", this._entityType.Assembly.FullName));
			this.ConditionContainer.Tree.NextLogicalOperator = LogicalOperatorEnum.And;
			this.ConditionContainer.Tree.ChildrenConditions.Add(critaria.ConditionContainer.Tree);
			return this;
		}

		public Criteria<TEntity> And<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression, OperatorEnum operationType, object value)
		{
			if (selectorExpression == null)
				throw new ArgumentException("Selector string can not be null or empty", "selectorExpression");

			ConditionTree newConditionTree = new ConditionTree
			{
				OperationType = operationType,
				Value = value,
				NextLogicalOperator = LogicalOperatorEnum.And,
				SelectorString = GetSelectorStringFromExpression(selectorExpression)
			};
			newConditionTree.Id = IdGenerator.GetId(newConditionTree, out _firstTime);
			this.ConditionContainer.Tree.ChildrenConditions.Add(newConditionTree);
			return this;
		}

		public Criteria<TEntity> And(string selectorString, OperatorEnum operationType, object value)
		{
			if (string.IsNullOrWhiteSpace(selectorString))
				throw new ArgumentException("Selector string can not be null or empty", "selectorString");

			Type targetPropertyType = GetTargetPropertyType(_entityType, selectorString);
			if (value != null && targetPropertyType != value.GetType() && operationType != OperatorEnum.Contain && operationType != OperatorEnum.NotContain)
				value = ChangeValueType(targetPropertyType, value);

			ConditionTree newConditionTree = new ConditionTree
			{
				OperationType = operationType,
				Value = value,
				NextLogicalOperator = LogicalOperatorEnum.And,
				SelectorString = selectorString,
			};
			newConditionTree.Id = IdGenerator.GetId(newConditionTree, out _firstTime);
			this.ConditionContainer.Tree.ChildrenConditions.Add(newConditionTree);
			return this;
		}

		#endregion

		#region Methods

		public Expression<Func<TDestination, bool>> TypedGetExpression<TDestination>() where TDestination : class
		{
			_checkedIds = new List<double>();
			Type entityType = typeof(TDestination);
			ParameterExpression parameterExpression = Expression.Parameter(entityType, "entity");
			Expression resultExpression = ConvertConditionToExpresion(this.ConditionContainer.Tree, entityType, parameterExpression);
			return Expression.Lambda<Func<TDestination, bool>>(resultExpression, parameterExpression);
		}

		public Criteria<TDestionation> Cast<TDestionation>() where TDestionation : class
		{
			Criteria<TDestionation> result = new Criteria<TDestionation>
			{
				_entityType = typeof(TDestionation),
				ConditionContainer = new Condition
				{
					EntityTypeName = this.ConditionContainer.EntityTypeName,
					Id = this.ConditionContainer.Id,
					Tree = CopyConditionTree(this.ConditionContainer.Tree)
				}
			};
			return result;
		}

		private static ConditionTree CopyConditionTree(ConditionTree sourceConditionTree)
		{
			if (sourceConditionTree == null) return null;
			ConditionTree result = new ConditionTree
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
					ConditionTree clonedObject = CopyConditionTree(childrenCondition);
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
			_checkedIds = new List<double>();
			Type entityType = typeof(TEntity);
			ParameterExpression parameterExpression = Expression.Parameter(entityType, "entity");
			Expression resultExpression = ConvertConditionToExpresion(this.ConditionContainer.Tree, entityType, parameterExpression);
			return Expression.Lambda<Func<TEntity, bool>>(resultExpression, parameterExpression);
		}

		private List<double> _checkedIds;

		private Expression ConvertConditionToExpresion(ConditionTree conditionTree, Type parameterExpressionType, ParameterExpression parameterExpression)
		{
			Expression resultExpression = GetConditionExpression(conditionTree, parameterExpressionType, parameterExpression);

			foreach (ConditionTree childrenConditionTree in conditionTree.ChildrenConditions)
			{
				if (_checkedIds.Contains(childrenConditionTree.Id)) continue;
				_checkedIds.Add(childrenConditionTree.Id);

				switch (childrenConditionTree.NextLogicalOperator)
				{
					case LogicalOperatorEnum.And:
						resultExpression = Expression.AndAlso(resultExpression, ConvertConditionToExpresion(childrenConditionTree, parameterExpressionType, parameterExpression));
						break;

					case LogicalOperatorEnum.Or:
						resultExpression = Expression.OrElse(resultExpression, ConvertConditionToExpresion(childrenConditionTree, parameterExpressionType, parameterExpression));
						break;
				}
			}

			return resultExpression;
		}

		private static Expression GetConditionExpression(ConditionTree conditionForConvert, Type parameterExpressionType, ParameterExpression parameterExpression)
		{
			if (conditionForConvert == null)
				throw new ArgumentNullException("conditionForConvert", "Condition tree is null");

			Expression result;

			#region True or False

			// True
			if (conditionForConvert.OperationType == OperatorEnum.None
				&& string.Equals(conditionForConvert.SerializedValue, ((int)TrueFalseEnum.True).ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase))
			{
				ConstantExpression constantExpression = Expression.Constant(1, typeof(int));
				BinaryExpression binaryExpression = Expression.Equal(constantExpression, constantExpression);
				return binaryExpression;

			}
			// False
			if (conditionForConvert.OperationType == OperatorEnum.None
				&& string.Equals(conditionForConvert.SerializedValue, ((int)TrueFalseEnum.False).ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase))
			{
				ConstantExpression constantExpression = Expression.Constant(1, typeof(int));
				BinaryExpression binaryExpression = Expression.NotEqual(constantExpression, constantExpression);
				return binaryExpression;
			}

			#endregion

			object valueObject;
			ConstantExpression rightSide = null;
			Expression collection = null;

			// نوع پروپرتی اصلی در دیتابیس
			Type leftSidePropertyType = GetTargetPropertyType(parameterExpressionType, conditionForConvert.SelectorString);
			bool isNumericType = leftSidePropertyType.IsNumericType();
			bool isString = leftSidePropertyType == StringType;

			MethodInfo trimMethodInfo = null;
			MethodInfo trimStartMethodInfo = null;
			MethodInfo trimEndMethodInfo = null;
			MethodInfo startsWithMethodInfo = null;
			MethodInfo endsWithMethodInfo = null;
			MethodInfo containsMethodInfo = null;
			MethodInfo stringCompareMethodInfo = null;
			Expression argumantsExpression = null;
			Expression leftSile = GetLeftSide(conditionForConvert.SelectorString, parameterExpressionType, parameterExpression);

			switch (conditionForConvert.OperationType)
			{
				case OperatorEnum.Contain:
				case OperatorEnum.NotContain:
					Type listType = typeof(ICollection<>);
					Type numbericGenericType = listType.MakeGenericType(leftSidePropertyType);
					valueObject = JsonConvert.DeserializeObject(!string.IsNullOrEmpty(conditionForConvert.SerializedValue) ? conditionForConvert.SerializedValue : null, numbericGenericType);
					containsMethodInfo = numbericGenericType.GetMethod("Contains", new[] { leftSidePropertyType });
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
					argumantsExpression = Expression.NewArrayInit(typeof(char));
					valueObject = JsonConvert.DeserializeObject(!string.IsNullOrEmpty(conditionForConvert.SerializedValue) ? conditionForConvert.SerializedValue : null, leftSidePropertyType);
					rightSide = Expression.Constant(valueObject, leftSidePropertyType);
					break;

				case OperatorEnum.NotEndsWith:
				case OperatorEnum.EndsWith:
					endsWithMethodInfo = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
					trimEndMethodInfo = typeof(string).GetMethod("TrimEnd", BindingFlags.Public | BindingFlags.Instance);
					argumantsExpression = Expression.NewArrayInit(typeof(char));
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
			Type nullableDoubleType = typeof(double?);
			MethodInfo stringConvertMethodInfo = null;

			if (isNumericType)
			{
				stringConvertMethodInfo = typeof(SqlFunctions).GetMethod("StringConvert", new[] { nullableDoubleType });
				unaryExpression = Expression.Convert(leftSile, nullableDoubleType);
				convertedRightSideConstantExpression = Expression.Constant(valueObject == null ? null : valueObject.ToString());
			}

			switch (conditionForConvert.OperationType)
			{
				case OperatorEnum.Equal:
				case OperatorEnum.IsNull:
					result = Expression.Equal(leftSile, rightSide);
					break;

				case OperatorEnum.NotEqual:
				case OperatorEnum.IsNotNull:
					result = Expression.NotEqual(leftSile, rightSide);
					break;

				case OperatorEnum.Contain:
					result = Expression.Call(collection, containsMethodInfo, leftSile);
					break;

				case OperatorEnum.NotContain:
					result = Expression.Not(Expression.Call(collection, containsMethodInfo, leftSile));
					break;

				case OperatorEnum.Like:
					if (isNumericType)
					{
						MethodCallExpression methodCallExpression1 = Expression.Call(stringConvertMethodInfo, unaryExpression);
						MethodCallExpression methodCallExpression2 = Expression.Call(methodCallExpression1, trimMethodInfo);
						result = Expression.Call(methodCallExpression2, containsMethodInfo, convertedRightSideConstantExpression);
					}
					else
					{
						result = Expression.Call(leftSile, containsMethodInfo, rightSide);
					}
					break;

				case OperatorEnum.NotLike:
					if (isNumericType)
					{
						MethodCallExpression methodCallExpression1 = Expression.Call(stringConvertMethodInfo, unaryExpression);
						MethodCallExpression methodCallExpression2 = Expression.Call(methodCallExpression1, trimMethodInfo);
						result = Expression.Call(methodCallExpression2, containsMethodInfo, convertedRightSideConstantExpression);
						result = Expression.Not(result);
					}
					else
					{
						result = Expression.Not(Expression.Call(leftSile, containsMethodInfo, rightSide));
					}
					break;

				case OperatorEnum.StartsWith:
					if (isNumericType)
					{
						MethodCallExpression methodCallExpression1 = Expression.Call(stringConvertMethodInfo, unaryExpression);
						MethodCallExpression methodCallExpression2 = Expression.Call(methodCallExpression1, trimStartMethodInfo, argumantsExpression);
						result = Expression.Call(methodCallExpression2, startsWithMethodInfo, convertedRightSideConstantExpression);
					}
					else
					{
						result = Expression.Call(leftSile, startsWithMethodInfo, rightSide);
					}
					break;

				case OperatorEnum.NotStartsWith:
					if (isNumericType)
					{
						MethodCallExpression methodCallExpression1 = Expression.Call(stringConvertMethodInfo, unaryExpression);
						MethodCallExpression methodCallExpression2 = Expression.Call(methodCallExpression1, trimStartMethodInfo, argumantsExpression);
						result = Expression.Call(methodCallExpression2, startsWithMethodInfo, convertedRightSideConstantExpression);
						result = Expression.Not(result);
					}
					else
					{
						result = Expression.Not(Expression.Call(leftSile, startsWithMethodInfo, rightSide));
					}
					break;

				case OperatorEnum.EndsWith:
					if (isNumericType)
					{
						MethodCallExpression methodCallExpression1 = Expression.Call(stringConvertMethodInfo, unaryExpression);
						MethodCallExpression methodCallExpression2 = Expression.Call(methodCallExpression1, trimEndMethodInfo, argumantsExpression);
						result = Expression.Call(methodCallExpression2, endsWithMethodInfo, convertedRightSideConstantExpression);
					}
					else
					{
						result = Expression.Call(leftSile, endsWithMethodInfo, rightSide);
					}
					break;

				case OperatorEnum.NotEndsWith:
					if (isNumericType)
					{
						MethodCallExpression methodCallExpression1 = Expression.Call(stringConvertMethodInfo, unaryExpression);
						MethodCallExpression methodCallExpression2 = Expression.Call(methodCallExpression1, trimEndMethodInfo, argumantsExpression);
						result = Expression.Call(methodCallExpression2, endsWithMethodInfo, convertedRightSideConstantExpression);
						result = Expression.Not(result);
					}
					else
					{
						result = Expression.Not(Expression.Call(leftSile, endsWithMethodInfo, rightSide));
					}
					break;

				case OperatorEnum.GreaterThan:
					if (isString)
					{
						result = Expression.GreaterThan(Expression.Call(leftSile, stringCompareMethodInfo, rightSide), Expression.Constant(0));
					}
					else
					{
						result = Expression.GreaterThan(leftSile, rightSide);
					}
					break;

				case OperatorEnum.GreaterThanOrEqual:
					if (isString)
					{
						result = Expression.GreaterThanOrEqual(Expression.Call(leftSile, stringCompareMethodInfo, rightSide), Expression.Constant(0));
					}
					else
					{
						result = Expression.GreaterThanOrEqual(leftSile, rightSide);
					}
					break;

				case OperatorEnum.LessThan:
					if (isString)
					{
						result = Expression.LessThan(Expression.Call(leftSile, stringCompareMethodInfo, rightSide), Expression.Constant(0));
					}
					else
					{
						result = Expression.LessThan(leftSile, rightSide);
					}
					break;

				case OperatorEnum.LessThanOrEqual:
					if (isString)
					{
						result = Expression.LessThanOrEqual(Expression.Call(leftSile, stringCompareMethodInfo, rightSide), Expression.Constant(0));
					}
					else
					{
						result = Expression.LessThanOrEqual(leftSile, rightSide);
					}
					break;

				default:
					throw new ArgumentException("Argument is not valid beacuse of operation type", "conditionForConvert");
			}

			return result;
		}

		private static Expression GetLeftSide(string selectorString, Type parameterExpressionType, ParameterExpression parameterExpression)
		{
			if (string.IsNullOrWhiteSpace(selectorString)) throw new ArgumentNullException("selectorString", "Selector string is not valid");
			string[] propertyParts = selectorString.Split(new[] { '.' });
			if (propertyParts.Any(string.IsNullOrWhiteSpace))
				throw new Exception(string.Format("Selector string \"{0}\" format is not valid.", selectorString));
			string firstPartOfSelector = GetInvariantCultrueString(propertyParts[0]);

			PropertyInfo propertyInThisType = parameterExpressionType.GetProperty(firstPartOfSelector);
			if (propertyInThisType == null)
				throw new Exception(string.Format("Selector string \"{0}\" is not exist in type \"{1}\".", selectorString, parameterExpressionType.Name));

			Expression expression = Expression.Property(parameterExpression, propertyInThisType);
			if (propertyParts.Length == 1)
				return expression;
			return GetLeftSide(string.Join(".", propertyParts, 1, propertyParts.Length - 1), expression);
		}

		private static Expression GetLeftSide(string selectorString, Expression inputExpression)
		{
			Expression resultExpression;
			Type inputExpressionType = inputExpression.Type;

			if (string.IsNullOrWhiteSpace(selectorString)) throw new ArgumentNullException("selectorString", "Selector string is not valid");
			string[] propertyParts = selectorString.Split(new[] { '.' });
			if (propertyParts.Any(string.IsNullOrWhiteSpace))
				throw new Exception(string.Format("Selector string \"{0}\" format is not valid.", selectorString));
			string firstPartOfSelector = GetInvariantCultrueString(propertyParts[0]);

			PropertyInfo selectedPropertyInfo = null;
			MethodInfo selectedMethodInfo = null;
			if (firstPartOfSelector.IndexOf('(') > 0)
			{
				string methodName = firstPartOfSelector.Remove(firstPartOfSelector.IndexOf('('));
				selectedMethodInfo = inputExpressionType.GetMethod(methodName, new Type[0]);
				resultExpression = Expression.Call(inputExpression, selectedMethodInfo);
			}
			else
			{
				selectedPropertyInfo = inputExpressionType.GetProperty(firstPartOfSelector);
				resultExpression = Expression.Property(inputExpression, selectedPropertyInfo);
			}

			if (selectedPropertyInfo == null && selectedMethodInfo == null)
				throw new Exception(string.Format("Selector string \"{0}\" is not exist in type \"{1}\".", selectorString, inputExpression.Type.Name));

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
			string selectorString = selectorExpression.Body.ToString();
			return selectorString.Remove(0, selectorString.IndexOf('.') + 1);
		}

		private static Type GetTargetPropertyType(Type entityType, string selectorString)
		{
			if (string.IsNullOrWhiteSpace(selectorString)) return null;
			string[] propertyParts = selectorString.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(q => q.Trim())
				.Where(q => !string.IsNullOrEmpty(q))
				.ToArray();
			string firstPartOfSelector = propertyParts[0].ToString(CultureInfo.InvariantCulture);
			PropertyInfo selectedPropertyInfo = null;
			MethodInfo selectedMethodInfo = null;
			if (firstPartOfSelector.IndexOf('(') > 0)
			{
				string methodName = firstPartOfSelector.Remove(firstPartOfSelector.IndexOf('('));
				selectedMethodInfo = entityType.GetMethod(methodName, new Type[0]);
			}
			else
				selectedPropertyInfo = entityType.GetProperty(firstPartOfSelector);
			if (selectedPropertyInfo == null && selectedMethodInfo == null)
				throw new Exception(string.Format("Selector string \"{0}\" is not exist in type \"{1}\".", selectorString, entityType.Name));
			if (propertyParts.Length != 1)
				return GetTargetPropertyType(selectedPropertyInfo.PropertyType, string.Join(".", propertyParts, 1, propertyParts.Length - 1));
			if (selectedPropertyInfo != null)
				return selectedPropertyInfo.PropertyType;
			return selectedMethodInfo.ReturnType;
		}

		private static object ChangeValueType(Type targetPropertyType, object value)
		{
			string propertyValueInString = value.ToString().ToEnglishNumber();

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
				propertyValueInString = Regex.Replace(propertyValueInString, @"[^\d\.]+|\.+$|^\.+", "");
				try
				{
					TypeConverter typeConverter = TypeDescriptor.GetConverter(targetPropertyType);
					propertyValue = typeConverter.ConvertFrom(propertyValueInString);
				}
				catch
				{
					FieldInfo fieldInfo = targetPropertyType.GetField("MinValue");
					propertyValue = fieldInfo.GetRawConstantValue();
				}
			}
			else if (targetPropertyType.IsBoolean())
			{
				try
				{
					TypeConverter typeConverter = TypeDescriptor.GetConverter(targetPropertyType);
					propertyValue = typeConverter.ConvertFrom(propertyValueInString.Trim());
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
