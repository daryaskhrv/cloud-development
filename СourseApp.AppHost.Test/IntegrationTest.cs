using System.Text.Json;
using Aspire.Hosting;
using CourseApp.Domain.Entity;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace СourseApp.AppHost.Test;

/// <summary>
/// Интеграционные тесты для проверки микросервисного пайплайна:
/// API -&gt; SQS -&gt; FileStorage -&gt; MinIO
/// </summary>
/// <param name="output">Служба журналирования юнит-тестов</param>
public class IntegrationTest(ITestOutputHelper output) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private DistributedApplication? _app;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.CourseApp_AppHost>(cancellationToken);
        builder.Configuration["DcpPublisher:RandomizePorts"] = "false";
        builder.Services.AddLogging(logging =>
        {
            logging.AddXUnit(output);
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);
        });
        _app = await builder.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Проверяет, что вызов гейтвея:
    /// <list type="bullet">
    /// <item><description>В ответ отдаёт сгенерированный курс</description></item>
    /// <item><description>Сериализует курс в MinIO через брокер SQS и файловый сервис</description></item>
    /// <item><description>Данные, отданные клиенту и положенные в объектное хранилище, идентичны</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task ApiToStorageIntegrationTest()
    {
        var id = new Random().Next(1, 100);

        using var gatewayClient = _app!.CreateHttpClient("api-gateway", "http");
        using var gatewayResponse = await gatewayClient.GetAsync($"/course?id={id}");
        var apiCourse = JsonSerializer.Deserialize<Course>(await gatewayResponse.Content.ReadAsStringAsync(), _jsonOptions);

        await Task.Delay(5000);

        using var storageClient = _app!.CreateHttpClient("service-filestorage", "http");
        using var listResponse = await storageClient.GetAsync("/api/files");
        var fileList = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync());
        using var fileResponse = await storageClient.GetAsync($"/api/files/course_{id}.json");
        var s3Course = JsonSerializer.Deserialize<Course>(await fileResponse.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.NotNull(fileList);
        Assert.Single(fileList);
        Assert.Equal($"course_{id}.json", fileList[0]);
        Assert.NotNull(apiCourse);
        Assert.NotNull(s3Course);
        Assert.Equal(id, s3Course.Id);
        Assert.Equivalent(apiCourse, s3Course);
    }

    /// <summary>
    /// Проверяет, что повторный запрос курса с тем же id обслуживается из кэша
    /// и не приводит к повторной публикации в брокер: в бакете остаётся ровно один файл.
    /// </summary>
    [Fact]
    public async Task CacheHitDoesNotDuplicateStorageObjectTest()
    {
        var id = new Random().Next(1, 100);

        using var gatewayClient = _app!.CreateHttpClient("api-gateway", "http");
        using var firstResponse = await gatewayClient.GetAsync($"/course?id={id}");
        var firstCourse = JsonSerializer.Deserialize<Course>(await firstResponse.Content.ReadAsStringAsync(), _jsonOptions);
        using var secondResponse = await gatewayClient.GetAsync($"/course?id={id}");
        var secondCourse = JsonSerializer.Deserialize<Course>(await secondResponse.Content.ReadAsStringAsync(), _jsonOptions);

        await Task.Delay(5000);

        using var storageClient = _app!.CreateHttpClient("service-filestorage", "http");
        using var listResponse = await storageClient.GetAsync("/api/files");
        var fileList = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync());

        Assert.NotNull(firstCourse);
        Assert.NotNull(secondCourse);
        Assert.Equivalent(firstCourse, secondCourse);
        Assert.NotNull(fileList);
        Assert.Single(fileList);
        Assert.Equal($"course_{id}.json", fileList[0]);
    }

    /// <summary>
    /// Проверяет, что три запроса с разными идентификаторами создают три отдельных
    /// файла в бакете, содержимое которых совпадает с ответами гейтвея.
    /// </summary>
    [Fact]
    public async Task MultipleDistinctCoursesAreStoredTest()
    {
        var ids = new[] { 11, 22, 33 };
        var apiCourses = new Dictionary<int, Course>();

        using var gatewayClient = _app!.CreateHttpClient("api-gateway", "http");
        foreach (var id in ids)
        {
            using var response = await gatewayClient.GetAsync($"/course?id={id}");
            var course = JsonSerializer.Deserialize<Course>(await response.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(course);
            apiCourses[id] = course!;
        }

        await Task.Delay(5000);

        using var storageClient = _app!.CreateHttpClient("service-filestorage", "http");
        using var listResponse = await storageClient.GetAsync("/api/files");
        var fileList = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync());

        Assert.NotNull(fileList);
        Assert.Equal(ids.Length, fileList!.Count);
        foreach (var id in ids)
            Assert.Contains($"course_{id}.json", fileList);

        foreach (var id in ids)
        {
            using var fileResponse = await storageClient.GetAsync($"/api/files/course_{id}.json");
            var s3Course = JsonSerializer.Deserialize<Course>(await fileResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(s3Course);
            Assert.Equal(id, s3Course!.Id);
            Assert.Equivalent(apiCourses[id], s3Course);
        }
    }

    /// <summary>
    /// Проверяет, что запрос несуществующего объекта из бакета завершается ошибкой.
    /// </summary>
    [Fact]
    public async Task MissingStorageObjectReturnsErrorTest()
    {
        using var storageClient = _app!.CreateHttpClient("service-filestorage", "http");
        using var response = await storageClient.GetAsync("/api/files/course_99999.json");

        Assert.False(response.IsSuccessStatusCode);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _app!.StopAsync();
        await _app.DisposeAsync();
    }
}
