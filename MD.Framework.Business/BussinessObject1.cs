using System.Data.Entity;

namespace MD.Framework.Business
{
    public abstract class BusinessObject<TContext, TEntity, TIdentifier> : BusinessObject<TEntity, TIdentifier>
            where TEntity : class
            where TContext : DbContext
            where TIdentifier : struct
    {
        public new TContext Context { get; set; }

        protected BusinessObject(TContext context) : base(context)
        {
            Context = context;
        }
    }
}