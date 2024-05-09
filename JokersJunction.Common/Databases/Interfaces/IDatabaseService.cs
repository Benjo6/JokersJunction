using JokersJunction.Common.Databases.Base.Interfaces;
using MongoDB.Bson;

namespace JokersJunction.Common.Databases.Interfaces;

public interface IDatabaseService
{
    /// <summary>
    /// Inserts a single document into the database.
    /// </summary>
    /// <typeparam name="TDocument">The type of document to insert.</typeparam>
    /// <param name="document">The document to insert.</param>
    void InsertOne<TDocument>(TDocument document) where TDocument : class, IDocument;

    /// <summary>
    /// Asynchronously retrieves all documents of a specified type from the database.
    /// </summary>
    /// <typeparam name="TDocument">The type of documents to retrieve.</typeparam>
    Task<List<TDocument>> ReadAsync<TDocument>() where TDocument : class, IDocument;

    /// <summary>
    /// Asynchronously retrieves a document of a specified type by name from the database.
    /// </summary>
    /// <typeparam name="TDocument">The type of document to retrieve.</typeparam>
    /// <param name="name">The name of the document.</param>
    Task<TDocument?> GetOneByNameAsync<TDocument>(string name) where TDocument : class, IDocument;

    /// <summary>
    /// Asynchronously retrieves a document of a specified type by ID from the database.
    /// </summary>
    /// <typeparam name="TDocument">The type of document to retrieve.</typeparam>
    /// <param name="id">The ID of the document.</param>
    Task<TDocument?> GetOneFromIdAsync<TDocument>(string id) where TDocument : class, IDocument;

    /// <summary>
    /// Asynchronously replaces an existing document with a new document in the database.
    /// </summary>
    /// <typeparam name="TDocument">The type of document to replace.</typeparam>
    /// <param name="document">The new document to replace the existing one.</param>
    Task ReplaceOneAsync<TDocument>(TDocument document) where TDocument : class, IDocument;

    /// <summary>
    /// Asynchronously deletes a document from the database.
    /// </summary>
    /// <typeparam name="TDocument">The type of document to delete.</typeparam>
    /// <param name="document">The document to delete.</param>
    /// <returns>True if the document is successfully deleted; otherwise, false.</returns>
    Task<bool> DeleteOneAsync<TDocument>(TDocument document) where TDocument : class, IDocument;
}