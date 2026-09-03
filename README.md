# Fiscal Documents API

API REST desenvolvida em ASP.NET Core para ingestão, armazenamento e gerenciamento de documentos fiscais eletrônicos em formato XML.

O projeto permite cadastrar documentos fiscais, consultar os registros armazenados, utilizar filtros e paginação, atualizar e excluir documentos. Também foi implementado processamento assíncrono utilizando RabbitMQ e um Worker separado da API.

## Tecnologias utilizadas

- C# / .NET
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- RabbitMQ
- Docker / Docker Compose
- NUnit
- OpenAPI

## Estrutura do projeto

A solução está dividida em três projetos principais:

```text
FiscalDocuments.Api
FiscalDocuments.Worker
FiscalDocuments.Tests
```

**FiscalDocuments.Api**  
Responsável pelos endpoints REST, regras da aplicação, persistência dos documentos e publicação das mensagens no RabbitMQ.

**FiscalDocuments.Worker**  
Responsável pelo consumo e processamento das mensagens publicadas pela API.

**FiscalDocuments.Tests**  
Projeto com os testes automatizados da aplicação utilizando NUnit.

## Funcionalidades

A API possui:

- Cadastro de documentos fiscais através de XML
- Suporte a NF-e, CT-e e NFS-e
- Extração de informações do XML
- Armazenamento do XML original
- Consulta por ID
- Listagem com paginação
- Filtro por tipo de documento
- Filtro por CNPJ
- Atualização do documento
- Exclusão lógica (soft delete)
- Controle de duplicidade
- Processamento assíncrono com RabbitMQ
- Testes automatizados

## Endpoints

Rota base:

```text
/api/fiscal-documents
```

| Método | Endpoint                     | Descrição                     |
| ------ | ---------------------------- | ----------------------------- |
| POST   | `/api/fiscal-documents`      | Cadastra um documento fiscal  |
| GET    | `/api/fiscal-documents`      | Lista os documentos           |
| GET    | `/api/fiscal-documents/{id}` | Consulta um documento pelo ID |
| PUT    | `/api/fiscal-documents/{id}` | Atualiza um documento         |
| DELETE | `/api/fiscal-documents/{id}` | Realiza a exclusão lógica     |

A listagem aceita paginação e filtros:

```text
?page=1&pageSize=10&documentType=NFe&cnpj=12345678000100
```

## Exemplos de uso

Com a API disponível em `http://localhost:8080`, os endpoints podem ser testados com requisições HTTP como nos exemplos abaixo.

### Cadastrar documento

```http
POST http://localhost:8080/api/fiscal-documents
Content-Type: application/json

{
  "xmlContent": "<NFe><infNFe Id=\"NFe35260912345678000199550010000009991000009999\"><ide><dhEmi>2026-09-03T01:35:00-03:00</dhEmi></ide><emit><CNPJ>12345678000199</CNPJ></emit><dest><CNPJ>98765432000188</CNPJ></dest></infNFe></NFe>"
}
```

Exemplo de resposta (campos principais):

```json
{
  "id": "7d7a3e54-18b5-4c82-9db5-40e58f10a741",
  "accessKey": "35260912345678000199550010000009991000009999",
  "documentType": "NFe",
  "issuerCnpj": "12345678000199",
  "recipientCnpj": "98765432000188",
  "active": true
}
```

### Listar documentos

```http
GET http://localhost:8080/api/fiscal-documents
```

Com paginação e filtros:

```http
GET http://localhost:8080/api/fiscal-documents?page=1&pageSize=10&documentType=NFe&cnpj=12345678000199
```

### Consultar documento por ID

```http
GET http://localhost:8080/api/fiscal-documents/{id}
```

### Atualizar documento

```http
PUT http://localhost:8080/api/fiscal-documents/{id}
Content-Type: application/json

{
  "xmlContent": "<NFe>...</NFe>"
}
```

### Excluir documento

```http
DELETE http://localhost:8080/api/fiscal-documents/{id}
```

A exclusão é lógica (soft delete). O registro permanece armazenado no banco com `Active = false`, mas deixa de ser retornado nas consultas normais.

## Idempotência

Para evitar o cadastro repetido do mesmo documento, é calculado um hash SHA-256 a partir do XML recebido.

O hash é armazenado junto ao documento e utilizado na verificação de duplicidade. Também existe uma restrição de unicidade no banco de dados, adicionando uma segunda proteção contra registros duplicados.

## RabbitMQ e Worker

Após o documento ser validado e persistido, a API publica uma mensagem no RabbitMQ.

O Worker consome as mensagens da fila:

```text
fiscal-document-processing
```

O consumo utiliza ACK manual, portanto a mensagem só é confirmada após o processamento.

Também foi implementada uma política simples de retry com até 3 tentativas e backoff exponencial. Caso o processamento continue falhando, a mensagem recebe NACK sem requeue, evitando tentativas infinitas.

Fluxo simplificado:

