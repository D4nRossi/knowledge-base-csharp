using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Domain.Interfaces.Services;

// O Domain não sabe qual banco vetorial esta sendo usado.
// Trocar o Qdrant por outro amanhã = só mudar a interface

public interface IVectorStore
{
    //Indexa um embedding e retorna o ID gerado pelo vector store
    Task<string> IndexAsync(Guid chunkId, float[] embedding, CancellationToken cancellationToken = default);
    
    //Busca os N chunks mais similares ao embedding da query
    Task<IEnumerable<Guid>> SerachAsync(float[] queryEmbedding, int topK = 5, CancellationToken cancellationToken = default);
}