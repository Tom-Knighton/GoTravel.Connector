using System.Reflection;
using System.Text.Json;
using GoTravel.Connector.Services.ServiceCollections;
using GoTravel.Connector.Services.ServiceCollections.Connections;

var builder = WebApplication.CreateBuilder(args);

var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{environmentName}.json", false)
    .AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly());
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddRedis(builder.Configuration.GetSection("Redis"))
    .AddRabbitMq(builder.Configuration.GetSection("Rabbit"))
    .AddHangfireJobs(builder.Configuration.GetSection("Hangfire"))
    .AddTfLServices(builder.Configuration.GetSection("Connections:TfL"))
    .RegisterConnectionImplementations()
    .ConfigureHttpJsonOptions(o =>
    {
        o.SerializerOptions.PropertyNameCaseInsensitive = true;
    });

var app = builder.Build();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHangfire(builder.Configuration.GetSection("Hangfire"));
app.UseHttpsRedirection();


app.Run();