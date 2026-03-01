//CassandraContext gerencia o ciclo de vida da sessão com o Cassandra
//É registrado como Singleton no DI - uma sessão por aplicação é o padrão recomendado pelo driver porque sessões são thread-safe e custosas pra criar.

using Cassandra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KnowledgeBase.Infrastructure.Cassandra;

public class CassandraContext : IDisposable
{
    private readonly ISession _session;
    private readonly ILogger _logger;

    public ISession Session => _session;

    public CassandraContext(IConfiguration configuration, ILogger<CassandraContext> logger)
    {
        _logger = logger;

        var hosts = configuration["Cassandra:Hosts"]?.Split(',') ?? ["localhost"];
        var keyspace = configuration["Cassandra:Keyspace"] ?? "knowledge_base";
        var username = configuration["Cassandra:Username"];
        var password = configuration["Cassandra:Password"];

        var builder = Cluster.Builder()
            .AddContactPoints(hosts)
            .WithDefaultKeyspace(keyspace)
            //QueryOptions define consistência padrão pra todas as queries
            .WithQueryOptions(new QueryOptions().SetConsistencyLevel(ConsistencyLevel.LocalQuorum));

        //Autenticação só se credenciais estiverem configuradas
        if (!string.IsNullOrWhiteSpace(username))
            builder.WithCredentials(username, password);

        var cluster = builder.Build();
        _session = cluster.ConnectAndCreateDefaultKeyspaceIfNotExists();

        _logger.LogInformation("Cassandra connected. Hosts: {Hosts}, Keyspace: {Keyspace}", string.Join(",", hosts),
            keyspace);

        Migrate();
    }

    //Migrate cria as tabelas se não existirem - idempotente
    private void Migrate()
    {
        _session.Execute(@"
            CREATE TABLE IF NOT EXISTS documents(
                id UUID PRIMARY KEY,
                title TEXT,
                source TEXT,
                content TEXT,
                created_at TIMESTAMP
            )
        ");

        _session.Execute(@"
            CREATE TABLE IF NOT EXISTS chunks(
                document_id UUID,
                chunk_index INT,
                id UUID,
                content TEXT,
                vector_id TEXT,
                created_at TIMESTAMP,
                PRIMARY KEY (document_id, chunk_index)
            )
        ");
        
        _logger.LogInformation("Cassandra migration completed");
    }
    
    public void Dispose() => _session?.Dispose();
}