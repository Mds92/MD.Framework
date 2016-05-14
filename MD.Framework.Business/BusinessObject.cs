using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using MD.Framework.Utility;

namespace MD.Framework.Business
{
	public abstract class BusinessObject<TEntity, TIdentifier> : IDisposable where TEntity : class
	{
		#region Properties and Fields

		public DbContext Context { get; set; }
		private DbSet<TEntity> DbSet { get; set; }

		public virtual string IdentifierColumnName { get { return "Id"; } }

		Type _entityType;
		Type EntityType
		{
			get
			{
				if (_entityType != null) return _entityType;
				_entityType = typeof(TEntity);
				return _entityType;
			}
		}

		Type _identifierType;
		Type IdentifierType
		{
			get
			{
				if (_identifierType != null) return _identifierType;
				_identifierType = typeof(TIdentifier);
				return _identifierType;
			}
		}

		PropertyInfo _identifierPropertyInfo;
		PropertyInfo IdentifierPropertyInfo
		{
			get
			{
				if (_identifierPropertyInfo != null) return _identifierPropertyInfo;
				_identifierPropertyInfo = EntityType.GetProperty(IdentifierColumnName);
				if (_identifierPropertyInfo == null)
					throw new Exception(string.Format("ستونی با نام '{0}' در موجودیت '{1}' وجود ندارد", IdentifierColumnName, EntityType.Name));
				return _identifierPropertyInfo;
			}
		}

		#endregion

		protected BusinessObject(DbContext context)
		{
			Context = context;
			DbSet = Context.Set<TEntity>();
		}

		#region Methods

		#region Delete

		protected virtual void BeforeRemoveOrDelete(TEntity entity) { }

		[DataObjectMethod(DataObjectMethodType.Delete)]
		public void Delete(Expression<Func<TEntity, bool>> predicate)
		{
			List<TEntity> entities = DbSet.Where(predicate).ToList();
			foreach (TEntity entity in entities)
			{
				BeforeRemoveOrDelete(entity);
				DbSet.Remove(entity);
				this.Context.Entry(entity).State = EntityState.Deleted;
			}
			SaveChanges();
		}

		[DataObjectMethod(DataObjectMethodType.Delete)]
		public void Delete(ServicePredicateBuilder<TEntity> servicePredicateBuilder)
		{
			Delete(servicePredicateBuilder.Criteria.GetExpression());
		}

		[DataObjectMethod(DataObjectMethodType.Delete)]
		public void Delete(TIdentifier identifier)
		{
			Delete(new List<TIdentifier> { identifier });
		}

		[DataObjectMethod(DataObjectMethodType.Delete)]
		public void Delete(IEnumerable<TIdentifier> identifiers)
		{
			List<TIdentifier> idsList = identifiers.ToList();
			List<TEntity> entities = SelectAll(idsList).ToList();
			foreach (TEntity entity in entities)
			{
				BeforeRemoveOrDelete(entity);
				this.Context.Entry(entity).State = EntityState.Deleted;
				DbSet.Remove(entity);
			}
			SaveChanges();
		}

		[DataObjectMethod(DataObjectMethodType.Delete)]
		public void Delete(TEntity entity)
		{
			Delete(new List<TEntity> { entity });
		}

		[DataObjectMethod(DataObjectMethodType.Delete)]
		public void Delete(IEnumerable<TEntity> entities)
		{
			List<TEntity> entitiesList = entities.ToList();
			foreach (TEntity entity in entitiesList)
			{
				BeforeRemoveOrDelete(entity);
				DbSet.Remove(entity);
				this.Context.Entry(entity).State = EntityState.Deleted;
			}
			SaveChanges();
		}

		#endregion

		#region Remove

		[DataObjectMethod(DataObjectMethodType.Delete)]
		public void Remove(Expression<Func<TEntity, bool>> predicate)
		{
			IQueryable<TEntity> entities = DbSet.Where(predicate);
			if (!entities.Any()) return;
			foreach (TEntity entity in entities)
			{
				BeforeRemoveOrDelete(entity);
				DbSet.Remove(entity);
				this.Context.Entry(entity).State = EntityState.Deleted;
			}
		}

