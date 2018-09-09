using MongoDB.Bson;
using MongoDB.Driver;
using MRDb.Infrastructure.Interface;
using MRDb.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MRDb.Repository
{
    public class BaseRepository<TEntity> : IRepository<TEntity>
        where TEntity : IEntity, new()
    {
        #region protected

        protected IMongoDatabase _db { get; set; }
        protected IMongoCollection<TEntity> _collection { get; set; }

        protected DbQuery<TEntity> DbQuery => new DbQuery<TEntity>();

        #endregion

        #region getters

        public IMongoDatabase Db => _db;
        public IMongoCollection<TEntity> Collection { get; set; }

        #endregion

        #region constructors

        public BaseRepository(string connectionString, string database, string collection) : this(new MongoClient(connectionString).GetDatabase(database), collection) { }

        public BaseRepository(string connectionString, string database) : this(connectionString, database, nameof(TEntity)) { }

        public BaseRepository(IMongoDatabase mongoDatabase) : this(mongoDatabase, typeof(TEntity).Name) { }

        public BaseRepository(IMongoDatabase mongoDatabase, string collection)
        {
            _db = mongoDatabase;
            _collection = _db.GetCollection<TEntity>(collection);
        }

        #endregion

        #region get

        public virtual async Task<ICollection<TEntity>> Get(DbQuery<TEntity> search)
        {
            var results = _collection.Find(search.FilterDefinition);

            if(search.ProjectionDefinition != null)
            {
                results = results.Project<TEntity>(search.ProjectionDefinition);
            }

            if(search.SortDefenition != null)
            {
                results.Sort(search.SortDefenition);
            }

            if(search.Skip != 0 && search.Limit.HasValue && search.Limit.Value > 0)
            {
                results = results.Skip(search.Skip).Limit(search.Limit);
            }

            return await results.ToListAsync();
        }

        public virtual async Task<ICollection<TEntity>> Get(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> sort = null, bool desc = true) => await Get(expression, null, null, sort, desc);

        public virtual async Task<ICollection<TEntity>> Get(Expression<Func<TEntity, bool>> expression, int? skip = null, int? limit = null, Expression<Func<TEntity, object>> sort = null, bool desc = true)
        {
            var query = new DbQuery<TEntity>(skip, limit)
                .Where(expression);

            if(sort != null)
            {
                if (desc) query.Descending(sort);
                else query.Ascending(sort);
            }

            return await Get(query);
        }

        public virtual async Task<TEntity> GetFirst(DbQuery<TEntity> search)
        {
            var results = _collection.Find(search.FilterDefinition);

            if(search.ProjectionDefinition != null)
            {
                results = results.Project<TEntity>(search.ProjectionDefinition);
            }

            if(search.SortDefenition != null)
            {
                results = results.Sort(search.SortDefenition);
            }

            return await results.FirstOrDefaultAsync();
        }

        public virtual async Task<TEntity> GetFirst(Expression<Func<TEntity, bool>> expression) => await GetFirst(new DbQuery<TEntity>().Where(expression));

        public virtual async Task<TEntity> GetFirst(string id) => await GetFirst(DbQuery.Eq(x => x.Id, id));

        #endregion

        #region insert

        public virtual async Task<TEntity> Insert(TEntity entity)
        {
            entity.CreatedTime = entity.UpdatedTime = DateTime.UtcNow;
            await _collection.InsertOneAsync(entity);
            return entity;
        }

        public virtual async Task<IEnumerable<TEntity>> Insert(IEnumerable<TEntity> list)
        {
            if (list == null || !list.Any()) return new TEntity[0];
            foreach(var entity in list)
            {
                entity.CreatedTime = entity.UpdatedTime = DateTime.UtcNow;
            }

            await _collection.InsertManyAsync(list);
            return list;
        }

        #endregion

        #region update

        public virtual async Task<UpdateResult> Update(DbQuery<TEntity> query)
        {
            query.UpdateDefinition.Set(x => x.UpdatedTime, DateTime.UtcNow);
            return await _collection.UpdateOneAsync(query.FilterDefinition, query.UpdateDefinition);
        }

        public virtual async Task<UpdateResult> Update(string id, DbQuery<TEntity> query)
        {
            query.CustomSearch(x => x.Eq(z => z.Id, id));
            return await Update(query);
        }

        public virtual async Task<UpdateResult> UpdateMany(DbQuery<TEntity> query)
        {
            query.UpdateDefinition.Set(x => x.UpdatedTime, DateTime.UtcNow);
            return await _collection.UpdateManyAsync(query.FilterDefinition, query.UpdateDefinition);
        }

        public virtual async Task<TEntity> Replace(TEntity entity)
        {
            if (entity == null) return default(TEntity);
            entity.OnUpdate();
            await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }

        public virtual async Task<IList<TEntity>>  Replace(IEnumerable<TEntity> list)
        {
            if (list == null || !list.Any()) return new List<TEntity>();

            foreach(var entity in list)
            {
                await Replace(entity);
            }

            return list.ToList();
        }

        #endregion

        #region remove

        public virtual async Task<DeleteResult> Remove(string id)
        {
            return await _collection.DeleteOneAsync(x => x.Id == id);
        }

        public virtual async Task<DeleteResult> Remove(IEnumerable<string> ids)
        {
            return await _collection.DeleteManyAsync(x => ids.Contains(x.Id));
        }

        public virtual async Task<UpdateResult> RemoveSoft(string id)
        {
            var query = DbQuery.Update(x => x.Set(z => z.State, false));
            return await Update(id, query);
        }

        #endregion

        #region count

        public virtual async Task<long> Count(Expression<Func<TEntity, bool>> predicate)
        {
            return await _collection.CountDocumentsAsync(predicate);
        }

        public virtual async Task<long> Count() => await Count(x => true);

        #endregion

        public virtual void Dispose() { }

    }
}
