using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace MRDb.Infrastructure.Interface
{
    public interface IEntity 
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        string Id { get; set; }
        DateTime CreatedTime { get; set; }
        DateTime UpdatedTime { get; set; }

        bool State { get; set; }

        IEntity OnUpdate();
    }
}
