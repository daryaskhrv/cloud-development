using Api.Gateway.LoadBalancers;
using CourseApp.ServiceDefaults;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot()
    .AddCustomLoadBalancer((sp, _, provider) =>
        new WeightedRandom(provider.GetAsync, sp.GetRequiredService<IConfiguration>()));

builder.Services.AddCors(options =>
{
    options.AddPolicy("wasm", policy =>
    {
        policy.AllowAnyOrigin()
        .WithMethods("GET")
        .WithHeaders("Content-Type");
    });
});

var app = builder.Build();

app.UseCors("wasm");

app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();
