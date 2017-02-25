using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MD.Framework.Business.Core
{
    public abstract class BusinessObject<TEntity, TIdentifier>
            where TEntity : class
            where TIdentifier : struct
    {
        #region Properties and Fields

        public DbContext Context { get; set; }
        private DbSet<TEntity> DbSet { get; }

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

        public void Delete(TIdentifier identifier)
        {
            Delete(new List<TIdentifier> { identifier });
        }

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

        public void Delete(TEntity entity)
        {
            Delete(new List<TEntity> { entity });
        }

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

        public Task DeleteAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return Task.Run(() => Delete(predicate));
        }

        public Task DeleteAsync(TIdentifier identifier)
        {
            return Task.Run(() => Delete(identifier));
        }

        public Task DeleteAsync(IEnumerable<TIdentifier> identifiers)
        {
            return Task.Run(() => Delete(identifiers));
        }

        public Task DeleteAsync(TEntity entity)
        {
            return Task.Run(() => Delete(entity));
        }

        public Task DeleteAsync(IEnumerable<TEntity> entities)
        {
            return Task.Run(() => Delete(entities));
        }

        #endregion

        #region Remove
        
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

        public void Remove(TIdentifier identifier)
        {
            var entity = SelectBy(identifier);
            if (entity == null) return;
            BeforeRemoveOrDelete(entity);
            DbSet.Remove(entity);
            Context.Entry(entity).State = EntityState.Deleted;
        }

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

        public void Remove(TEntity entity)
        {
            BeforeRemoveOrDelete(entity);
            DbSet.Remove(entity);
            Context.Entry(entity).State = EntityState.Deleted;
        }

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

        public void Add(TEntity entity)
        {
            Add(new List<TEntity> { entity });
        }

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

        public TEntity Insert(TEntity entity)
        {
            return Insert(new List<TEntity> { entity }).FirstOrDefault();
        }

        public List<TEntity> Insert(List<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                BeforeAddOrInsert(entity);
                DbSet.Add(entity);
                Context.Entry(entity).State = EntityState.Added;
            }
            SaveChanges(entities);
            return entities;
        }

        // ----------- Async -----------------

        public Task<TEntity> InsertAsync(TEntity entity)
        {
            return Task.Run(() => Insert(entity));
        }

        public Task<List<TEntity>> InsertAsync(List<TEntity> entities)
        {
            return Task.Run(() => Insert(entities));
        }

        #endregion

        #region Select

        #region SelectAll

        public IQueryable<TEntity> SelectAll(List<string> includeNavigationProperties = null)
        {
            IQueryable<TEntity> result = DbSet;
            if (includeNavigationProperties == null || includeNavigationProperties.Count <= 0) return result;
            return includeNavigationProperties.Aggregate(result, (current, property) => current.Include(property));
        }
        
        public IQueryable<TEntity> SelectAll(List<TIdentifier> ids, List<string> includeNavigationProperties = null)
        {
            var iCollectionType = typeof(ICollection<TIdentifier>);
            var parameterExpression = Expression.Parameter(EntityType, "q");
            var constantExpression = Expression.Constant(ids, iCollectionType);
            var memberExpression = Expression.Property(parameterExpression, IdentifierColumnName);
            var containsMethodInfo = iCollectionType.GetMethod("Contains", new[] { IdentifierType });

            var methodCallExpression = Expression.Call(constantExpression, containsMethodInfo, memberExpression);
            var lambdaExpression = Expression.Lambda<Func<TEntity, bool>>(methodCallExpression, parameterExpression);

            if (includeNavigationProperties == null || includeNavigationProperties.Count <= 0) return DbSet.Where(lambdaExpression);
            var query = includeNavigationProperties.Aggregate<string, IQueryable<TEntity>>(DbSet, (current, property) => current.Include(property));

            return query.Where(lambdaExpression);
        }
        
        public IQueryable<TEntity> SelectAll(Expression<Func<TEntity, bool>> predicate, List<string> includeNavigationProperties = null)
        {
            if (includeNavigationProperties == null || !includeNavigationProperties.Any())
                return DbSet.Where(predicate);
            var query = includeNavigationProperties.Aggregate<string, IQueryable<TEntity>>(DbSet, (current, property) => current.Include(property));
            return query.Where(predicate);
        }
        
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
                var query = includeNavigationProperties.Aggregate<string, IQueryable<TEntity>>(DbSet, (current, property) => current.Include(property));
                if (orderByExpression != null)
                    result = orderByFunc(query.Where(predicate));
                else
                    result = query.Where(predicate);
            }
            return result;
        }
        
        // ----------- Async -----------------

        public Task<IQueryable<TEntity>> SelectAllAsync(List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectAll(includeNavigationProperties));
        }
        
        public Task<IQueryable<TEntity>> SelectAllAsync(List<TIdentifier> ids, List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectAll(ids, includeNavigationProperties));
        }
        
        public Task<IQueryable<TEntity>> SelectAllAsync(Expression<Func<TEntity, bool>> predicate, List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectAll(predicate, includeNavigationProperties));
        }
        
        public Task<IQueryable<TEntity>> SelectAllAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression, List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectAll(predicate, orderByExpression, includeNavigationProperties));
        }

        #endregion

        #region SelectBy
        
        public TEntity SelectBy(Expression<Func<TEntity, bool>> predicate, List<string> includeNavigationProperties = null)
        {
            TEntity result;
            if (includeNavigationProperties == null || !includeNavigationProperties.Any())
                result = DbSet.FirstOrDefault(predicate);
            else
            {
                var query = includeNavigationProperties.Aggregate<string, IQueryable<TEntity>>(DbSet, (current, property) => current.Include(property));
                result = query.FirstOrDefault(predicate);
            }
            return result;
        }
        
        public TEntity SelectBy(TIdentifier identifier, List<string> includeNavigationProperties = null)
        {
            var parameter = Expression.Parameter(EntityType, "q");
            var propertyAccess = Expression.MakeMemberAccess(parameter, IdentifierPropertyInfo);
            var rightExpr = Expression.Constant(identifier, IdentifierType);
            return SelectBy(Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(propertyAccess, rightExpr), parameter), includeNavigationProperties);
        }

        // ----------- Async -----------------

        public Task<TEntity> SelectByAsync(Expression<Func<TEntity, bool>> predicate, List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectBy(predicate, includeNavigationProperties));
        }
        
        public Task<TEntity> SelectByAsync(TIdentifier identifier, List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectBy(identifier, includeNavigationProperties));
        }

        #endregion

        #region SelectCount
        
        public int SelectCount()
        {
            return DbSet.Count();
        }

        public int SelectCount(Expression<Func<TEntity, bool>> predicate)
        {
            return DbSet.Count(predicate);
        }

        // ----------- Async -----------------
        
        public Task<int> SelectCountAsync()
        {
            return DbSet.CountAsync();
        }
        
        public Task<int> SelectCountAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return DbSet.CountAsync(predicate);
        }

        #endregion

        #region SelectPage
        
        public IQueryable<TEntity> SelectPage(Expression<Func<TEntity, bool>> predicate, Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression, int currentPage, int itemsPerPage, List<string> includeNavigationProperties = null)
        {
            var orderByFunc = orderByExpression.Compile();

            if (includeNavigationProperties == null || !includeNavigationProperties.Any())
                return orderByFunc(DbSet.Where(predicate)).Skip(((currentPage - 1) * itemsPerPage)).Take(itemsPerPage);
            var query = includeNavigationProperties.Aggregate<string, IQueryable<TEntity>>(DbSet, (current, property) => current.Include(property));
            return orderByFunc(query.Where(predicate)).Skip(((currentPage - 1) * itemsPerPage)).Take(itemsPerPage);
        }
        
        public IQueryable<TEntity> SelectPage(Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression, int currentPage, int itemsPerPage, List<string> includeNavigationProperties = null)
        {
            var orderByFunc = orderByExpression.Compile();

            if (includeNavigationProperties == null || !includeNavigationProperties.Any())
                return orderByFunc(DbSet).Skip(((currentPage - 1) * itemsPerPage)).Take(itemsPerPage);
            var query = includeNavigationProperties.Aggregate<string, IQueryable<TEntity>>(DbSet, (current, property) => current.Include(property));
            return orderByFunc(query).Skip(((currentPage - 1) * itemsPerPage)).Take(itemsPerPage);
        }

        // ----------- Async -----------------

        public Task<IQueryable<TEntity>> SelectPageAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression, int currentPage, int itemsPerPage, List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectPage(predicate, orderByExpression, currentPage, itemsPerPage, includeNavigationProperties));
        }
        
        public Task<IQueryable<TEntity>> SelectPageAsync(Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> orderByExpression, int currentPage, int itemsPerPage, List<string> includeNavigationProperties = null)
        {
            return Task.Run(() => SelectPage(orderByExpression, currentPage, itemsPerPage, includeNavigationProperties));
        }

        #endregion

        #region CountOfPage

        public int CountOfPage(Expression<Func<TEntity, bool>> predicate, int itemPerPage)
        {
            var allItemsCount = SelectCount(predicate);
            return (allItemsCount % itemPerPage) == 0
                       ? (allItemsCount / itemPerPage)
                       : (allItemsCount / itemPerPage) + 1;
        }
        
        public int CountOfPage(int itemPerPage)
        {
            var allItemsCount = SelectCount();
            return (allItemsCount % itemPerPage) == 0
                       ? (allItemsCount / itemPerPage)
                       : (allItemsCount / itemPerPage) + 1;
        }

        // ----------- Async -----------------
        
        public Task<int> CountOfPageAsync(Expression<Func<TEntity, bool>> predicate, int itemPerPage)
        {
            return Task.Run(() => CountOfPage(predicate, itemPerPage));
        }

        public Task<int> CountOfPageAsync(int itemPerPage)
        {
            return Task.Run(() => CountOfPage(itemPerPage));
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
                    foreach (var entity in entities)
                        BeforeUpdateOrSaveChangesOrAddOrInsert(entity);
                Context.SaveChanges();
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
            foreach (var entity in entities)
                BeforeUpdate(entity);
            SaveChanges(entities);
            return entities;
        }

        public TEntity Update(TEntity entity)
        {
            return Update(new List<TEntity> { entity }).First();
        }

        public List<TEntity> Update(List<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                BeforeUpdate(entity);
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

        #endregion

        private void GetExceptions(Exception ex)
        {
            var exception = ex;
            var exceptionMessage = $"Exception at '{EntityType.Name}': {Environment.NewLine}{ex.Message}";
            while (exception.InnerException != null)
            {
                exceptionMessage += $"{Environment.NewLine}InnerMessage: {exception.InnerException.Message}";
                exception = exception.InnerException;
            }
            throw new Exception(exceptionMessage, ex);
        }
    }
}