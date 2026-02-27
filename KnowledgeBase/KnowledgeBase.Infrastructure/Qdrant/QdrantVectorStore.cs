//QdrantVectorStore implementa IVectorStore usando o Qdrant como banco vetorial
//O Qdrant organiza vetores em "collections" - equivalente a tabelas, mas otimizado pra busca por similaridade via algoritimo HNSW

using KnowledgeBase.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace KnowledgeBase.Infrastructure.Qdrant;

public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _client;
    private readonly ILogger<QdrantVectorStore> _logger;
    private readonly string _collectionName;

    //Dimensão do vetor gerada pelo nomic-embed-text - deve coincidir com o modelo
    //Se trocar de modelo de embedding, essa constante muda junto
    private const ulong VectorSize = 768;

    public QdrantVectorStore(IConfiguration configuration, ILogger<QdrantVectorStore> logger)
    {
        _logger = logger;
        _collectionName = configuration["Qdrant:CollectionName"] ?? "knowledge_chunk";

        var host = configuration["Qdrant:Host"] ?? "localhost";
        var port = int.Parse(configuration["Qdrant:Port"] ?? "6334");

        //Porta 6334 é gRPC - mais eficiente que REST pra operações em volume
        _client = new QdrantClient(host, port);
    }

    //EnsureCollectionAsync cria a collection se não existir - idempotente
    //Chamado no startup via DependencyInjection
    public async Task EnsureCollectionAsync()
    {
        var collections = await _client.ListCollectionsAsync();

        //ListCollectionsAsync não retorna string diretamente; retorna objetos com Name
        if (collections.Any(c => c == _collectionName))
        {
            _logger.LogInformation("Qdrant collection already exists: {Collection}", _collectionName);
            return;
        }

        //CreateCollectionAsync no SDK aceita VectorParams diretamente (em vez de VectorsConfig)
        await _client.CreateCollectionAsync(
            _collectionName,
            new VectorParams
            {
                Size = VectorSize,
                // Cosine mede ângulo entre vetores — melhor pra similaridade semântica
                Distance = Distance.Cosine
            }
        );

        _logger.LogInformation("Qdrant collection created: {Collection}", _collectionName);
    }

    public async Task<string> IndexAsync(Guid chunkId, float[] embedding, CancellationToken cancellationToken = default)
    {
        //Qdrant usa UUID como ID do ponto - usamos o chunkId diretamente
        var pointId = new PointId { Uuid = chunkId.ToString() };

        await _client.UpsertAsync(_collectionName, new[]
        {
            new PointStruct
            {
                Id = pointId,
                Vectors = embedding,
                //Payload armazena metadados junto ao vetor - útil pra filtros
                Payload = { ["chunk_id"] = chunkId.ToString() }
            }
        }, cancellationToken: cancellationToken);

        _logger.LogDebug("Indexed vector for chunk: {ChunkId}", chunkId);
        return chunkId.ToString();
    }

    public async Task<IEnumerable<Guid>> SearchAsync(float[] queryEmbedding, int topK = 5, CancellationToken cancellationToken = default)
    {
        var results = await _client.SearchAsync(
            _collectionName,
            queryEmbedding,
            limit: (ulong)topK,
            cancellationToken: cancellationToken
        );

        //Resultado pode vir com PointId "oneof" (numérico ou uuid); aqui extraímos UUID
        return results
            .Select(r => r.Id?.Uuid)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => Guid.TryParse(u, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value);
    }
}