using JokersJunction.Common.Databases.Base.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace JokersJunction.Common.Databases.Base;

public class Document : IDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public DateTime CreatedAt => Id.CreationTime.ToLocalTime();
    public string? Name { get; set; }
}