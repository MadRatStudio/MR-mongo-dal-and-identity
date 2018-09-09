using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MRDb.Infrastructure.Interface;
using System;

namespace MRDb.Domain
{
    public class Entity : IEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
        public bool State { get; set; } = true;

        public IEntity OnUpdate()
        {
            UpdatedTime = DateTime.UtcNow;
            return this;
        }
    }
}
