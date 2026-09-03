namespace FiscalDocuments.Api.Interfaces;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(string queueName, T message);
}