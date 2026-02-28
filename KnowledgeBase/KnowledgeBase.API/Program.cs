// Program.cs é o entrypoint da aplicação.
// Responsabilidade única: configurar o pipeline e iniciar o servidor.
// Toda lógica de negócio fica nas camadas internas.
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.UseCases.IngestDocument;
using KnowledgeBase.Application.UseCases.QueryKnowledge;
using KnowledgeBase.Infrastructure;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// Registra MediatR — descobre automaticamente todos os Handlers do Assembly
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(IngestDocumentHandler).Assembly));

// Registra toda a Infrastructure com uma única chamada
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Inicializa recursos assíncronos de startup — cria collection do Qdrant
await app.Services.InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// -----------------------------------------------------------------------
// Endpoints — Minimal API
// Cada endpoint delega pro MediatR que roteia pro Handler correto.
// A API não tem lógica de negócio — só recebe, delega e responde.
// -----------------------------------------------------------------------

// POST /documents — ingere um novo documento no sistema
app.MapPost("/documents", async (IngestDocumentRequest request, IMediator mediator) =>
{
    var command = new IngestDocumentCommand(request.Title, request.Source, request.Content);
    var result = await mediator.Send(command);
    return Results.Created($"/documents/{result.Id}", result);
})
.WithName("IngestDocument")
.WithSummary("Ingest a new document into the knowledge base");

// POST /query — faz uma pergunta sobre os documentos ingeridos
app.MapPost("/query", async (QueryRequest request, IMediator mediator) =>
{
    var query = new QueryKnowledgeQuery(request.Question, request.TopK);
    var result = await mediator.Send(query);
    return Results.Ok(result);
})
.WithName("QueryKnowledge")
.WithSummary("Query the knowledge base with a question");

// GET /health — endpoint de health check pra monitoramento
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
.WithName("HealthCheck");

app.Run();