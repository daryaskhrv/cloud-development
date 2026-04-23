using System.Text.Json;
using CourseApp.Api.Messaging;
using CourseApp.Domain.Entity;
using Microsoft.Extensions.Caching.Distributed;

namespace CourseApp.Api.Services;

/// <summary>
/// Сервис для получения информации о курсе
/// </summary>
public class CourseService(IDistributedCache _cache, IConfiguration _configuration,
                ILogger<CourseService> _logger, CourseGenerator _generator,
                IProducerService _producer)
{
    /// <summary>
    /// Получает курс по идентификатору из кэша или с помощью генератора
    /// </summary>
    public async Task<Course> GetCourseAsync(int id)
    {

        var cacheKey = $"course-{id}";
        _logger.LogInformation("Попытка получить курс {CourseId} из кэша", id);
        var cachedData = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            try
            {
                var cachedCourse = JsonSerializer.Deserialize<Course>(cachedData);

                if (cachedCourse != null)
                {
                    _logger.LogInformation("Курс {CourseId} успешно получен из кэша", id);
                    return cachedCourse;
                }
                _logger.LogWarning("Курс {CourseId} найден в кэше, но десериализация вернула null", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка десериализации курса {CourseId} из кэша", id);
            }
        }

        _logger.LogInformation("Курс {CourseId} отсутствует в кэше. Начинаем генерацию", id);

        var course = _generator.Generate(id);

        await _producer.SendMessage(course);

        try
        {
            var expirationMinutes = _configuration.GetValue("CacheSettings:ExpirationMinutes", 5);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes)
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(course), cacheOptions);
            _logger.LogInformation("Курс {CourseId} сгенерирован и сохранён в кэш", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось сохранить курс {CourseId} в кэш. Работа продолжается без кэширования.", id);
        }
        return course;
    }
}