		[DataObjectMethod(DataObjectMethodType.Delete)]
		public void Remove(ServicePredicateBuilder<TEntity> servicePredicateBuilder)
		{
			Remove(servicePredicateBuilder.Criteria.GetExpression());
		}

		[DataObjectMethod(DataObjectMethodType.Delete)]
		public void Remove(TIdentifier identifier)
		{
			TEntity entity = SelectBy(identifier);
			if (entity == null) return;
			BeforeRemoveOrDelete(entity);
			DbSet.Remove(entity);
			this.Context.Entry(entity).State = EntityState.Deleted;
		}

		[DataObjectMethod(DataObjectMethodType.Delete)]
		public void Remove(IEnumerable<TIdentifier> identifiers)
		{
			List<TEntity> entities = SelectAll(identifiers.ToList()).ToList();
			foreach (TEntity entity in entities)
			{
				BeforeRemoveOrDelete(entity);
				DbSet.Remove(entity);
				this.Context.Entry(entity).State = EntityState.Deleted;
			}
		}

		[DataObjectMethod(DataObjectMethodType.Delete)]
		public void Remove(TEntity entity)
		{
			BeforeRemoveOrDelete(entity);
			DbSet.Remove(entity);
			this.Context.Entry(entity).State = EntityState.Deleted;
		}

		[DataObjectMethod(DataObjectMethodType.Delete)]
		public void Remove(IEnumerable<TEntity> entities)
		{
			List<TEntity> entityList = entities.ToList();
			foreach (TEntity entity in entityList)
			{
				BeforeRemoveOrDelete(entity);
				DbSet.Remove(entity);
				this.Context.Entry(entity).State = EntityState.Deleted;
			}
		}

		#endregion

		#region Insert

		protected virtual void BeforeAddOrInsert(TEntity entity) { }
		protected virtual void BeforeUpdateOrSaveChangesOrAddOrInsert(TEntity entity) { }

		[DataObjectMethod(DataObjectMethodType.Insert, true)]
		public void Add(TEntity entity)
		{
			Add(new List<TEntity> { entity });
		}

		[DataObjectMethod(DataObjectMethodType.Insert, true)]
		public void Add(IEnumerable<TEntity> entities)
		{
			foreach (TEntity entity in entities)
			{
				BeforeAddOrInsert(entity);
				BeforeUpdateOrSaveChangesOrAddOrInsert(entity);
				DbSet.Add(entity);
				this.Context.Entry(entity).State = EntityState.Added;
			}
		}

		[DataObjectMethod(DataObjectMethodType.Insert, true)]
		public TEntity Insert(TEntity entity)
		{
			return Insert(new List<TEntity> {entity}).FirstOrDefault();
		}

		[DataObjectMethod(DataObjectMethodType.Insert, true)]
		public List<TEntity> Insert(List<TEntity> entities)
		{
			foreach (TEntity entity in entities)
			{
				BeforeAddOrInsert(entity);
				DbSet.Add(entity);
				this.Context.Entry(entity).State = EntityState.Added;
			}
			SaveChanges(entities);
			return entities;
		}

		#endregion

		#region Tools

		[DataObjectMethod(DataObjectMethodType.Select)]
		public long SelectNextId()
		{
			string tableName = this.GetCurrentEntityTableName();
			const string command = @"SELECT IDENT_CURRENT ({0}) AS Current_Identity;";
			decimal id = (Context as IObjectContextAdapter).ObjectContext.ExecuteStoreQuery<decimal>(command, tableName).FirstOrDefault();
			return Convert.ToInt64(id) + 1;
		}

