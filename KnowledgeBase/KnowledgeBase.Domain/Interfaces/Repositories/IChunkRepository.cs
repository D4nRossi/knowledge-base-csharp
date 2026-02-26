using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Domain.Interfaces.Repositories;

public interface IChunkRepository
{
    Task<IEnumerable<Chunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<Chunk> chunks, CancellationToken cancellationToken = default);
}