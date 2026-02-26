// Document representa um arquivo de conhecimento inserido pelo usuário.
// É a entidade core do domínio - tudo gira em torno dela

using System.Data;

namespace KnowledgeBase.Domain.Entities;

public class Document
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }

    // Origem do documento - caminho de arquivo, URL, etc.
    public string Source { get; private set; }

    // Conteúdo completo antes do chunking
    public string Content { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Chunks são gerados a partir deste documento durante a ingestão
    public IReadOnlyCollection<Chunk> Chunks => _chunks.AsReadOnly();
    private readonly List<Chunk> _chunks = new();
    
    // Construtor privado - Document só pode ser criado via factory method
    // Isso garante que nunca existe um Document inválido no sistema
    private Document() {}
    
    // Create é o único ponto de entrada para cria um Document
    // Centraliza validações e garante invariantes do domínio
    public static Document Create(string title, string source, string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(title, nameof(title));
        ArgumentException.ThrowIfNullOrEmpty(source, nameof(source));
        ArgumentException.ThrowIfNullOrEmpty(content, nameof(content));

        return new Document
        {
            Id = Guid.NewGuid(),
            Title = title,
            Source = source,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    // AddChunk é controlado pela entidade - ninguém adiciona chunk diratamente na lista
    // Isso é encapsulamento real: o Document conhece suas próprias regras
    public void AddChunk(Chunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk, nameof(chunk));
        _chunks.Add(chunk);
    }
}