		public bool IsUnique<TProperty>(Expression<Func<TEntity, TProperty>> propertyLambda, TProperty value, TIdentifier identifier)
		{
			Type typeOfProperty = typeof(TProperty);

			if (Equals(value, default(TProperty)))
				return true;

			MemberExpression member = propertyLambda.Body as MemberExpression;
			if (member == null)
				throw new ArgumentException(string.Format("Expression '{0}' refers to a method, not a property.", propertyLambda));

			PropertyInfo propInfo = member.Member as PropertyInfo;
			if (propInfo == null)
				throw new ArgumentException(string.Format("Expression '{0}' refers to a field, not a property.", propertyLambda));

			if (propInfo.ReflectedType == null)
				throw new ArgumentException(string.Format("Property '{0}' dosen't have a ReflectedType.", propInfo.Name));

			if (EntityType != propInfo.ReflectedType && !EntityType.IsSubclassOf(propInfo.ReflectedType))
				throw new ArgumentException(string.Format("Expresion '{0}' refers to a property that is not from type {1}.", propertyLambda, EntityType));

			ParameterExpression parameterExpression = Expression.Parameter(EntityType, "q");

			// ------------------------------------------------------------------
			MemberExpression memberExpression1 = Expression.Property(parameterExpression, propInfo);
			ConstantExpression constantExpression1 = Expression.Constant(value, typeOfProperty);
			BinaryExpression binaryExpression1 = Expression.Equal(memberExpression1, constantExpression1);

			// ------------------------------------------------------------------
			MemberExpression memberExpression2 = Expression.MakeMemberAccess(parameterExpression, IdentifierPropertyInfo);
			ConstantExpression constantExpression2 = Expression.Constant(identifier, IdentifierType);
			BinaryExpression binaryExpression2 = Expression.NotEqual(memberExpression2, constantExpression2);

			Expression<Func<TEntity, bool>> lambdaExpression = Expression.Lambda<Func<TEntity, bool>>(Expression.AndAlso(binaryExpression1, binaryExpression2), parameterExpression);

			return SelectCount(lambdaExpression) == 0;
		}

