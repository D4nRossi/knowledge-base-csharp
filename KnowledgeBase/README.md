# knowledge-base-go — RAG Knowledge Base (V1)

Sistema de base de conhecimento com **RAG (Retrieval-Augmented Generation)** construído com .NET 8, Clean Architecture e ferramentas modernas de mercado.

> V1 — funcional, com ingestão de documentos de texto e consulta via LLM local.

---

## Stack

| Camada | Tecnologia | Função |
|---|---|---|
| API | ASP.NET Core 8 Minimal API | Entry point, endpoints REST |
| Orquestração | MediatR (CQRS) | Separação de comandos e queries |
| Storage de documentos | Apache Cassandra 4.1 | Chunks e conteúdo bruto |
| Busca vetorial | Qdrant | Embeddings e busca semântica |
| LLM + Embeddings | Ollama (local) | Geração de respostas e vetores |
| Containers | Docker Compose | Orquestração local dos serviços |

---

## Arquitetura

O projeto segue **Clean Architecture** com separação estrita de responsabilidades:

```
KnowledgeBase.Domain          → Entidades, interfaces, regras de negócio
KnowledgeBase.Application     → Casos de uso (CQRS com MediatR)
KnowledgeBase.Infrastructure  → Cassandra, Qdrant, Ollama, Chunker
KnowledgeBase.API             → Endpoints, DI, configuração
```

**Regra de dependência:** as camadas internas nunca conhecem as externas.

```
Domain ← Application ← Infrastructure
Domain ← Application ← API
```

---

## Como o RAG funciona

### Ingestão
```
Documento recebido
      ↓
ChunkerService divide em pedaços (500 chars, 50 overlap)
      ↓
OllamaEmbeddingService gera vetor pra cada chunk (nomic-embed-text)
      ↓
QdrantVectorStore indexa o vetor
      ↓
CassandraRepository salva chunk + metadados
```

