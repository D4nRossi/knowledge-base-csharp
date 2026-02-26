//DTOs são objetos de transferência - trafegam entre camadas sem expor entidades do Domain
//A API nunca recebe ou retorna uma entidade Domain diretamente

namespace KnowledgeBase.Application.DTOs;

public record DocumentDto(
    Guid Id,
    string Title,
    string Source,
    DateTime CreatedAt,
    int ChunkCount
);

public record IngestDocumentRequest(
    string Title,
    string Source,
    string Content
);

public record QueryRequest(
    string Question,
    int TopK = 5 //Quantos chunks buscar - mais chunks = mais contexto, mas resposta mais lenta
);

public record QueryResponse(
    string Answer,
    IEnumerable<string> SourceChunks //Chunks usados para gerar a resposta - transparência do RAG
);