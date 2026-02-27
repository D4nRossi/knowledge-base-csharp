using Cassandra;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace KnowledgeBase.Infrastructure.Cassandra.Repositories;

public class ChunkRepository : IChunkRepository
{
    private readonly ISession _session;
    private readonly ILogger<ChunkRepository> _logger;

    public ChunkRepository(CassandraContext context, ILogger<ChunkRepository> logger)
    {
        _session = context.Session;
        _logger = logger;
    }
    
    public async Task<IEnumerable<Chunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var statement =
            await _session.PrepareAsync(
                "SELECT document_id, chunk_index, id, content, vector_id, createdAt FROM chunks WHERE document_id = ?");
        
        var result = await _session.ExecuteAsync(statement.Bind(documentId));

        return result.Select(row => Chunk.Reconstitute(
            row.GetValue<Guid>("id"),
            row.GetValue<Guid>("document_id"),
            row.GetValue<string>("content"),
            row.GetValue<int>("chunk_index"),
            row.GetValue<string>("vector_id")
        ));
    }

    public async Task SaveManyAsync(IEnumerable<Chunk> chunks, CancellationToken cancellationToken = default)
    {
        var statement = await _session.PrepareAsync(
            @"INSERT INTO chunks (document_id, chunk_index, id, content, vector_id, createdAt) VALUES (?,?,?,?,?,?)");
        
        //BatchStatement agrupa múltiplos inserts numa operação atômica
        //Ideal pra salvar todos os chunks de um documento de uma vez
        var batch = new BatchStatement();

        foreach (var chunk in chunks)
        {
            batch.Add(statement.Bind(
                chunk.DocumentId,
                chunk.ChunkIndex,
                chunk.Id,
                chunk.Content,
                chunk.VectorId,
                chunk.CreatedAt
            ));
        }
        
        await _session.ExecuteAsync(batch);
        _logger.LogDebug("Saved {Count} chunks", chunks.Count());
    }
}