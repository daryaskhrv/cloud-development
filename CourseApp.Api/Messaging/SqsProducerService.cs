using System.Net;
using System.Text.Json;
using Amazon.SQS;
using CourseApp.Domain.Entity;

namespace CourseApp.Api.Messaging;

/// <summary>
/// Служба для отправки сообщений в SQS
/// </summary>
/// <param name="client">Клиент SQS</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public class SqsProducerService(IAmazonSQS client, IConfiguration configuration, ILogger<SqsProducerService> logger) : IProducerService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("SQS queue name was not found in configuration");

    /// <inheritdoc/>
    public async Task SendMessage(Course course)
    {
        try
        {
            var json = JsonSerializer.Serialize(course, _jsonOptions);
            var response = await client.SendMessageAsync(_queueName, json);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                logger.LogInformation("Курс {id} отправлен в файловый сервис через SQS", course.Id);
            else
                throw new Exception($"SQS вернул статус {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось отправить курс в очередь SQS");
        }
    }
}
