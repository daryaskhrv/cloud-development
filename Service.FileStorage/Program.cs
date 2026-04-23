using Amazon.SQS;
using CourseApp.ServiceDefaults;
using LocalStack.Client.Extensions;
using Service.FileStorage.Messaging;
using Service.FileStorage.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSQS>();
builder.Services.AddHostedService<SqsConsumerService>();

builder.AddMinioClient("courseapp-minio");
builder.Services.AddScoped<IFileStorageService, MinioFileStorageService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
    await storage.EnsureBucketExists();
}

app.MapDefaultEndpoints();
app.MapControllers();
app.Run();
