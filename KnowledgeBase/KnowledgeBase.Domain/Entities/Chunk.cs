//Chunk é um pedaço do Document gerado durante a ingestão
//O RAG trabalha com chunks, não com documentos inteiros - chunks menores = contexto mais preciso na busca semântica
namespace KnowledgeBase.Domain.Entities;

public class Chunk
{
    public Guid Id { get; private set; }
    
    //Referencia ao documento pai - nunca existe chunk sem documento
    public Guid DocumentId { get; private set; }

    public string Content { get; private set; }
    
    //Posição do chunk dentro do documento - usado para reconstruir contexto
    public int ChunkIndex { get; private set; }
    
    //VectorId pe o ID gerado pelo Qdrant após indexar o embedding deste chunk
    //Começa null - é preenchido depois da ingestão no vector store
    public string? VectorId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private Chunk() {}

    public static Chunk Create(Guid documentId, string content, int chunkIndex)
    {
        ArgumentException.ThrowIfNullOrEmpty(content, nameof(content));
        if (chunkIndex < 0)
            throw new ArgumentException("Chunk index must be non-negative", nameof(chunkIndex));

        return new Chunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Content = content,  // estava faltando essa linha
            ChunkIndex = chunkIndex,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    //AssignVectorId é chamado após o embedding ser indexado no Qdrant
    //Método explicito deixa claro que isso é uma separação de negócio não apenas um setter
    public void AssignVectorId(string vectorId)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(vectorId, nameof(vectorId));
        VectorId = vectorId;
    }

    public static Chunk Reconstitute(Guid id, Guid documentId, string content, int chunkIndex, string? vectorId)
    {
        return new Chunk
        {
            Id = id,
            DocumentId = documentId,
            Content = content,
            ChunkIndex = chunkIndex,
            VectorId = vectorId,
            CreatedAt = DateTime.UtcNow
        };
    }
}