```text
Cliente
   |
   v
API
   |
   +----> PostgreSQL
   |
   v
RabbitMQ
   |
   v
Worker
```

## Executando com Docker

A maneira mais simples de executar o projeto é utilizando Docker Compose.

O ambiente sobe:

- PostgreSQL
- RabbitMQ
- API
- Worker

### Pré-requisitos

Para executar o projeto com Docker é necessário ter o Docker Desktop instalado e em execução.

Também é necessário que as portas utilizadas pelo projeto estejam disponíveis:

| Serviço             | Porta |
| ------------------- | ----: |
| API                 |  8080 |
| PostgreSQL          |  5433 |
| RabbitMQ            |  5672 |
| RabbitMQ Management | 15672 |

> O PostgreSQL utiliza a porta `5432` internamente no Docker e é exposto na porta `5433` do host.

### 1. Clonar o repositório

```bash
git clone https://github.com/santhiagovasconcelos/fiscal-documents-api.git
cd fiscal-documents-api
```

### 2. Configurar o ambiente

Na raiz do projeto existe o arquivo:

```text
.env.example
```

Crie uma cópia chamada `.env`:

```text
.env.example -> .env
```

Exemplo:

```env
POSTGRES_DB=fiscal_documents
POSTGRES_USER=postgres
POSTGRES_PASSWORD=change-me
POSTGRES_PORT=5433

RABBITMQ_USER=admin
RABBITMQ_PASSWORD=change_me
RABBITMQ_PORT=5672
RABBITMQ_MANAGEMENT_PORT=15672
```

O arquivo `.env` não deve ser versionado.

### 3. Subir os containers

```bash
docker compose up --build
```

Ou em segundo plano:

```bash
docker compose up -d --build
```

A API ficará disponível em:

```text
http://localhost:8080
```

O painel de gerenciamento do RabbitMQ ficará disponível em:

```text
http://localhost:15672
```

Para verificar os containers:

```bash
docker compose ps
```

Para acompanhar os logs:

```bash
docker compose logs api
docker compose logs worker
```

Para encerrar:

```bash
docker compose down
```

Os dados do PostgreSQL e RabbitMQ são mantidos em volumes Docker.

Para remover também os volumes:

```bash
docker compose down -v
```

## Banco de dados e Migrations

O projeto utiliza PostgreSQL com Entity Framework Core.

A criação e evolução da estrutura do banco são controladas através de EF Core Migrations. Na inicialização da API, as migrations pendentes são aplicadas automaticamente.

Também foi criado um seed com documentos de exemplo para facilitar a execução e avaliação do projeto.

## Executando sem Docker

Também é possível executar a API diretamente pelo .NET.

Nesse caso é necessário possuir PostgreSQL e RabbitMQ disponíveis localmente.

Para evitar credenciais no `appsettings.json`, a connection string local pode ser configurada através do .NET User Secrets:

```bash
cd FiscalDocuments.Api

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=fiscal_documents;Username=postgres;Password=SUA_SENHA"
```

Depois:

```bash
dotnet run
```

O uso de User Secrets mantém a senha do banco fora do repositório.

## Testes

Os testes automatizados foram implementados utilizando NUnit.

Para executar:

```bash
dotnet test
```

Foram adicionados testes para regras importantes do serviço, incluindo criação de documentos, validações, duplicidade, atualização, exclusão e consultas.

## Algumas decisões do projeto

### XML original

Além dos dados extraídos para consulta, o XML original é armazenado no banco.

A ideia é permitir trabalhar com os campos principais de forma estruturada sem perder o documento original recebido.

### Soft delete

A exclusão não remove fisicamente o documento do banco. O registro é marcado como inativo, preservando seu histórico.

### Worker separado

O processamento das mensagens foi colocado em um Worker para não deixar esse processamento preso ao ciclo da requisição HTTP.

Neste projeto, o `FiscalDocuments.Worker` referencia diretamente o projeto `FiscalDocuments.Api`.

Essa foi uma simplificação adotada considerando o tamanho e o prazo do projeto.

Em uma aplicação maior, seria melhor separar contratos, mensagens e componentes compartilhados em projetos próprios, por exemplo:

```text
FiscalDocuments.Api
FiscalDocuments.Worker
FiscalDocuments.Contracts
FiscalDocuments.Infrastructure
```

Assim o Worker não precisaria depender diretamente da API.

## Segurança das configurações

As credenciais não ficam versionadas no repositório.

Para o ambiente Docker é utilizado um arquivo `.env`, baseado no `.env.example`.

Para execução local, pode ser utilizado o .NET User Secrets.

O XML também não precisa ser retornado nas consultas comuns, evitando trafegar todo o conteúdo do documento quando ele não é necessário.

## Possíveis melhorias

Algumas melhorias que poderiam ser feitas em uma evolução do projeto:

- autenticação e autorização;
- maior cobertura de testes;
- CI/CD;
- separação dos contratos compartilhados entre API e Worker;
- validações mais completas dos layouts dos documentos fiscais.

## Autor

Santhiago Vasconcelos
