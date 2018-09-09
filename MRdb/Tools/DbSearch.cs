using MongoDB.Driver;
using MRDb.Infrastructure.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MRDb.Tools
{
    public class DbQuery<TEntity>
        where TEntity : IEntity, new()
    {
        public FilterDefinition<TEntity> FilterDefinition { get; set; }
        public SortDefinition<TEntity> SortDefenition { get; set; }
        public UpdateDefinition<TEntity> UpdateDefinition { get; set; }

        public ProjectionDefinition<TEntity> ProjectionDefinition { get; set; }

        public int? Skip { get; set; } = null;
        public int? Limit { get; set; } = null;

        protected FilterDefinitionBuilder<TEntity> _builder = Builders<TEntity>.Filter;
        protected SortDefinitionBuilder<TEntity> _sortBuilder = Builders<TEntity>.Sort;
        protected UpdateDefinitionBuilder<TEntity> _updateBuilder = Builders<TEntity>.Update;
        protected ProjectionDefinitionBuilder<TEntity> _projectionBuilder = Builders<TEntity>.Projection;

        public DbQuery()
        {
            FilterDefinition = _builder.Where(x => true);
        }

        public DbQuery(int? skip, int? limit) : this()
        {
            Skip = skip;
            Limit = limit;
        }

        #region search

        public DbQuery<TEntity> Where(Expression<Func<TEntity, bool>> expression)
        {
            FilterDefinition = _builder.And(FilterDefinition, _builder.Where(expression));
            return this;
        }

        public DbQuery<TEntity> Contains<TField>(Expression<Func<TEntity, TField>> expression, IEnumerable<TField> values)
        {
            if(values != null && values.Any())
            {
                FilterDefinition = _builder.And(FilterDefinition, _builder.In(expression, values));
            }

            return this;
        }

        public DbQuery<TEntity> Eq<TField>(Expression<Func<TEntity, TField>> expression, TField value)
        {
            FilterDefinition = _builder.And(FilterDefinition, _builder.Eq(expression, value));
            return this;
        }

        public DbQuery<TEntity> CustomSearch(Expression<Func<FilterDefinitionBuilder<TEntity>, FilterDefinition<TEntity>>> filter)
        {
            FilterDefinition = _builder.And(FilterDefinition, filter.Compile().Invoke(_builder));
            return this;
        }

        #endregion

        #region sort

        public DbQuery<TEntity> Ascending(Expression<Func<TEntity, object>> expression)
        {
            if(SortDefenition == null)
            {
                SortDefenition = _sortBuilder.Ascending(expression);
            }
            else
            {
                SortDefenition = _sortBuilder.Combine(SortDefenition, _sortBuilder.Ascending(expression));
            }

            return this;
        }

        public DbQuery<TEntity> Descending(Expression<Func<TEntity, object>> expression)
        {
            if (SortDefenition == null)
            {
                SortDefenition = _sortBuilder.Descending(expression);
            }
            else
            {
                SortDefenition = _sortBuilder.Combine(SortDefenition, _sortBuilder.Descending(expression));
            }

            return this;
        }

        #endregion

        #region projection

        public DbQuery<TEntity> Projection(Expression<Func<ProjectionDefinitionBuilder<TEntity>, ProjectionDefinition<TEntity>>> expression)
        {
            ProjectionDefinition = expression.Compile().Invoke(_projectionBuilder);
            return this;
        }

        #endregion

        #region update

        public DbQuery<TEntity> Update(Expression<Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>>> expression)
        {
            UpdateDefinition = expression.Compile().Invoke(_updateBuilder);
            return this;
        }

        #endregion



    }

    public class DbSearchIn<TEntity, TField>
        where TEntity : IEntity, new()
    {
        public Expression<Func<TEntity, TField>> Field { get; set; }
        public TField Value { get; set; }
    }
}
