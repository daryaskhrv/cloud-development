using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var gateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder.AddLocalStack("courseapp-localstack", awsConfig: awsConfig, configureContainer: container =>
{
    container.Lifetime = ContainerLifetime.Session;
    container.DebugLevel = 1;
    container.LogLevel = LocalStackLogLevel.Debug;
    container.Port = 4566;
    container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
});

var awsResources = builder.AddAWSCloudFormationTemplate("resources", "CloudFormation/course-template.yaml", "courseapp")
    .WithReference(awsConfig);

var minio = builder.AddMinioContainer("courseapp-minio");

for (var i = 0; i < 5; i++)
{
    var api = builder.AddProject<Projects.CourseApp_Api>($"courseapp-api-{i}", launchProfileName: null)
        .WithReference(redis)
        .WithReference(awsResources)
        .WaitFor(redis)
        .WaitFor(awsResources)
        .WithHttpsEndpoint(port: 8000 + i);
    gateway.WaitFor(api);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.AddProject<Projects.Service_FileStorage>("service-filestorage")
    .WithReference(awsResources)
    .WithReference(minio)
    .WithEnvironment("AWS__Resources__MinioBucketName", "courseapp-bucket")
    .WaitFor(awsResources)
    .WaitFor(minio);

builder.UseLocalStack(localstack);

builder.Build().Run();
