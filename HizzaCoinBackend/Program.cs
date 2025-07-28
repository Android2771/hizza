
using HizzaCoinBackend.Models;
using HizzaCoinBackend.Services;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HizzaCoinDatabaseSettings>(
    builder.Configuration.GetSection("HizzaCoinDatabase"));

//Created once and used for every http request (to instantiate client)
builder.Services.AddSingleton<IMongoDatabase>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<HizzaCoinDatabaseSettings>>().Value;
    var client = new MongoClient(settings.ConnectionString);
    return client.GetDatabase(settings.DatabaseName);
});

builder.Services.AddSingleton<AccountsService>();
builder.Services.AddSingleton<ChallengesService>();
builder.Services.AddSingleton<TransactionsService>();
builder.Services.AddSingleton<RewardsService>();
builder.Services.AddSingleton<CoinCommandsService>();

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

using (var scope = app.Services.CreateScope())
{
    var challengesService = app.Services.GetRequiredService<ChallengesService>();
    await challengesService.CancelAllChallenges();
    var challenges = await challengesService.GetAsync();
}

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