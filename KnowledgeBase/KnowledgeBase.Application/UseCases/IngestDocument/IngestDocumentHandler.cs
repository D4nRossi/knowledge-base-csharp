//Handler contém a lógica do use case - é aqui que a orquestração acontece
//Ele conhece as interfaces do Domain mas não sabe nada das implementações concretas
// As implementações são injetadas pelo container DI em runtime

using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Interfaces;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Interfaces.Repositories;
using KnowledgeBase.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KnowledgeBase.Application.UseCases.IngestDocument;

public class IngestDocumentHandler : IRequestHandler<IngestDocumentCommand, DocumentDto>
{
    
    private readonly IDocumentRepository _documentRepository;
    private readonly IChunkRepository _chunkRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IChunkerService _chunkerService;
    private readonly ILogger<IngestDocumentHandler> _logger;

    public IngestDocumentHandler(
        IDocumentRepository documentRepository, 
        IChunkRepository chunkRepository, 
        IEmbeddingService embeddingService, 
        IVectorStore vectorStore, 
        IChunkerService chunkerService, 
        ILogger<IngestDocumentHandler> logger)
    {
        _documentRepository = documentRepository;
        _chunkRepository = chunkRepository;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _chunkerService = chunkerService;
        _logger = logger;
    }

    public async Task<DocumentDto> Handle(IngestDocumentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting ingestion for document: {Title}", request.Title);
        
        //Cria a entidade Document via factory method do Domain
        var document = Document.Create(request.Title, request.Source, request.Content);
        
        //Divide o conteúdo em chunks
        var chunkContents = _chunkerService.Chunk(request.Content).ToList();
        _logger.LogInformation("Document split into {Count} chunks}", chunkContents.Count);
        
        //Pra cada chunk: gere embedding, indexa no Qdrant, cria entidade Chunk
        for (var i = 0; i < chunkContents.Count; i++)
        {
            var chunkContent = chunkContents[i];
            
            //Gera o vetor de embedding via Ollama
            var embedding = await _embeddingService.GenerateAsync(chunkContent, cancellationToken);
            
            //Indexa no Qdrant e recebe o ID gerado
            var vectorId = await _vectorStore.IndexAsync(document.Id, embedding, cancellationToken);
            
            //Cria o Chunk e associa o vectorId
            var chunk = Chunk.Create(document.Id, chunkContent, i);
            chunk.AssignVectorId(vectorId);
            
            document.AddChunk(chunk);
        }
        
        //Persiste o documento e os chunks no Cassandra
        await _documentRepository.SaveAsync(document, cancellationToken);
        await _chunkRepository.SaveManyAsync(document.Chunks, cancellationToken);
        
        _logger.LogInformation("Document ingested sucessfully: {DocumentId}", document.Id);

        return new DocumentDto(
            document.Id,
            document.Title,
            document.Source,
            document.CreatedAt,
            document.Chunks.Count
        );
    }
}