### Query
```
Pergunta do usuário
      ↓
OllamaEmbeddingService gera vetor da pergunta
      ↓
QdrantVectorStore busca os N chunks mais similares (cosine similarity)
      ↓
CassandraRepository recupera o texto dos chunks
      ↓
OllamaLLMService monta prompt com contexto e gera resposta (llama3.2)
      ↓
Resposta + chunks utilizados retornados ao usuário
```

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker + Docker Compose](https://docs.docker.com/get-docker/)
- [Ollama](https://ollama.com)

---

## Setup

### 1. Clone o repositório

```bash
git clone git@github.com:D4nRossi/knowledge-base-csharp.git
cd knowledge-base-csharp/KnowledgeBase
```

### 2. Instale os modelos do Ollama

```bash
ollama pull llama3.2          # LLM principal (~2GB)
ollama pull nomic-embed-text  # Modelo de embedding (~274MB)
```

### 3. Suba os containers

```bash
docker compose -f docker-compose.yml up -d
```

Aguarde o Cassandra ficar `healthy` (pode levar ~60 segundos):

```bash
docker compose -f docker-compose.yml ps
```

### 4. Crie o keyspace no Cassandra

```bash
docker exec -it kb-cassandra cqlsh
```

```sql
CREATE KEYSPACE IF NOT EXISTS knowledge_base
WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};

exit
```

### 5. Configure as variáveis de ambiente

O `appsettings.json` já vem com valores padrão para desenvolvimento local. Se precisar sobrescrever, crie um `appsettings.Development.json` (não sobe pro git):

```json
{
  "Cassandra": {
    "Hosts": "localhost",
    "Keyspace": "knowledge_base"
  },
  "Qdrant": {
    "Host": "localhost",
    "Port": "6334",
    "CollectionName": "knowledge_chunks"
  },
  "Ollama": {
    "Host": "http://localhost:11434",
    "EmbedModel": "nomic-embed-text",
    "LLMModel": "llama3.2"
  }
}
```

### 6. Rode a aplicação

```bash
dotnet run --project KnowledgeBase.API
```

A API sobe em `http://localhost:5279`. O Swagger está disponível em `http://localhost:5279/swagger`.

---

## Endpoints

### `GET /health`
Verifica se a aplicação está rodando.

**Response:**
```json
{
  "status": "Healthy",
  "timestamp": "2026-03-01T17:00:00Z"
}
```

---

### `POST /documents`
Ingere um documento na base de conhecimento.

**Request:**
```json
{
  "title": "Fundamentos de Clean Architecture",
  "source": "clean-arch-guide.txt",
  "content": "Clean Architecture é um conjunto de princípios criado por Robert C. Martin..."
}
```

**Response `201 Created`:**
```json
{
  "id": "675c4b34-f5a3-4c6b-a832-2d7a29adc297",
  "title": "Fundamentos de Clean Architecture",
  "source": "clean-arch-guide.txt",
  "createdAt": "2026-03-01T17:11:03Z",
  "chunkCount": 3
}
```

---

### `POST /query`
Faz uma pergunta sobre os documentos ingeridos.

**Request:**
```json
{
  "question": "O que é a regra de dependência na Clean Architecture?",
  "topK": 3
}
```

**Response `200 OK`:**
```json
{
  "answer": "A regra de dependência é a ideia de que o código fonte só pode apontar para dentro, nunca para fora...",
  "sourceChunks": [
    "Clean Architecture é um conjunto de princípios criado por Robert C. Martin...",
    "A regra de dependência diz que o código fonte só pode apontar para dentro..."
  ]
}
```

O campo `sourceChunks` retorna os trechos do documento usados pra gerar a resposta — transparência total do RAG.

---

## Estrutura do Projeto

```
KnowledgeBase/
├── KnowledgeBase.API/
│   ├── Program.cs                          # Entry point, endpoints, DI
│   └── appsettings.json                    # Configuração padrão
│
├── KnowledgeBase.Application/
│   ├── DTOs/
│   │   └── DocumentDto.cs                  # Contratos de request/response
│   ├── Interfaces/
│   │   └── IChunkerService.cs              # Abstração do chunker
│   └── UseCases/
│       ├── IngestDocument/
│       │   ├── IngestDocumentCommand.cs    # CQRS Command
│       │   └── IngestDocumentHandler.cs   # Orquestração da ingestão
│       └── QueryKnowledge/
│           ├── QueryKnowledgeQuery.cs      # CQRS Query
│           └── QueryKnowledgeHandler.cs   # Orquestração da consulta
│
├── KnowledgeBase.Domain/
│   ├── Entities/
│   │   ├── Document.cs                     # Entidade raiz
│   │   └── Chunk.cs                        # Pedaço de documento
│   └── Interfaces/
│       ├── Repositories/
│       │   ├── IDocumentRepository.cs
│       │   └── IChunkRepository.cs
│       └── Services/
│           ├── IEmbeddingService.cs        # Contrato de embedding
│           ├── IVectorStore.cs             # Contrato de busca vetorial
│           └── ILLMService.cs              # Contrato do LLM
│
├── KnowledgeBase.Infrastructure/
│   ├── Cassandra/
│   │   ├── CassandraContext.cs             # Sessão + migrations
│   │   └── Repositories/
│   │       ├── DocumentRepository.cs
│   │       └── ChunkRepository.cs
│   ├── Qdrant/
│   │   └── QdrantVectorStore.cs            # Indexação e busca vetorial
│   ├── Ollama/
│   │   ├── OllamaEmbeddingService.cs       # Geração de embeddings
│   │   └── OllamaLLMService.cs             # Geração de respostas
│   ├── Chunking/
│   │   └── ChunkerService.cs               # Divisão de texto com overlap
│   └── DependencyInjection.cs              # Registro de todos os serviços
│
└── docker-compose.yml                      # Cassandra + Qdrant
```

---

## Decisões de Design

**Por que Cassandra?**
Cassandra foi escolhido pra aprender modelagem orientada a queries e conceitos de bancos distribuídos (partition key, consistência tunável, replicação). Em produção com volume real, essa escolha faz sentido pra escrita massiva de chunks.

**Por que Qdrant?**
Banco vetorial dedicado com suporte a cosine similarity e filtros de payload. O SDK oficial em .NET é bem mantido e a API gRPC é eficiente pra operações em volume.

**Por que Ollama?**
Permite rodar modelos LLM localmente sem custo e sem dependência de APIs externas. Ideal pra desenvolvimento e aprendizado. Na V2 será substituído pelo Azure AI Foundry.

**Por que CQRS com MediatR?**
Separação clara entre operações que mudam estado (Commands) e operações que leem estado (Queries). Facilita testes, evolução independente de cada caso de uso e adição de behaviors transversais como logging e validação.

---

## Roadmap

### V2 (em desenvolvimento)
- [ ] Azure AI Foundry substituindo Ollama
- [ ] Ingestão de múltiplos formatos (PDF, DOCX, Excel, CSV, imagens)
- [ ] PostgreSQL para metadados e histórico de queries
- [ ] Autenticação JWT com Azure AD
- [ ] OpenTelemetry + Prometheus + Grafana
- [ ] Rate limiting nos endpoints
- [ ] FluentValidation nos Commands e Queries
- [ ] Frontend Blazor Server

### V3 (planejado)
- [ ] Deploy em GCP com VM Linux
- [ ] Nginx como reverse proxy
- [ ] SSL via Let's Encrypt
- [ ] RAG multimodal (busca semântica em imagens)
- [ ] Reranking dos chunks antes de montar o prompt
- [ ] Kubernetes para orquestração
