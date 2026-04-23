using Amazon.SQS;
using CourseApp.Api.Messaging;
using CourseApp.Api.Services;
using CourseApp.ServiceDefaults;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<CourseGenerator>();
builder.Services.AddScoped<CourseService>();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSQS>();
builder.Services.AddScoped<IProducerService, SqsProducerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
