using FiscalDocuments.Api.Data;
using FiscalDocuments.Api.DTOs;
using FiscalDocuments.Api.Interfaces;
using FiscalDocuments.Api.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FiscalDocuments.Tests.Services;

public class FiscalDocumentServiceTests
{
    [Test]
    public void CreateAsync_XmlContentVazio_DeveLancarArgumentException()
    {
        // Arrange - prepara o cenário necessário para executar o teste.

        // Usando banco em memória para não depender nem alterar o PostgreSQL real.
        var options = new DbContextOptionsBuilder<FiscalDocumentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new FiscalDocumentsDbContext(options);

        // Simulando o publicador do RabbitMQ para não depender da fila
        var rabbitMqPublisherMock = new Mock<IRabbitMqPublisher>();

        // Cria o service usando somente dependências controladas pelo teste.
        var service = new FiscalDocumentService(
            dbContext,
            rabbitMqPublisherMock.Object
        );

        //Criando o XML vazio para validar regra de conteúdo obrigatório.
        var dto = new CreateFiscalDocumentDto
        {
            XmlContent = ""
        };

        //executando o método que está sendo testado.
        var exception = Assert.ThrowsAsync<ArgumentException>(
            async () => await service.CreateAsync(dto)
        );

        //Verificando se retornou o erro esperado.
        Assert.That(
            exception!.Message,
            Is.EqualTo("O conteúdo XML é obrigatório.")
        );
    }
}