using Cassandra;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace KnowledgeBase.Infrastructure.Cassandra.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly ISession _session;
    private readonly ILogger<DocumentRepository> _logger;

    public DocumentRepository(CassandraContext context, ILogger<DocumentRepository> logger)
    {
        _session = context.Session;
        _logger = logger;
    }
    
    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var statement = await _session.PrepareAsync("SELECT id, title, source, content, created_at FROM documents WHERE id = ?");
        var result = await _session.ExecuteAsync(statement.Bind(id));
        var row = result.FirstOrDefault();

        if (row == null) return null;
        
        //Reconstroi a entidade a partir dos dados do banco via reflection
        //Como o ctor é privado (proteção do Domain), usamos o pattern de recriar via método estático interno ou reflection controlada
        return MapToDocument(row);
    }

    public async Task<IEnumerable<Document>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await _session.ExecuteAsync(
            new SimpleStatement("SELECT id, title, source, content, created_at FROM documents"));
        
        return result.Select(MapToDocument);
    }

    public async Task SaveAsync(Document document, CancellationToken cancellationToken = default)
    {
        //PreparedStatement é compilado uma vez no Cassandra e reutilizado -  mais performático e proteje contra CQL injection
        var statement = await _session.PrepareAsync(
            "INSERT INTO documents (id, title, source, content, created_at) VALUES (?,?,?,?,?)");
            
            var bound = statement.Bind(
                document.Id,
                document.Title,
                document.Source,
                document.Content,
                document.CreatedAt
            );
                
            await _session.ExecuteAsync(bound);
            _logger.LogDebug($"Document {document.Id} saved"); 
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var statement = await _session.PrepareAsync("DELETE FROM documents WHERE id = ?");
        
        await _session.ExecuteAsync(statement.Bind(id));
        _logger.LogDebug("Document deleted: {DocumentId}", id);
    }
    
    //MapToDocument reconstrói a entidade a partir de uma Row do Cassandra
    //Centraliza o mapeamento -  se a tabela mudar, só aqui muda
    private static Document MapToDocument(Row row)
    {
        return Document.Reconstitute(
            row.GetValue<Guid>("id"),
            row.GetValue<string>("title"),
            row.GetValue<string>("source"),
            row.GetValue<string>("content"),
            row.GetValue<DateTimeOffset>("created_at").UtcDateTime
        );
    }
}