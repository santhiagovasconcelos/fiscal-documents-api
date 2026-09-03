using FiscalDocuments.Api.Data;
using FiscalDocuments.Api.DTOs;
using FiscalDocuments.Api.Interfaces;
using FiscalDocuments.Api.Services;
using FiscalDocuments.Api.Models;
using FiscalDocuments.Api.Messaging;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FiscalDocuments.Tests.Services;

public class FiscalDocumentServiceTests
{
    [Test]
    public void CreateAsync_XmlContentVazio_DeveLancarArgumentException()
    {
        // Preparando o cenário necessário para executar o teste.

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

    [Test]
    public void CreateAsync_XmlInvalido_DeveLancarArgumentException()
    {
        // Prepara um banco isolado somente para este teste.
        var options = new DbContextOptionsBuilder<FiscalDocumentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new FiscalDocumentsDbContext(options);

        // Simula o RabbitMQ para não precisar de uma conexão real.
        var rabbitMqPublisherMock = new Mock<IRabbitMqPublisher>();

        var service = new FiscalDocumentService(
            dbContext,
            rabbitMqPublisherMock.Object
        );

        // Conteúdo propositalmente inválido para testar a validação do XML.
        var dto = new CreateFiscalDocumentDto
        {
            XmlContent = "<xml-invalido"
        };

        // Act - tenta criar o documento com um XML malformado.
        var exception = Assert.ThrowsAsync<ArgumentException>(
            async () => await service.CreateAsync(dto)
        );

        // Confirma que foi retornado o erro esperado.
        Assert.That(
            exception!.Message,
            Is.EqualTo("O conteúdo informado não é um XML válido.")
        );
    }

    [Test]
    public void CreateAsync_TipoDocumentoNaoSuportado_DeveLancarArgumentException()
    {
        // Criando um banco isolado para este teste.
        var options = new DbContextOptionsBuilder<FiscalDocumentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new FiscalDocumentsDbContext(options);

        // Simula o publicador para não acessar o RabbitMQ real.
        var rabbitMqPublisherMock = new Mock<IRabbitMqPublisher>();

        var service = new FiscalDocumentService(
            dbContext,
            rabbitMqPublisherMock.Object
        );

        // XML válido, porém sem estrutura de NFe, CTe ou NFSe.
        var dto = new CreateFiscalDocumentDto
        {
            XmlContent = """
                <Documento>
                    <Teste>123</Teste>
                </Documento>
                """
        };

        // Act - tenta criar um documento fiscal com tipo não suportado.
        var exception = Assert.ThrowsAsync<ArgumentException>(
            async () => await service.CreateAsync(dto)
        );

        // Assert - confirma que a validação do tipo foi executada.
        Assert.That(
            exception!.Message,
            Is.EqualTo("Tipo de documento fiscal não suportado.")
        );
    }

    [Test]
    public async Task CreateAsync_NFeValida_DeveCriarDocumentoCorretamente()
    {
        // cria um banco isolado para este teste.
        var options = new DbContextOptionsBuilder<FiscalDocumentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new FiscalDocumentsDbContext(options);

        // Simula o RabbitMQ para não publicar em uma fila real.
        var rabbitMqPublisherMock = new Mock<IRabbitMqPublisher>();

        var service = new FiscalDocumentService(
            dbContext,
            rabbitMqPublisherMock.Object
        );

        var dto = new CreateFiscalDocumentDto
        {
            XmlContent = """
                <nfeProc>
                    <NFe>
                        <infNFe Id="NFe12345678901234567890123456789012345678901234">
                            <ide>
                                <dhEmi>2026-09-02T10:30:00-03:00</dhEmi>
                            </ide>

                            <emit>
                                <CNPJ>12345678000199</CNPJ>
                            </emit>

                            <dest>
                                <CNPJ>98765432000188</CNPJ>
                            </dest>
                        </infNFe>
                    </NFe>
                </nfeProc>
                """
        };

        //Criando o documento a partir da NFe informada.
        var result = await service.CreateAsync(dto);

        // Assert - valida os principais dados extraídos do XML.
        Assert.That(result, Is.Not.Null);

        Assert.That(
            result.DocumentType,
            Is.EqualTo(FiscalDocumentType.NFe)
        );

        Assert.That(
            result.AccessKey,
            Is.EqualTo("12345678901234567890123456789012345678901234")
        );

        Assert.That(
            result.IssuerCnpj,
            Is.EqualTo("12345678000199")
        );

        Assert.That(
            result.RecipientCnpj,
            Is.EqualTo("98765432000188")
        );

        // Confirma que o documento também foi persistido no banco em memória.
        Assert.That(
            dbContext.FiscalDocuments.Count(),
            Is.EqualTo(1)
        );

        // Confirma que a mensagem foi enviada para o publicador simulado.
        rabbitMqPublisherMock.Verify(
            x => x.PublishAsync(
                "fiscal-document-processing",
                It.IsAny<FiscalDocumentMessage>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task CreateAsync_XmlDuplicado_DeveLancarInvalidOperationException()
    {
        // Arrange - cria um banco isolado para este teste.
        var options = new DbContextOptionsBuilder<FiscalDocumentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new FiscalDocumentsDbContext(options);

        var rabbitMqPublisherMock = new Mock<IRabbitMqPublisher>();

        var service = new FiscalDocumentService(
            dbContext,
            rabbitMqPublisherMock.Object
        );

        var dto = new CreateFiscalDocumentDto
        {
            XmlContent = """
                <nfeProc>
                    <NFe>
                        <infNFe Id="NFe12345678901234567890123456789012345678901234">
                            <ide>
                                <dhEmi>2026-09-02T10:30:00-03:00</dhEmi>
                            </ide>

                            <emit>
                                <CNPJ>12345678000199</CNPJ>
                            </emit>

                            <dest>
                                <CNPJ>98765432000188</CNPJ>
                            </dest>
                        </infNFe>
                    </NFe>
                </nfeProc>
                """
        };

        // Salva o documento pela primeira vez.
        await service.CreateAsync(dto);

        // Act - tenta processar exatamente o mesmo XML novamente.
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.CreateAsync(dto)
        );

        //Confirmando que o hash do XML impediu o processamento duplicado.
        Assert.That(
            exception!.Message,
            Is.EqualTo("Este XML já foi processado.")
        );

        Assert.That(
            dbContext.FiscalDocuments.Count(),
            Is.EqualTo(1)
        );
    }

    [Test]
    public async Task Delete_DocumentoExistente_DeveRealizarSoftDelete()
    {
        // Arrange - cria um banco isolado para este teste.
        var options = new DbContextOptionsBuilder<FiscalDocumentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new FiscalDocumentsDbContext(options);

        var rabbitMqPublisherMock = new Mock<IRabbitMqPublisher>();

        var service = new FiscalDocumentService(
            dbContext,
            rabbitMqPublisherMock.Object
        );

        var dto = new CreateFiscalDocumentDto
        {
            XmlContent = """
                <nfeProc>
                    <NFe>
                        <infNFe Id="NFe98765432109876543210987654321098765432109876">
                            <ide>
                                <dhEmi>2026-09-02T10:30:00-03:00</dhEmi>
                            </ide>

                            <emit>
                                <CNPJ>12345678000199</CNPJ>
                            </emit>

                            <dest>
                                <CNPJ>98765432000188</CNPJ>
                            </dest>
                        </infNFe>
                    </NFe>
                </nfeProc>
                """
        };

        var createdDocument = await service.CreateAsync(dto);

        // Executa a exclusão lógica.
        var result = service.Delete(createdDocument.Id);

        // Assert - confirma que a operação foi realizada.
        Assert.That(result, Is.True);

        // Confirma que o registro continua fisicamente no banco.
        var documentInDatabase = dbContext.FiscalDocuments
            .FirstOrDefault(x => x.Id == createdDocument.Id);

        Assert.That(documentInDatabase, Is.Not.Null);

        // Confirma que apenas o campo Active foi desativado.
        Assert.That(documentInDatabase!.Active, Is.False);

        // Como GetById busca somente documentos ativos,
        // o documento excluído não deve mais ser retornado pela aplicação.
        var documentReturnedByService = service.GetById(createdDocument.Id);

        Assert.That(documentReturnedByService, Is.Null);
    }

}