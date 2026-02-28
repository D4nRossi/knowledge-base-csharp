//DependencyInjection é o único arquivo da Infraestrutura que a API conhece
//A API chama AddInfrastructure() e rece tudo configurado - sem precisar saber que exsiste Cassandra, Qdrant ou Ollama
//Isso é o princípio de encapsulamento aplicado a camadas inteiras

using KnowledgeBase.Application.Interfaces;
using KnowledgeBase.Domain.Interfaces.Repositories;
using KnowledgeBase.Domain.Interfaces.Services;
using KnowledgeBase.Infrastructure.Cassandra;
using KnowledgeBase.Infrastructure.Cassandra.Repositories;
using KnowledgeBase.Infrastructure.Chunking;
using KnowledgeBase.Infrastructure.Ollama;
using KnowledgeBase.Infrastructure.Qdrant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBase.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        //Cassandra - Singleton porque sessão é thread-safe e custosa pra criar
        services.AddSingleton<CassandraContext>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChunkRepository, ChunkRepository>();
        
        //Qdrant - Singleton pelo mesmo motivo
        services.AddSingleton<QdrantVectorStore>();
        services.AddSingleton<IVectorStore>(sp => sp.GetRequiredService<QdrantVectorStore>());
        
        //Ollama - HttpClient gerenciado pelo IHttpClientFactory
        //Isso evita socket exhaustion que acontece ao criar HttpClient manualmente
        var ollamaHost = configuration["Ollama:Host"] ?? "http://localhost:11434";

        services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(client =>
        {
            client.BaseAddress = new Uri(ollamaHost);
            //Timeout generoso - modelos grandes podem demorar
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddHttpClient<ILLMService, OllamaLLMService>(client =>
        {
            client.BaseAddress = new Uri(ollamaHost);
            client.Timeout = TimeSpan.FromMinutes(5);
        });
        
        //Chunker - Transient porque é stateless
        services.AddTransient<IChunkerService, ChunkerService>();

        return services;
    }
    
    //InitializeAsync executa operações assíncronas de startup que não cabem no ctor - como criar a collection do Qdrant
    public static async Task InitializeAsync(this IServiceProvider serviceProvider)
    {
        var qdrant = serviceProvider.GetRequiredService<QdrantVectorStore>();
        await qdrant.EnsureCollectionAsync();
    }
}