using System.Net.Http.Headers;

namespace KnowledgeBase.Domain.Interfaces.Services;

//IEmbeddingService abstrai qualquer modelo de embedding - Ollama local, Azure AI, etc.

public interface IEmbeddingService
{
    //Gera um vetor de embedding a partir de um contexto
    Task<float[]> GenerateAsync(string text, CancellationToken cancelationToken =  default);
}