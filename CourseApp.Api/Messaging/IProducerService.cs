using CourseApp.Domain.Entity;

namespace CourseApp.Api.Messaging;

/// <summary>
/// Интерфейс службы для отправки сгенерированных курсов в брокер сообщений
/// </summary>
public interface IProducerService
{
    /// <summary>
    /// Отправляет сообщение в брокер
    /// </summary>
    /// <param name="course">Учебный курс</param>
    public Task SendMessage(Course course);
}
