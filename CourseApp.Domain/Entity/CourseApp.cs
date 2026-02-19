namespace CourseApp.Domain.Entity;

/// <summary>
/// Модель учебного курса
/// </summary>
public class Course
{
    /// <summary>
    /// Идентификатор курса в системе
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Наименование курса
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ФИО преподавателя
    /// </summary>
    public string TeacherFullName { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала курса
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Дата окончания курса 
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Максимальное количество студентов
    /// </summary>
    public int MaxStudents { get; set; }

    /// <summary>
    /// Текущее количество записанных студентов
    /// </summary>
    public int CurrentStudents { get; set; }

    /// <summary>
    /// Выдача сертификата
    /// </summary>
    public bool HasCertificate { get; set; }

    /// <summary>
    /// Стоимость 
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Рейтинг 
    /// </summary>
    public int Rating { get; set; }
}