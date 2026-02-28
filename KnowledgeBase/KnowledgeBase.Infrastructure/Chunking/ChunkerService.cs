//ChunkerService implementa a estratégia de chunking por tamanho fixo com overlap
//Overlap evita perder contexto nas bordas dos chunks - imagine um conceito que começa no final de um chunk e termina no início do próximo
//Com overlap, ambos os chunks contêm parte desse contexto.

using KnowledgeBase.Application.Interfaces;

namespace KnowledgeBase.Infrastructure.Chunking;

public class ChunkerService : IChunkerService
{
    public IEnumerable<string> Chunk(string text, int chunkSize = 500, int overlap = 50)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));

        if(chunkSize <= 0) throw new ArgumentException("Chunk size must be positive", nameof(chunkSize));
        if(overlap < 0) throw new ArgumentException("Overlap must be non-negative", nameof(overlap));
        if (overlap >= chunkSize) throw new ArgumentException("Overlao must be smaller than chunk size", nameof(overlap));

        var chunks = new List<string>();
        var step = chunkSize -  overlap;
        var position = 0;

        while (position < text.Length)
        {
            var length = Math.Min(chunkSize, text.Length - position);
            var chunk = text.Substring(position, length).Trim();
            
            if(!string.IsNullOrEmpty(chunk))
                chunks.Add(chunk);

            position += step;
        }

        
        return chunks;
    }
}