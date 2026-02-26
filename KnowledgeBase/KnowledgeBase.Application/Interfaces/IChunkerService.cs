//IChunkService abstrai a estrtégia de chunking
//Existem várias estratégias: tamanho fixo, por parágrafo, por sentença, semântico
//A interface permite trocar a estratégia sem mudar os use cases

namespace KnowledgeBase.Application.Interfaces;

public interface IChunkerService
{
    //Divide o texto em chunks com overlap opcional
    //Overlap = sopreposição entre chunks consecutivos pra não perder nas bordas
    IEnumerable<string> Chunk(string text, int chunkSize = 500, int overlap = 50);
}