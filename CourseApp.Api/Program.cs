using CourseApp.Api.Services;
using CourseApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("wasm", policy =>
    {
        policy.AllowAnyOrigin()
        .WithMethods("GET", "POST")
        .WithHeaders("Content-Type");
    });
});

builder.Services.AddScoped<CourseGenerator>();
builder.Services.AddScoped<CourseService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseCors("wasm");
app.UseAuthorization();
app.MapControllers();
app.Run();