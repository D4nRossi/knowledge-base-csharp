//Command representa uma intenção de mudança de estado - padrão CQRS
//Commands mudam dados, Queries leem dados. Nunca os dois ao mesmo tempo

using MediatR;
using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.UseCases.IngestDocument;

//IRequest<DocumentDto> significa que este command retorna um DocumentDto quando executado
public record IngestDocumentCommand
(
    string Title,
    string Source,
    string Content
) : IRequest<DocumentDto>;