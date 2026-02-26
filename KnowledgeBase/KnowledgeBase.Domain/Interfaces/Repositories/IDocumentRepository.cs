// O Domain define O QUE precisa ser feito - Infrastructure define COMO.
// Isso é Dependency Inversion: a camada de dentro define a interface,
// a camada de fora implementa.
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Domain.Interfaces.Repositories;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(Document document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}