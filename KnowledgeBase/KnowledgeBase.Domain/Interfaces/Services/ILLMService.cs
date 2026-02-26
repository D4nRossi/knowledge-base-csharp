//ILLMService abstrai qualquer modelo de linguagem - Ollama, OpenAI, Azure OpenAI, etc.

namespace KnowledgeBase.Domain.Interfaces.Services;

public interface ILLMService
{
    //Recebe a pergunta e os chunks de contexto, retorna a resposta gerada
    Task<string> GenerateAnswerAsync(string question, IEnumerable<string> contextChunks, CancellationToken cancellationToken = default);
}