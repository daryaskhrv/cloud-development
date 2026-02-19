using CourseApp.Api.Services;
using CourseApp.Domain.Entity;
using Microsoft.AspNetCore.Mvc;

namespace CourseApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")] 
public class CourseController(CourseService _courseService) : ControllerBase
{
    /// <summary>
    /// Получить курс по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор курса</param>
    /// <returns>Информация о курсе</returns>
    [HttpGet] 
    public async Task<ActionResult<Course>> Get(int id)
    {
        var course = await _courseService.GetCourseAsync(id);
        return Ok(course);
    }
}