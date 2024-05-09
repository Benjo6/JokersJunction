using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace JokersJunction.Common.Databases.Base.Interfaces;

public interface IDocument
{
    /// <summary>
    /// The object ID that is used as the unique identifier by the MongoDB driver.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    ObjectId Id { get; set; }

    /// <summary>
    /// Timestamp to indicate the time of when the document was created.
    /// </summary>
    DateTime CreatedAt { get; }

    string? Name { get; set; }
}
