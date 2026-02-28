//OllamaLLMService monta o prompt com contexto e gera a resposta via Ollama
//O prompt engineering aqui é simples mas eficaz - contexto + pergunta

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KnowledgeBase.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KnowledgeBase.Infrastructure.Ollama;

public class OllamaLLMService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaLLMService> _logger;
    private readonly string _model;

    public OllamaLLMService(HttpClient httpClient, ILogger<OllamaLLMService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _model = configuration["Ollama:Model"] ?? "llama3.2";
    }

    public async Task<string> GenerateAnswerAsync(string question, IEnumerable<string> contextChunks,
        CancellationToken cancellationToken = default)
    {
        //Monta o contexto concatenando os chunks relevantes
        var context = string.Join("\n\n---\n\n", contextChunks);

        //Prompt instrui o modelo a responder apenas com base no contexto fornecido
        //Isso evita alucinação - o modelo não inventa informações fora do contexto
        var prompt = $"""
                      You are a helpful assistant that answers questions based only on the provided context.
                      If the answer is not in the context, say "I don't have enough information to answer this question"

                      Context:
                      {context}

                      Question: {question}
                      """;

        var request = new GenerateRequest(Model: _model, Prompt: prompt, Stream: false);
        
        _logger.LogInformation("Sending query to Ollama model: {Model}", _model);
        
        var response = await _httpClient.PostAsJsonAsync("/api/generate", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GenerateResponse>(cancellationToken);
        
        if(result?.Response is null)
            throw new InvalidOperationException("Ollama returned empty response");
        
        return result.Response;
    }

    private record GenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream
    );

    private record GenerateResponse(
        [property: JsonPropertyName("response")]
        string Response
    );
}