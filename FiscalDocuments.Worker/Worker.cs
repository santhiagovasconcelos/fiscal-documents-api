using System.Text;
using System.Text.Json;
using FiscalDocuments.Api.Data;
using FiscalDocuments.Api.Messaging;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FiscalDocuments.Worker;

public class Worker : BackgroundService
{
    private const int MaxRetryAttempts = 3;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Worker> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public Worker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var host = _configuration["RabbitMq:Host"]
            ?? throw new InvalidOperationException(
                "RabbitMq:Host não configurado."
            );

        var port = int.Parse(
            _configuration["RabbitMq:Port"] ?? "5672"
        );

        var user = _configuration["RabbitMq:User"]
            ?? throw new InvalidOperationException(
                "RabbitMq:User não configurado."
            );

        var password = _configuration["RabbitMq:Password"]
            ?? throw new InvalidOperationException(
                "RabbitMq:Password não configurado."
            );

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = user,
            Password = password
        };

        _connection = await factory.CreateConnectionAsync(
            stoppingToken
        );

        _channel = await _connection.CreateChannelAsync(
            cancellationToken: stoppingToken
        );

        await _channel.QueueDeclareAsync(
            queue: "fiscal-document-processing",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        //limitando o Worker a uma mensagem não confirmada por vez
        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();

                var json = Encoding.UTF8.GetString(body);

                var message =
                    JsonSerializer.Deserialize<FiscalDocumentMessage>(
                        json
                    );

                if (message is null)
                {
                    throw new InvalidOperationException(
                        "Mensagem inválida."
                    );
                }

                await ProcessWithRetryAsync(
                    message,
                    stoppingToken
                );

                await _channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken
                );

                _logger.LogInformation(
                    "Mensagem confirmada com ACK. Documento: {DocumentId}",
                    message.DocumentId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha definitiva no processamento da mensagem."
                );

                await _channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken
                );
            }
        };

        await _channel.BasicConsumeAsync(
            queue: "fiscal-document-processing",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        await Task.Delay(
            Timeout.Infinite,
            stoppingToken
        );
    }

    private async Task ProcessWithRetryAsync(
        FiscalDocumentMessage message,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1;
            attempt <= MaxRetryAttempts;
            attempt++)
        {
            try
            {
                await ProcessMessageAsync(
                    message,
                    cancellationToken
                );

                return;
            }
            catch (Exception ex)
            {
                if (attempt == MaxRetryAttempts)
                {
                    _logger.LogError(
                        ex,
                        "Documento {DocumentId} falhou após {Attempts} tentativas.",
                        message.DocumentId,
                        MaxRetryAttempts
                    );

                    throw;
                }

                var delaySeconds =
                    (int)Math.Pow(2, attempt);

                _logger.LogWarning(
                    ex,
                    "Erro ao processar documento {DocumentId}. " +
                    "Tentativa {Attempt}/{MaxAttempts}. " +
                    "Nova tentativa em {DelaySeconds}s.",
                    message.DocumentId,
                    attempt,
                    MaxRetryAttempts,
                    delaySeconds
                );

                await Task.Delay(
                    TimeSpan.FromSeconds(delaySeconds),
                    cancellationToken
                );
            }
        }
    }

    private async Task ProcessMessageAsync(
        FiscalDocumentMessage message,
        CancellationToken cancellationToken)
    {
        using var scope =
            _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<FiscalDocumentsDbContext>();

        var document =
            await dbContext.FiscalDocuments
                .FirstOrDefaultAsync(
                    x => x.Id == message.DocumentId,
                    cancellationToken
                );

        if (document is null)
        {
            throw new InvalidOperationException(
                $"Documento {message.DocumentId} não encontrado."
            );
        }

        _logger.LogInformation(
            "Documento processado: {DocumentId} - {DocumentType}",
            document.Id,
            document.DocumentType
        );
    }
}
