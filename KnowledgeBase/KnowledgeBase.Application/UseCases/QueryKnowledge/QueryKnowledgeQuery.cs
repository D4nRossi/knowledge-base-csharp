using KnowledgeBase.Application.DTOs;
using MediatR;

namespace KnowledgeBase.Application.UseCases.QueryKnowledge;

public record QueryKnowledgeQuery(
    string Question,
    int TopK = 5
) : IRequest<QueryResponse>;