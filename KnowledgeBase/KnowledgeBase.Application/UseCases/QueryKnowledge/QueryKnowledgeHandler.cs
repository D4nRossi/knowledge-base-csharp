using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Interfaces;
using KnowledgeBase.Domain.Interfaces.Repositories;
using KnowledgeBase.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KnowledgeBase.Application.UseCases.QueryKnowledge;

public class QueryKnowledgeHandler : IRequestHandler<QueryKnowledgeQuery, QueryResponse>
{
    private readonly IChunkRepository _chunkRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore  _vectorStore;
    private readonly ILogger _logger; 
    
    //ILLMService abstrai o modelo de linguagem
    private readonly  ILLMService _llmService;
    
    public QueryKnowledgeHandler(
        IChunkRepository chunkRepository,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILogger<QueryKnowledgeHandler> logger,
        ILLMService  llmService
        )
    {
        _chunkRepository = chunkRepository;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _logger = logger;
        _llmService = llmService;
    }

    public async Task<QueryResponse> Handle(QueryKnowledgeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing query: {Question}", request.Question);
        
        //Gera embedding da pergunta
        var queryEmbedding = await _embeddingService.GenerateAsync(request.Question, cancellationToken);
        
        //Busca os chunks mais similares no Qdrant
        var chunkIds = (await _vectorStore.SerachAsync(queryEmbedding, request.TopK, cancellationToken)).ToList();
        _logger.LogInformation("Found {Count} relevant chunks", chunkIds.Count);
        
        //Busca o conteúdo dos chunks no Cassandra
        var chunks = new List<string>();
        foreach (var chunkId in chunkIds)
        {
            var documentChunks = await _chunkRepository.GetByDocumentIdAsync(chunkId, cancellationToken);
            chunks.AddRange(documentChunks.Select(c => c.Content));
        }
        
        //Monta o prompt com o contexto e envia pro LLM
        var answer = await _llmService.GenerateAnswerAsync(request.Question, chunks, cancellationToken);
        return new QueryResponse(answer, chunks);
    }
}