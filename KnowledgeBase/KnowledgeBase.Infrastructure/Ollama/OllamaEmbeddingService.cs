//OllamaEmbeddingService gera embeddings chamando a API HTTP do Ollama
//Ollama expoe uma API REST simples - sem SDK oficial em .NET, então usamos HttpClient diretamente. Isso é proposital: você aprende como APIs de LLM funcionam por baixo

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KnowledgeBase.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KnowledgeBase.Infrastructure.Ollama;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaEmbeddingService> _logger;
    private readonly string _model;

    public OllamaEmbeddingService(HttpClient httpClient, ILogger<OllamaEmbeddingService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _model = configuration["Ollama:EmbedModel"] ?? "nomic-embed-text";
    }

    public async Task<float[]> GenerateAsync(string text, CancellationToken cancelationToken = default)
    {
        var request = new EmbeddingRequest(Model: _model, Prompt: text);
        
        var response = await _httpClient.PostAsJsonAsync("/api/embeddings", request, cancelationToken);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancelationToken);

        if (result?.Embedding is null)
            throw new InvalidOperationException("Ollama returned empty embedding");
        
        _logger.LogDebug("Generated embedding with {Dimensions} dimensions", result.Embedding.Length);
        
        return result.Embedding;
    }

    //Records mapeaiam exatamente o JSON API do Ollama
    private record EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt
    );

    private record EmbeddingResponse(
        [property: JsonPropertyName("embedding")]
        float[] Embedding
    );
}