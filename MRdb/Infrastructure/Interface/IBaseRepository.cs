using MongoDB.Bson;
using MongoDB.Driver;
using MRDb.Tools;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MRDb.Infrastructure.Interface
{
    public interface IRepository<TEntity> : IDisposable
        where TEntity : IEntity, new()
    {
        #region getters

        IMongoDatabase Db { get; }
        IMongoCollection<TEntity> Collection { get; set; }

        #endregion


        #region get

        Task<ICollection<TEntity>> Get(DbQuery<TEntity> search);

        Task<ICollection<TEntity>> Get(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> sort = null, bool desc = true);

        Task<ICollection<TEntity>> Get(Expression<Func<TEntity, bool>> expression, int? skip = null, int? limit = null, Expression<Func<TEntity, object>> sort = null, bool desc = true);

        Task<TEntity> GetFirst(DbQuery<TEntity> search);

        Task<TEntity> GetFirst(Expression<Func<TEntity, bool>> expression);

        Task<TEntity> GetFirst(string id);

        #endregion

        #region insert

        Task<TEntity> Insert(TEntity entity);

        Task<IEnumerable<TEntity>> Insert(IEnumerable<TEntity> list);

        #endregion

        #region update
        Task<UpdateResult> Update(DbQuery<TEntity> query);

        Task<UpdateResult> Update(string id, DbQuery<TEntity> query);

        Task<UpdateResult> UpdateMany(DbQuery<TEntity> query);
        
        Task<TEntity> Replace(TEntity entity);

        #endregion

        #region remove

        Task<DeleteResult> Remove(string id);

        Task<DeleteResult> Remove(IEnumerable<string> ids);

        Task<UpdateResult> RemoveSoft(string id);

        #endregion

        #region count

        Task<long> Count(Expression<Func<TEntity, bool>> predicate);

        Task<long> Count();

        #endregion
    }
}