		public string GetDatabaseName()
		{
			string connectionString = Context.Database.Connection.ConnectionString;
			MatchCollection matchs = Regex.Matches(connectionString, @"(?<=initial\s+catalog\s*=)[^;]+(?=;)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
			if (matchs.Count <= 0) throw new Exception("'initial catalog' didn't find in your connection string");
			return matchs[0].Value;
		}

		public virtual string GetConnectionString()
		{
			return Context.Database.Connection.ConnectionString;
		}

		public string GetCurrentEntityTableName()
		{
			return Context.GetTableName<TEntity>();
		}

		public void RunTsqWithVoidReturnValue(string tsql)
		{
			using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
			using (SqlCommand cmd = new SqlCommand("RunTsql", sqlConnection))
			{
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.Add("@Tsql", SqlDbType.NVarChar).Value = tsql;
				sqlConnection.Open();
				cmd.ExecuteReader(CommandBehavior.CloseConnection);
				sqlConnection.Close();
			}
		}
		public List<TIdentifier> RunTsqWithIdsReturnValue(string tsql)
		{
			List<TIdentifier> foundIds;
			using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
			using (SqlCommand cmd = new SqlCommand("RunTsql", sqlConnection))
			{
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.Add("@Tsql", SqlDbType.NVarChar).Value = tsql;

				sqlConnection.Open();
				using (SqlDataReader sqlDataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
				{
					foundIds = ConvertSqlDataReaderToList(sqlDataReader);
				}
				sqlConnection.Close();
			}
			return foundIds;
		}
		public List<TEntity> RunTsqWithEntitiesReturnValue(string tsql)
		{
			List<TEntity> foundEntities;
			using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
			using (SqlCommand cmd = new SqlCommand("RunTsql", sqlConnection))
			{
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.Add("@Tsql", SqlDbType.NVarChar).Value = tsql;

				sqlConnection.Open();
				using (SqlDataReader sqlDataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
				{
					foundEntities = ConvertSqlDataReaderToEntities(sqlDataReader);
				}
				sqlConnection.Close();
			}
			return foundEntities;
		}

		#endregion

		#region Select

		#region SelectAll

		[DataObjectMethod(DataObjectMethodType.Select)]
		public IQueryable<TEntity> SelectAll(List<string> includeNavigationProperties = null)
		{
			IQueryable<TEntity> result;
			if (includeNavigationProperties != null && includeNavigationProperties.Any())
				result = includeNavigationProperties.Aggregate<string, DbQuery<TEntity>>(DbSet,
					(current, property) => current.Include(property));
			else
				result = DbSet;
			return result;
		}

		[DataObjectMethod(DataObjectMethodType.Select)]
		public IQueryable<TEntity> SelectAll(List<TIdentifier> ids, List<string> includeNavigationProperties = null)
		{
			Type iCollectionType = typeof(ICollection<TIdentifier>);
			ParameterExpression parameterExpression = Expression.Parameter(EntityType, "q");
			ConstantExpression constantExpression = Expression.Constant(ids, iCollectionType);
			MemberExpression memberExpression = Expression.Property(parameterExpression, IdentifierColumnName);
			MethodInfo containsMethodInfo = iCollectionType.GetMethod("Contains", new[] { IdentifierType });

			MethodCallExpression methodCallExpression = Expression.Call(constantExpression, containsMethodInfo, memberExpression);
			Expression<Func<TEntity, bool>> lambdaExpression = Expression.Lambda<Func<TEntity, bool>>(methodCallExpression, parameterExpression);

			if (includeNavigationProperties == null || !includeNavigationProperties.Any())
				return DbSet.Where(lambdaExpression);
			DbQuery<TEntity> query = includeNavigationProperties.Aggregate<string, DbQuery<TEntity>>(DbSet, (current, property) => current.Include(property));

			return query.Where(lambdaExpression);
		}

		[DataObjectMethod(DataObjectMethodType.Select)]
		public IQueryable<TEntity> SelectAll(Expression<Func<TEntity, bool>> predicate, List<string> includeNavigationProperties = null)
		{
			if (includeNavigationProperties == null || !includeNavigationProperties.Any())
				return DbSet.Where(predicate);
			DbQuery<TEntity> query = includeNavigationProperties.Aggregate<string, DbQuery<TEntity>>(DbSet, (current, property) => current.Include(property));
			return query.Where(predicate);
		}

		[DataObjectMethod(DataObjectMethodType.Select)]
		public IQueryable<TEntity> SelectAll(Expression<Func<TEntity, bool>> predicate, Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression, List<string> includeNavigationProperties = null)
		{
			IQueryable<TEntity> result;
			Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByFunc = null;
			if (orderByExpression != null)
				orderByFunc = orderByExpression.Compile();

			if (includeNavigationProperties == null || !includeNavigationProperties.Any())
			{
				if (orderByFunc != null)
					result = orderByFunc(DbSet.Where(predicate));
				else
					result = DbSet.Where(predicate);
			}
			else
			{
				DbQuery<TEntity> query = includeNavigationProperties.Aggregate<string, DbQuery<TEntity>>(DbSet, (current, property) => current.Include(property));
				if (orderByExpression != null)
					result = orderByFunc(query.Where(predicate));
				else
					result = query.Where(predicate);
			}
			return result;
		}

		[DataObjectMethod(DataObjectMethodType.Select)]
		public IQueryable<TEntity> SelectAll(ServicePredicateBuilder<TEntity> servicePredicateBuilder)
		{
			Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression = null;
			if (servicePredicateBuilder.SortCondition != null)
				orderByExpression = servicePredicateBuilder.SortCondition.GetIQueryableSortingExpression();
			return SelectAll(servicePredicateBuilder.Criteria.GetExpression(), orderByExpression, servicePredicateBuilder.IncludedNavigationProperties);
		}

		#endregion

		#region SelectBy

		[DataObjectMethod(DataObjectMethodType.Select)]
		public TEntity SelectBy(Expression<Func<TEntity, bool>> predicate, List<string> includeNavigationProperties = null)
		{
			TEntity result;

			if (includeNavigationProperties == null || !includeNavigationProperties.Any())
				result = DbSet.FirstOrDefault(predicate);
			else
			{
				DbQuery<TEntity> query = includeNavigationProperties.Aggregate<string, DbQuery<TEntity>>(DbSet, (current, property) => current.Include(property));
				result = query.FirstOrDefault(predicate);
			}

			return result;
		}

		[DataObjectMethod(DataObjectMethodType.Select)]
		public TEntity SelectBy(ServicePredicateBuilder<TEntity> servicePredicateBuilder)
		{
			return SelectBy(servicePredicateBuilder.Criteria.GetExpression(), servicePredicateBuilder.IncludedNavigationProperties);
		}

		[DataObjectMethod(DataObjectMethodType.Select)]
		public TEntity SelectBy(TIdentifier identifier, List<string> includeNavigationProperties = null)
		{
			ParameterExpression parameter = Expression.Parameter(EntityType, "q");
			MemberExpression propertyAccess = Expression.MakeMemberAccess(parameter, IdentifierPropertyInfo);
			ConstantExpression rightExpr = Expression.Constant(identifier, IdentifierType);
			return SelectBy(Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(propertyAccess, rightExpr), parameter), includeNavigationProperties);
		}

		#endregion

		#region SelectCount

		[DataObjectMethod(DataObjectMethodType.Select)]
		public int SelectCount()
		{
			return DbSet.Count();
		}

		[DataObjectMethod(DataObjectMethodType.Select)]
		public int SelectCount(Expression<Func<TEntity, bool>> predicate)
		{
			return DbSet.Count(predicate);
		}

		[DataObjectMethod(DataObjectMethodType.Select)]
		public int SelectCount(ServicePredicateBuilder<TEntity> servicePredicateBuilder)
		{
			return SelectCount(servicePredicateBuilder.Criteria.GetExpression());
		}

		#endregion

		#region SelectPage

		[DataObjectMethod(DataObjectMethodType.Select)]
		public IQueryable<TEntity> SelectPage(
			Expression<Func<TEntity, bool>> predicate,
			Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression,
			int currentPage, int itemsPerPage,
			List<string> includeNavigationProperties = null)
		{
			Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByFunc = orderByExpression.Compile();

			if (includeNavigationProperties == null || !includeNavigationProperties.Any())
				return orderByFunc(DbSet.Where(predicate)).Skip(((currentPage - 1) * itemsPerPage)).Take(itemsPerPage);
			DbQuery<TEntity> query = includeNavigationProperties.Aggregate<string, DbQuery<TEntity>>(DbSet, (current, property) => current.Include(property));
			return orderByFunc(query.Where(predicate)).Skip(((currentPage - 1) * itemsPerPage)).Take(itemsPerPage);
		}

		[DataObjectMethod(DataObjectMethodType.Select)]
		public IQueryable<TEntity> SelectPage(ServicePredicateBuilder<TEntity> servicePredicateBuilder)
		{
			if (servicePredicateBuilder.SortCondition == null)
				throw new Exception(string.Format("برای گرفتن صفحه ای از '{0}' باید مرتب سازی را تعیین نمایید", EntityType.Name));
			return SelectPage(servicePredicateBuilder.Criteria.GetExpression(), servicePredicateBuilder.SortCondition.GetIQueryableSortingExpression(),
				servicePredicateBuilder.PaginationData.PageNumber, servicePredicateBuilder.PaginationData.ItemsPerPage, servicePredicateBuilder.IncludedNavigationProperties);
		}

		[DataObjectMethod(DataObjectMethodType.Select)]
		public IQueryable<TEntity> SelectPage(
		   Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression,
		   int currentPage, int itemsPerPage,
		   List<string> includeNavigationProperties = null)
		{
			Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByFunc = orderByExpression.Compile();

			if (includeNavigationProperties == null || !includeNavigationProperties.Any())
				return orderByFunc(DbSet).Skip(((currentPage - 1) * itemsPerPage)).Take(itemsPerPage);
			DbQuery<TEntity> query = includeNavigationProperties.Aggregate<string, DbQuery<TEntity>>(DbSet, (current, property) => current.Include(property));
			return orderByFunc(query).Skip(((currentPage - 1) * itemsPerPage)).Take(itemsPerPage);
		}


		#endregion

		#region CountOfPage

		[DataObjectMethod(DataObjectMethodType.Select)]
		public int CountOfPage(Expression<Func<TEntity, bool>> predicate, int itemPerPage)
		{
			int allItemsCount = SelectCount(predicate);
			return (allItemsCount % itemPerPage) == 0
					   ? (allItemsCount / itemPerPage)
					   : (allItemsCount / itemPerPage) + 1;
		}

		[DataObjectMethod(DataObjectMethodType.Select)]
		public int CountOfPage(ServicePredicateBuilder<TEntity> servicePredicateBuilder)
		{
			return CountOfPage(servicePredicateBuilder.Criteria.GetExpression(), servicePredicateBuilder.PaginationData.ItemsPerPage);
		}

		[DataObjectMethod(DataObjectMethodType.Select)]
		public int CountOfPage(int itemPerPage)
		{
			int allItemsCount = SelectCount();
			return (allItemsCount % itemPerPage) == 0
					   ? (allItemsCount / itemPerPage)
					   : (allItemsCount / itemPerPage) + 1;
		}

		#endregion

		#endregion

		#region Update

		protected virtual void BeforeUpdate(TEntity entity) { }

		public void SaveChanges(List<TEntity> entities = null)
		{
			try
			{
				if (entities != null)
					foreach (TEntity entity in entities)
						BeforeUpdateOrSaveChangesOrAddOrInsert(entity);
				Context.SaveChanges();
			}
			catch (Exception ex)
			{
				GetExceptions(ex);
			}
		}

		[DataObjectMethod(DataObjectMethodType.Update)]
		public TEntity Update(TEntity entity)
		{
			return Update(new List<TEntity> { entity }).First();
		}

		[DataObjectMethod(DataObjectMethodType.Update)]
		public List<TEntity> Update(List<TEntity> entities)
		{
			foreach (var entity in entities)
				BeforeUpdate(entity);
			SaveChanges(entities);
			return entities;
		}

		#endregion

		#region FullTextSearch

		public List<TEntity> FullTextSearch(string searchText, string conditionTSql, int page, int itemsPerPage,
			 List<Expression<Func<TEntity, string>>> columnsForSearching, List<KeyValuePair<Expression<Func<TEntity, object>>, SortDirection>> sortingsValuePairs)
		{
			searchText = searchText.Trim();
			if (string.IsNullOrEmpty(searchText))
				throw new Exception("searchText نباید خالی باشد");

			string columnsToSearch = columnsForSearching.Select(expression => expression.Body.ToString().Replace("Convert", "").Replace(")", "").Replace("(", "")).Aggregate(string.Empty, (current, s) => current + string.Format("{0}, ", s.Remove(0, s.IndexOf('.') + 1)));
			columnsToSearch = columnsToSearch.Remove(columnsToSearch.LastIndexOf(','));
			if (string.IsNullOrWhiteSpace(columnsToSearch)) return null;

			string orderingString = string.Empty;
			foreach (var item in sortingsValuePairs)
			{
				var columnName = item.Key.Body.ToString().Replace("Convert", "").Replace(")", "").Replace("(", "");
				columnName = columnName.Remove(0, columnName.IndexOf('.') + 1);
				orderingString += string.Format("{0}.{1} {2}, ", "{AlternativeTableNamePlaceHolder}", columnName, item.Value == SortDirection.Ascending ? "ASC" : "DESC");
			}
			orderingString = orderingString.Remove(orderingString.LastIndexOf(','));

			using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
			using (SqlCommand cmd = new SqlCommand("FullTextSearch", sqlConnection))
			{
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.Add("@WhereExpression", SqlDbType.NVarChar).Value = conditionTSql;
				cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = page;
				cmd.Parameters.Add("@ItemsPerPage", SqlDbType.Int).Value = itemsPerPage;
				cmd.Parameters.Add("@TableName", SqlDbType.NVarChar).Value = this.GetCurrentEntityTableName();
				cmd.Parameters.Add("@ColumnNamesToSearch", SqlDbType.NVarChar).Value = columnsToSearch;
				cmd.Parameters.Add("@SearchText", SqlDbType.NVarChar).Value = searchText;
				cmd.Parameters.Add("@OrderingString", SqlDbType.NVarChar).Value = orderingString;

				sqlConnection.Open();
				List<TEntity> entities;
				using (SqlDataReader sqlDataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
				{
					entities = ConvertSqlDataReaderToEntities(sqlDataReader);
				}
				sqlConnection.Close();
				return entities;
			}
		}
		public int FullTextSearchCount(string searchText, string conditionTSql, List<Expression<Func<TEntity, string>>> columnsForSearching)
		{
			searchText = searchText.Trim();
			if (string.IsNullOrEmpty(searchText))
				throw new Exception("searchText نباید خالی باشد");
			string columnsTosearch = columnsForSearching.Select(expression => expression.Body.ToString().Replace("Convert", "").Replace(")", "").Replace("(", "")).Aggregate(string.Empty, (current, s) => current + string.Format("{0}, ", s.Remove(0, s.IndexOf('.') + 1)));
			columnsTosearch = columnsTosearch.Remove(columnsTosearch.LastIndexOf(','));

			using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
			using (SqlCommand cmd = new SqlCommand("FullTextSearchCount", sqlConnection))
			{
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.Add("@WhereExpression", SqlDbType.NVarChar).Value = conditionTSql;
				cmd.Parameters.Add("@TableName", SqlDbType.NVarChar).Value = this.GetCurrentEntityTableName();
				cmd.Parameters.Add("@ColumnNamesToSearch", SqlDbType.NVarChar).Value = columnsTosearch;
				cmd.Parameters.Add("@SearchText", SqlDbType.NVarChar).Value = searchText;
				sqlConnection.Open();
				return (int)cmd.ExecuteScalar();
			}
		}

		public List<TEntity> ConvertSqlDataReaderToEntities(SqlDataReader sqlDataReader)
		{
			List<TEntity> entities = new List<TEntity>();
			PropertyInfo[] props = EntityType.GetProperties();

			while (sqlDataReader.Read())
			{
				TEntity entity = Activator.CreateInstance<TEntity>();
				foreach (PropertyInfo col in props)
				{
					try
					{
						if (sqlDataReader[col.Name].Equals(DBNull.Value))
							col.SetValue(entity, null, null);
						else
							col.SetValue(entity, sqlDataReader[col.Name], null);
					}
					catch
					{
						// ignored
					}
				}
				entities.Add(entity);
			}
			return entities;
		}
		public List<TIdentifier> ConvertSqlDataReaderToList(IDataReader dataReader)
		{
			List<TIdentifier> list = new List<TIdentifier>();
			while (dataReader.Read())
				list.Add((TIdentifier)dataReader.GetValue(0));
			return list;
		}

		#endregion

		#endregion

		#region Implementation of IDisposable

		/// <summary>
		///   Releases all resources used by the WarrantManagement.DataExtract.Dal.ReportDataBase
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		///   Releases all resources used by the WarrantManagement.DataExtract.Dal.ReportDataBase
		/// </summary>
		/// <param name="disposing"> A boolean value indicating whether or not to dispose managed resources </param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing) return;
			if (Context == null) return;
			Context.Dispose();
			Context = null;
		}

		#endregion

		void GetExceptions(Exception ex)
		{
			Exception exception = ex;
			StackTrace stackTrace = new StackTrace(1);
			string exceptionMessage = string.Format("Exception at '{0}', '{1}': {2}{3}",
				EntityType.Name, stackTrace.GetFrame(0).GetMethod().Name, Environment.NewLine, ex.Message);
			while (exception.InnerException != null)
			{
				exceptionMessage += string.Format("{0}InnerMessage: {1}", Environment.NewLine, exception.InnerException.Message);
				exception = exception.InnerException;
			}

			DbEntityValidationException dbEntityValidationException = ex as DbEntityValidationException;
			if (dbEntityValidationException == null || dbEntityValidationException.EntityValidationErrors == null || !dbEntityValidationException.EntityValidationErrors.Any())
				throw new Exception(exceptionMessage, ex);

			exceptionMessage += Environment.NewLine;

			List<DbEntityValidationResult> dbEntityValidationResults = dbEntityValidationException.EntityValidationErrors.ToList();
			foreach (DbEntityValidationResult dbEntityValidationResult in dbEntityValidationResults)
			{
				var entityName = dbEntityValidationResult.Entry.Entity.GetType().Name;
				foreach (DbValidationError validationError in dbEntityValidationResult.ValidationErrors)
				{
					exceptionMessage += string.Format("{0}EntityValidationError: PropertyName='{1}' in '{2}', {3}",
						Environment.NewLine, validationError.PropertyName, entityName, validationError.ErrorMessage);
				}
			}

			throw new Exception(exceptionMessage, ex);
		}
	}
}