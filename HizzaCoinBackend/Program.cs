
using HizzaCoinBackend.Models;
using HizzaCoinBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<HizzaCoinDatabaseSettings>(
    builder.Configuration.GetSection("HizzaCoinDatabase"));

builder.Services.AddSingleton<AccountsService>();

builder.Services.AddControllers()
    .AddJsonOptions(
        options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddOpenApiDocument(settings =>
{
    settings.Title = "My NSwag API";
    settings.Version = "v1";
    settings.DocumentName = "v1";
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi(config =>
    {
        config.Path = "/openapi/v1.json";
    });

    // Serve Swagger UI at /swagger, pointing to the custom JSON path
    app.UseSwaggerUi(config =>
    {
        config.Path = "/swagger";
        config.DocumentPath = "/openapi/v1.json";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();