using MRDb.Infrastructure.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace MRDb.Infrastructure.Exceptions
{
    public class EntityNotFoundException : Exception
    {
        public Type EntityType { get; set; }
        public string ExpectedId { get; set; }


        public EntityNotFoundException(Type entityType, string id) : base($"Entity {nameof(entityType)} with id {id} not found")
        {
            EntityType = entityType;
            ExpectedId = id;
        }
    }
}
