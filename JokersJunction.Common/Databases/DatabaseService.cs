using JokersJunction.Common.Databases.Base.Interfaces;
using JokersJunction.Common.Databases.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace JokersJunction.Common.Databases;

public class DatabaseService : IDatabaseService
{
    private readonly IMongoDatabase _mongoDatabase;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseService"/> class.
    /// </summary>
    /// <param name="connectionString"></param>
    public DatabaseService(string? connectionString)
    {
        var mongoClient = new MongoClient(connectionString);
        _mongoDatabase = mongoClient.GetDatabase("GameManagement");
    }
    /// <inheritdoc/>
    public void InsertOne<TDocument>(TDocument document) where TDocument : class, IDocument
    {
        var collection = _mongoDatabase.GetCollection<TDocument>(typeof(TDocument).Name) ?? throw new NullReferenceException($"The collection with the name {nameof(TDocument)} does not exist"); collection?.InsertOne(document);
    }

    /// <inheritdoc/>
    public async Task<List<TDocument>> ReadAsync<TDocument>() where TDocument : class, IDocument
    {
        var collection = _mongoDatabase.GetCollection<TDocument>(typeof(TDocument).Name) ?? throw new NullReferenceException($"The collection with the name {nameof(TDocument)} does not exist");
        return await collection.Find(_ => true).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<TDocument?> GetOneByNameAsync<TDocument>(string name) where TDocument : class, IDocument
    {
        var collection = _mongoDatabase.GetCollection<TDocument>(typeof(TDocument).Name) ?? throw new NullReferenceException($"The collection with the name {nameof(TDocument)} does not exist");
        // If the collection exists, then retrieve the document with the specified ID.
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Name, name);
        return await collection.Find(filter).SingleOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<TDocument?> GetOneFromIdAsync<TDocument>(string id) where TDocument : class, IDocument
    {
        var collection = _mongoDatabase.GetCollection<TDocument>(typeof(TDocument).Name) ?? throw new NullReferenceException($"The collection with the name {nameof(TDocument)} does not exist");

        // Use the string id in the filter
        var filter = Builders<TDocument>.Filter.Eq("_id", ObjectId.Parse(id));
        return await collection.FindAsync(filter).Result.SingleOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task ReplaceOneAsync<TDocument>(TDocument document) where TDocument : class, IDocument
    {
        var collection = _mongoDatabase.GetCollection<TDocument>(typeof(TDocument).Name) ?? throw new NullReferenceException($"The collection with the name {nameof(TDocument)} does not exist");
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);
        _ = await collection.FindOneAndReplaceAsync(filter, document);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteOneAsync<TDocument>(TDocument document) where TDocument : class, IDocument
    {
        var collection = _mongoDatabase.GetCollection<TDocument>(typeof(TDocument).Name);
        if (collection is null)
        {
            return false;
        }
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);
        var result = await collection.FindOneAndDeleteAsync(filter);
        return result != null;
    }
}