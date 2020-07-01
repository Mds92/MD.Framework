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
using System.Threading.Tasks;
using MD.Framework.Utility;

namespace MD.Framework.Business
{
    public abstract class BusinessObject<TEntity, TIdentifier>
            where TEntity : class
            where TIdentifier : struct
    {
        #region Properties and Fields

        public DbContext Context { get; set; }
        private DbSet<TEntity> DbSet { get; set; }

        public virtual string IdentifierColumnName => "Id";

        private Type _entityType;
        private Type EntityType
        {
            get
            {
                if (_entityType != null) return _entityType;
                _entityType = typeof(TEntity);
                return _entityType;
            }
        }

        private Type _identifierType;
        private Type IdentifierType
        {
            get
            {
                if (_identifierType != null) return _identifierType;
                _identifierType = typeof(TIdentifier);
                return _identifierType;
            }
        }

        private PropertyInfo _identifierPropertyInfo;
        private PropertyInfo IdentifierPropertyInfo
        {
            get
            {
                if (_identifierPropertyInfo != null) return _identifierPropertyInfo;
                _identifierPropertyInfo = EntityType.GetProperty(IdentifierColumnName);
                if (_identifierPropertyInfo == null)
                    throw new Exception(
                        $"ستونی با نام '{IdentifierColumnName}' در موجودیت '{EntityType.Name}' وجود ندارد");
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
            var entities = DbSet.Where(predicate).ToList();
            foreach (var entity in entities)
            {
                BeforeRemoveOrDelete(entity);
                DbSet.Remove(entity);
                Context.Entry(entity).State = EntityState.Deleted;
            }
            SaveChanges();
        }

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public void Delete(ExpressionInfo<TEntity> expressionInfo)
        {
            Delete(expressionInfo.Expression);
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
            var idsList = identifiers.ToList();
            var entities = SelectAll(idsList).ToList();
            foreach (var entity in entities)
            {
                BeforeRemoveOrDelete(entity);
                Context.Entry(entity).State = EntityState.Deleted;
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
            var entitiesList = entities.ToList();
            foreach (var entity in entitiesList)
            {
                BeforeRemoveOrDelete(entity);
                DbSet.Remove(entity);
                Context.Entry(entity).State = EntityState.Deleted;
            }
            SaveChanges();
        }

        // ----------- Async -----------------

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public Task DeleteAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return Task.Run(() => Delete(predicate));
        }

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public Task DeleteAsync(ExpressionInfo<TEntity> expressionInfo)
        {
            return Task.Run(() => Delete(expressionInfo));
        }

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public Task DeleteAsync(ServicePredicateBuilder<TEntity> servicePredicateBuilder)
        {
            return Task.Run(() => Delete(servicePredicateBuilder));
        }

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public Task DeleteAsync(TIdentifier identifier)
        {
            return Task.Run(() => Delete(identifier));
        }

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public Task DeleteAsync(IEnumerable<TIdentifier> identifiers)
        {
            return Task.Run(() => Delete(identifiers));
        }

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public Task DeleteAsync(TEntity entity)
        {
            return Task.Run(() => Delete(entity));
        }

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public Task DeleteAsync(IEnumerable<TEntity> entities)
        {
            return Task.Run(() => Delete(entities));
        }

        #endregion

        #region Remove

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public void Remove(Expression<Func<TEntity, bool>> predicate)
        {
            var entities = DbSet.Where(predicate);
            if (!entities.Any()) return;
            foreach (var entity in entities)
            {
                BeforeRemoveOrDelete(entity);
                DbSet.Remove(entity);
                Context.Entry(entity).State = EntityState.Deleted;
            }
        }

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public void Remove(ExpressionInfo<TEntity> expressionInfo)
        {
            Remove(expressionInfo.Expression);
        }

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public void Remove(ServicePredicateBuilder<TEntity> servicePredicateBuilder)
        {
            Remove(servicePredicateBuilder.Criteria.GetExpression());
        }

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public void Remove(TIdentifier identifier)
        {
            var entity = SelectBy(identifier);
            if (entity == null) return;
            BeforeRemoveOrDelete(entity);
            DbSet.Remove(entity);
            Context.Entry(entity).State = EntityState.Deleted;
        }

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public void Remove(IEnumerable<TIdentifier> identifiers)
        {
            var entities = SelectAll(identifiers.ToList()).ToList();
            foreach (var entity in entities)
            {
                BeforeRemoveOrDelete(entity);
                DbSet.Remove(entity);
                Context.Entry(entity).State = EntityState.Deleted;
            }
        }

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public void Remove(TEntity entity)
        {
            BeforeRemoveOrDelete(entity);
            DbSet.Remove(entity);
            Context.Entry(entity).State = EntityState.Deleted;
        }

        [DataObjectMethod(DataObjectMethodType.Delete)]
        public void Remove(IEnumerable<TEntity> entities)
        {
            var entityList = entities.ToList();
            foreach (var entity in entityList)
            {
                BeforeRemoveOrDelete(entity);
                DbSet.Remove(entity);
                Context.Entry(entity).State = EntityState.Deleted;
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
            foreach (var entity in entities)
            {
                BeforeAddOrInsert(entity);
                BeforeUpdateOrSaveChangesOrAddOrInsert(entity);
                DbSet.Add(entity);
                Context.Entry(entity).State = EntityState.Added;
            }
        }

        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public TEntity Insert(TEntity entity)
        {

            return Insert(new List<TEntity> { entity }).FirstOrDefault();
        }

        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public List<TEntity> Insert(List<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                BeforeUpdateOrSaveChangesOrAddOrInsert(entity);
                BeforeAddOrInsert(entity);
                DbSet.Add(entity);
                Context.Entry(entity).State = EntityState.Added;
            }
            SaveChanges(entities);
            return entities;
        }

        // ----------- Async -----------------

        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public Task<TEntity> InsertAsync(TEntity entity)
        {
            return Task.Run(() => Insert(entity));
        }

        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public Task<List<TEntity>> InsertAsync(List<TEntity> entities)
        {
            return Task.Run(() => Insert(entities));
        }

        #endregion

        #region Tools

        [DataObjectMethod(DataObjectMethodType.Select)]
        public long SelectNextId()
        {
            var tableName = GetCurrentEntityTableName();
            const string command = @"SELECT IDENT_CURRENT ({0}) AS Current_Identity;";
            var id = (Context as IObjectContextAdapter).ObjectContext.ExecuteStoreQuery<decimal>(command, tableName).FirstOrDefault();
            return Convert.ToInt64(id) + 1;
        }

        public bool IsUnique<TProperty>(Expression<Func<TEntity, TProperty>> propertyLambda, TProperty value, TIdentifier identifier)
        {
            var typeOfProperty = typeof(TProperty);

            if (Equals(value, default(TProperty)))
                return true;

            var member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a method, not a property.");

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a field, not a property.");

            if (propInfo.ReflectedType == null)
                throw new ArgumentException($"Property '{propInfo.Name}' dosen't have a ReflectedType.");

            if (EntityType != propInfo.ReflectedType && !EntityType.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException(
                    $"Expression '{propertyLambda}' refers to a property that is not from type {EntityType}.");

            var parameterExpression = Expression.Parameter(EntityType, "q");

            // ------------------------------------------------------------------
            var memberExpression1 = Expression.Property(parameterExpression, propInfo);
            var constantExpression1 = Expression.Constant(value, typeOfProperty);
            var binaryExpression1 = Expression.Equal(memberExpression1, constantExpression1);

            // ------------------------------------------------------------------
            var memberExpression2 = Expression.MakeMemberAccess(parameterExpression, IdentifierPropertyInfo);
            var constantExpression2 = Expression.Constant(identifier, IdentifierType);
            var binaryExpression2 = Expression.NotEqual(memberExpression2, constantExpression2);

            var lambdaExpression = Expression.Lambda<Func<TEntity, bool>>(Expression.AndAlso(binaryExpression1, binaryExpression2), parameterExpression);

            return SelectCount(lambdaExpression) == 0;
        }

        public string GetDatabaseName()
        {
            var connectionString = Context.Database.Connection.ConnectionString;
            var matchs = Regex.Matches(connectionString, @"(?<=initial\s+catalog\s*=)[^;]+(?=;)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
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
            using (var sqlConnection = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand("RunTsql", sqlConnection))
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
            using (var sqlConnection = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand("RunTsql", sqlConnection))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@Tsql", SqlDbType.NVarChar).Value = tsql;

                sqlConnection.Open();
                using (var sqlDataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
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
            using (var sqlConnection = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand("RunTsql", sqlConnection))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@Tsql", SqlDbType.NVarChar).Value = tsql;

                sqlConnection.Open();
                using (var sqlDataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    foundEntities = ConvertSqlDataReaderToEntities(sqlDataReader);
                }
                sqlConnection.Close();
            }
            return foundEntities;
        }

        // ----------- Async -----------------

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<long> SelectNextIdAsync()
        {
            return Task.Run(() => SelectNextId());
        }

        public Task<bool> IsUniqueAsync<TProperty>(Expression<Func<TEntity, TProperty>> propertyLambda, TProperty value, TIdentifier identifier)
        {
            return Task.Run(() => IsUnique(propertyLambda, value, identifier));
        }


        #endregion

        #region Select

        #region SelectAll

        [DataObjectMethod(DataObjectMethodType.Select)]
        public IQueryable<TEntity> SelectAll(List<string> includeNavigationProperties = null)
        {
            IQueryable<TEntity> result;
            if (includeNavigationProperties != null && includeNavigationProperties.Any())
                result = includeNavigationProperties.Aggregate<string, DbQuery<TEntity>>(DbSet, (current, property) => current.Include(property));
            else
                result = DbSet;
            return result;
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public IQueryable<TEntity> SelectAll(List<TIdentifier> ids, List<string> includeNavigationProperties = null)
        {
            var iCollectionType = typeof(ICollection<TIdentifier>);
            var parameterExpression = Expression.Parameter(EntityType, "q");
            var constantExpression = Expression.Constant(ids, iCollectionType);
            var memberExpression = Expression.Property(parameterExpression, IdentifierColumnName);
            var containsMethodInfo = iCollectionType.GetMethod("Contains", new[] { IdentifierType });

            var methodCallExpression = Expression.Call(constantExpression, containsMethodInfo, memberExpression);
            var lambdaExpression = Expression.Lambda<Func<TEntity, bool>>(methodCallExpression, parameterExpression);

            if (includeNavigationProperties == null || !includeNavigationProperties.Any())
                return DbSet.Where(lambdaExpression);
            var query = includeNavigationProperties.Aggregate<string, DbQuery<TEntity>>(DbSet, (current, property) => current.Include(property));

            return query.Where(lambdaExpression);
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public IQueryable<TEntity> SelectAll(Expression<Func<TEntity, bool>> predicate, List<string> includeNavigationProperties = null)
        {
            if (includeNavigationProperties == null || !includeNavigationProperties.Any())
                return DbSet.Where(predicate);
            var query = includeNavigationProperties.Aggregate<string, DbQuery<TEntity>>(DbSet, (current, property) => current.Include(property));
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
                var query = includeNavigationProperties.Aggregate<string, DbQuery<TEntity>>(DbSet, (current, property) => current.Include(property));
                if (orderByExpression != null)
                    result = orderByFunc(query.Where(predicate));
                else
                    result = query.Where(predicate);
            }
            return result;
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public IQueryable<TEntity> SelectAll(Expression<Func<TEntity, bool>> predicate, Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression, int currentPage, int itemsPerPage, List<string> includeNavigationProperties = null)
        {
            var orderByFunc = orderByExpression.Compile();
            if (includeNavigationProperties == null || !includeNavigationProperties.Any())
                return orderByFunc(DbSet.Where(predicate)).Skip(((currentPage - 1) * itemsPerPage)).Take(itemsPerPage);
            var query = includeNavigationProperties.Aggregate<string, DbQuery<TEntity>>(DbSet, (current, property) => current.Include(property));
            return orderByFunc(query.Where(predicate)).Skip(((currentPage - 1) * itemsPerPage)).Take(itemsPerPage);
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public IQueryable<TEntity> SelectAll(Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression, int currentPage, int itemsPerPage, List<string> includeNavigationProperties = null)
        {
            var orderByFunc = orderByExpression.Compile();
            if (includeNavigationProperties == null || !includeNavigationProperties.Any())
                return orderByFunc(DbSet).Skip(((currentPage - 1) * itemsPerPage)).Take(itemsPerPage);
            var query = includeNavigationProperties.Aggregate<string, DbQuery<TEntity>>(DbSet, (current, property) => current.Include(property));
            return orderByFunc(query).Skip(((currentPage - 1) * itemsPerPage)).Take(itemsPerPage);
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public IQueryable<TEntity> SelectAll(ServicePredicateBuilder<TEntity> servicePredicateBuilder)
        {
            if (servicePredicateBuilder.PaginationData != null)
            {
                if (servicePredicateBuilder.SortCondition == null)
                    throw new Exception($"برای گرفتن صفحه ای از '{EntityType.Name}' باید مرتب سازی را تعیین نمایید");
                return SelectAll(servicePredicateBuilder.Criteria.GetExpression(), servicePredicateBuilder.SortCondition.GetIQueryableSortingExpression(),
                    servicePredicateBuilder.PaginationData.PageNumber, servicePredicateBuilder.PaginationData.ItemsPerPage, servicePredicateBuilder.IncludedNavigationProperties);
            }
            Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression = null;
            if (servicePredicateBuilder.SortCondition != null)
                orderByExpression = servicePredicateBuilder.SortCondition.GetIQueryableSortingExpression();
            return SelectAll(servicePredicateBuilder.Criteria.GetExpression(), orderByExpression, servicePredicateBuilder.IncludedNavigationProperties);

        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public IQueryable<TEntity> SelectAll(ExpressionInfo<TEntity> expressionInfo)
        {
            if (expressionInfo.PaginationData != null)
            {
                if (expressionInfo.SortCondition == null)
                    throw new Exception($"برای گرفتن صفحه ای از '{EntityType.Name}' باید مرتب سازی را تعیین نمایید");
                return SelectAll(expressionInfo.Expression, expressionInfo.SortCondition.GetIQueryableSortingExpression(), expressionInfo.PaginationData.PageNumber, expressionInfo.PaginationData.ItemsPerPage, expressionInfo.IncludedNavigationProperties);
            }
            Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression = null;
            if (expressionInfo.SortCondition != null)
                orderByExpression = expressionInfo.SortCondition.GetIQueryableSortingExpression();
            return SelectAll(expressionInfo.Expression, orderByExpression, expressionInfo.IncludedNavigationProperties);
        }

        // ----------- Async -----------------

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<IQueryable<TEntity>> SelectAllAsync(List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectAll(includeNavigationProperties));
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<IQueryable<TEntity>> SelectAllAsync(List<TIdentifier> ids, List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectAll(ids, includeNavigationProperties));
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<IQueryable<TEntity>> SelectAllAsync(Expression<Func<TEntity, bool>> predicate, List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectAll(predicate, includeNavigationProperties));
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<IQueryable<TEntity>> SelectAllAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression, List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectAll(predicate, orderByExpression, includeNavigationProperties));
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<IQueryable<TEntity>> SelectAllAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression, int currentPage, int itemsPerPage, List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectAll(predicate, orderByExpression, currentPage, itemsPerPage, includeNavigationProperties));
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<IQueryable<TEntity>> SelectAllAsync(Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression, int currentPage, int itemsPerPage, List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectAll(orderByExpression, currentPage, itemsPerPage, includeNavigationProperties));
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<IQueryable<TEntity>> SelectAllAsync(ServicePredicateBuilder<TEntity> servicePredicateBuilder)
        {
            return Task.Run(() => SelectAll(servicePredicateBuilder));
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<IQueryable<TEntity>> SelectAllAsync(ExpressionInfo<TEntity> expressionInfo)
        {
            return Task.Run(() => SelectAll(expressionInfo));
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
                var query = includeNavigationProperties.Aggregate<string, DbQuery<TEntity>>(DbSet, (current, property) => current.Include(property));
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
        public TEntity SelectBy(ExpressionInfo<TEntity> expressionInfo)
        {
            return SelectBy(expressionInfo.Expression, expressionInfo.IncludedNavigationProperties);
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public TEntity SelectBy(TIdentifier identifier, List<string> includeNavigationProperties = null)
        {
            var parameter = Expression.Parameter(EntityType, "q");
            var propertyAccess = Expression.MakeMemberAccess(parameter, IdentifierPropertyInfo);
            var rightExpr = Expression.Constant(identifier, IdentifierType);
            return SelectBy(Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(propertyAccess, rightExpr), parameter), includeNavigationProperties);
        }

        // ----------- Async -----------------

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<TEntity> SelectByAsync(Expression<Func<TEntity, bool>> predicate, List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectBy(predicate, includeNavigationProperties));
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<TEntity> SelectByAsync(ServicePredicateBuilder<TEntity> servicePredicateBuilder)
        {
            return Task.Run(() => SelectBy(servicePredicateBuilder));
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<TEntity> SelectByAsync(ExpressionInfo<TEntity> expressionInfo)
        {
            return Task.Run(() => SelectBy(expressionInfo));
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<TEntity> SelectByAsync(TIdentifier identifier, List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectBy(identifier, includeNavigationProperties));
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

        [DataObjectMethod(DataObjectMethodType.Select)]
        public int SelectCount(ExpressionInfo<TEntity> expressionInfo)
        {
            return SelectCount(expressionInfo.Expression);
        }

        // ----------- Async -----------------

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<int> SelectCountAsync()
        {
            return DbSet.CountAsync();
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<int> SelectCountAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return DbSet.CountAsync(predicate);
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<int> SelectCountAsync(ServicePredicateBuilder<TEntity> servicePredicateBuilder)
        {
            return SelectCountAsync(servicePredicateBuilder.Criteria.GetExpression());
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<int> SelectCountAsync(ExpressionInfo<TEntity> expressionInfo)
        {
            return SelectCountAsync(expressionInfo.Expression);
        }

        #endregion

        #region CountOfPage

        [DataObjectMethod(DataObjectMethodType.Select)]
        public int CountOfPage(Expression<Func<TEntity, bool>> predicate, int itemPerPage)
        {
            var allItemsCount = SelectCount(predicate);
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
        public int CountOfPage(ExpressionInfo<TEntity> expressionInfo)
        {
            return CountOfPage(expressionInfo.Expression, expressionInfo.PaginationData.ItemsPerPage);
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public int CountOfPage(int itemPerPage)
        {
            var allItemsCount = SelectCount();
            return (allItemsCount % itemPerPage) == 0
                       ? (allItemsCount / itemPerPage)
                       : (allItemsCount / itemPerPage) + 1;
        }

        // ----------- Async -----------------

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<int> CountOfPageAsync(Expression<Func<TEntity, bool>> predicate, int itemPerPage)
        {
            return Task.Run(() => CountOfPage(predicate, itemPerPage));
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<int> CountOfPageAsync(ServicePredicateBuilder<TEntity> servicePredicateBuilder)
        {
            return Task.Run(() => CountOfPage(servicePredicateBuilder.Criteria.GetExpression(), servicePredicateBuilder.PaginationData.ItemsPerPage));
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<int> CountOfPageAsync(ExpressionInfo<TEntity> expressionInfo)
        {
            return Task.Run(() => CountOfPage(expressionInfo));
        }

        [DataObjectMethod(DataObjectMethodType.Select)]
        public Task<int> CountOfPageAsync(int itemPerPage)
        {
            return Task.Run(() => CountOfPage(itemPerPage));
        }

        #endregion

        #endregion

        #region Update

        protected virtual void BeforeUpdate(TEntity entity) { }
        protected virtual void AfterSaveChanges(TEntity entity) { }

        public void SaveChanges(List<TEntity> entities = null)
        {
            try
            {
                if (entities != null)
                    foreach (var entity in entities)
                    {
                        BeforeUpdateOrSaveChangesOrAddOrInsert(entity);
                        BeforeUpdate(entity);
                    }
                Context.SaveChanges();
                if (entities != null)
                    foreach (var entity in entities)
                        AfterSaveChanges(entity);
            }
            catch (Exception ex)
            {
                GetExceptions(ex);
            }
        }

        public TEntity UpdateWithSaveChanges(TEntity entity)
        {
            return UpdateWithSaveChanges(new List<TEntity> { entity }).First();
        }

        public List<TEntity> UpdateWithSaveChanges(List<TEntity> entities)
        {
            Update(entities);
            SaveChanges(entities);
            return entities;
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
            {
                BeforeUpdate(entity);
                BeforeUpdateOrSaveChangesOrAddOrInsert(entity);
                Context.Entry(entity).State = EntityState.Modified;
            }
            return entities;
        }

        public TEntity ChangeState(TEntity entity, EntityState entityState)
        {
            Context.Entry(entity).State = entityState;
            return entity;
        }

        public List<TEntity> ChangeState(List<TEntity> entities, EntityState entityState)
        {
            foreach (var entity in entities)
                Context.Entry(entity).State = EntityState.Modified;
            return entities;
        }

        // ----------- Async -----------------

        public Task SaveChangesAsync()
        {
            return Context.SaveChangesAsync();
        }

        public Task<TEntity> UpdateWithSaveChangesAsync(TEntity entity)
        {
            return Task.Run(() => UpdateWithSaveChanges(entity));
        }

        public Task<List<TEntity>> UpdateWithSaveChangesAsync(List<TEntity> entities)
        {
            return Task.Run(() => UpdateWithSaveChanges(entities));
        }

        #endregion

        #region FullTextSearch

        public List<TEntity> FullTextSearch(string searchText, string conditionTSql, int page, int itemsPerPage, List<Expression<Func<TEntity, string>>> columnsForSearching, List<KeyValuePair<Expression<Func<TEntity, object>>, SortDirection>> sortingsValuePairs)
        {
            searchText = searchText.Trim();
            if (string.IsNullOrEmpty(searchText))
                throw new Exception("searchText نباید خالی باشد");

            var columnsToSearch = columnsForSearching.Select(expression => expression.Body.ToString().Replace("Convert", "").Replace(")", "").Replace("(", "")).Aggregate(string.Empty, (current, s) => current +
                                                                                                                                                                                                        $"{s.Remove(0, s.IndexOf('.') + 1)}, ");
            columnsToSearch = columnsToSearch.Remove(columnsToSearch.LastIndexOf(','));
            if (string.IsNullOrWhiteSpace(columnsToSearch)) return null;

            var orderingString = string.Empty;
            foreach (var item in sortingsValuePairs)
            {
                var columnName = item.Key.Body.ToString().Replace("Convert", "").Replace(")", "").Replace("(", "");
                columnName = columnName.Remove(0, columnName.IndexOf('.') + 1);
                orderingString +=
                    $"{{AlternativeTableNamePlaceHolder}}.{columnName} {(item.Value == SortDirection.Ascending ? "ASC" : "DESC")}, ";
            }
            orderingString = orderingString.Remove(orderingString.LastIndexOf(','));

            using (var sqlConnection = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand("FullTextSearch", sqlConnection))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@WhereExpression", SqlDbType.NVarChar).Value = conditionTSql;
                cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = page;
                cmd.Parameters.Add("@ItemsPerPage", SqlDbType.Int).Value = itemsPerPage;
                cmd.Parameters.Add("@TableName", SqlDbType.NVarChar).Value = GetCurrentEntityTableName();
                cmd.Parameters.Add("@ColumnNamesToSearch", SqlDbType.NVarChar).Value = columnsToSearch;
                cmd.Parameters.Add("@SearchText", SqlDbType.NVarChar).Value = searchText;
                cmd.Parameters.Add("@OrderingString", SqlDbType.NVarChar).Value = orderingString;

                sqlConnection.Open();
                List<TEntity> entities;
                using (var sqlDataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
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
            var columnsTosearch = columnsForSearching.Select(expression => expression.Body.ToString().Replace("Convert", "").Replace(")", "").Replace("(", "")).Aggregate(string.Empty, (current, s) => current +
                                                                                                                                                                                                        $"{s.Remove(0, s.IndexOf('.') + 1)}, ");
            columnsTosearch = columnsTosearch.Remove(columnsTosearch.LastIndexOf(','));

            using (var sqlConnection = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand("FullTextSearchCount", sqlConnection))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@WhereExpression", SqlDbType.NVarChar).Value = conditionTSql;
                cmd.Parameters.Add("@TableName", SqlDbType.NVarChar).Value = GetCurrentEntityTableName();
                cmd.Parameters.Add("@ColumnNamesToSearch", SqlDbType.NVarChar).Value = columnsTosearch;
                cmd.Parameters.Add("@SearchText", SqlDbType.NVarChar).Value = searchText;
                sqlConnection.Open();
                return (int)cmd.ExecuteScalar();
            }
        }

        public List<TEntity> ConvertSqlDataReaderToEntities(SqlDataReader sqlDataReader)
        {
            var entities = new List<TEntity>();
            var props = EntityType.GetProperties();

            while (sqlDataReader.Read())
            {
                var entity = Activator.CreateInstance<TEntity>();
                foreach (var col in props)
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
            var list = new List<TIdentifier>();
            while (dataReader.Read())
                list.Add((TIdentifier)dataReader.GetValue(0));
            return list;
        }

        // ----------- Async -----------------

        public Task<List<TEntity>> FullTextSearchAsync(string searchText, string conditionTSql, int page, int itemsPerPage, List<Expression<Func<TEntity, string>>> columnsForSearching, List<KeyValuePair<Expression<Func<TEntity, object>>, SortDirection>> sortingsValuePairs)
        {
            return Task.Run(() => FullTextSearch(searchText, conditionTSql, page, itemsPerPage, columnsForSearching, sortingsValuePairs));
        }
        public Task<int> FullTextSearchCountAsync(string searchText, string conditionTSql, List<Expression<Func<TEntity, string>>> columnsForSearching)
        {
            return Task.Run(() => FullTextSearchCount(searchText, conditionTSql, columnsForSearching));
        }

        public Task<List<TEntity>> ConvertSqlDataReaderToEntitiesAsync(SqlDataReader sqlDataReader)
        {
            return Task.Run(() => ConvertSqlDataReaderToEntities(sqlDataReader));
        }
        public Task<List<TIdentifier>> ConvertSqlDataReaderToListAsync(IDataReader dataReader)
        {
            return Task.Run(() => ConvertSqlDataReaderToList(dataReader));
        }

        #endregion

        #endregion

        private void GetExceptions(Exception ex)
        {
            var exception = ex;
            var stackTrace = new StackTrace(1);
            var exceptionMessage = $"Exception at '{EntityType.Name}', '{stackTrace.GetFrame(0).GetMethod().Name}': {Environment.NewLine}{ex.Message}";
            while (exception.InnerException != null)
            {
                exceptionMessage += $"{Environment.NewLine}InnerMessage: {exception.InnerException.Message}";
                exception = exception.InnerException;
            }

            var dbEntityValidationException = ex as DbEntityValidationException;
            if (dbEntityValidationException?.EntityValidationErrors == null || !dbEntityValidationException.EntityValidationErrors.Any())
                throw new Exception(exceptionMessage, ex);

            exceptionMessage += Environment.NewLine;

            var dbEntityValidationResults = dbEntityValidationException.EntityValidationErrors.ToList();
            foreach (var dbEntityValidationResult in dbEntityValidationResults)
            {
                var entityName = dbEntityValidationResult.Entry.Entity.GetType().Name;
                exceptionMessage = dbEntityValidationResult.ValidationErrors.Aggregate(exceptionMessage, (current, validationError) => current + $"{Environment.NewLine}EntityValidationError: PropertyName='{validationError.PropertyName}' in '{entityName}', {validationError.ErrorMessage}");
            }

            throw new Exception(exceptionMessage, ex);
        }
    }
}