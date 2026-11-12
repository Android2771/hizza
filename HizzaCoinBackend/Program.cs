
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

builder.Services.AddSingleton<IAccountsService, AccountsService>();
builder.Services.AddSingleton<IChallengesService, ChallengesService>();
builder.Services.AddSingleton<ITransactionsService, TransactionsService>();
builder.Services.AddSingleton<IRewardsService, RewardsService>();
builder.Services.AddSingleton<IRouletteService, RouletteService>();
builder.Services.AddSingleton<IStatisticsService, StatisticsService>();
builder.Services.AddSingleton<ICoinCommandsService, CoinCommandsService>();
builder.Services.AddSingleton<ICoinRandomSource, CoinRandomSource>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IDestinyProvider, DestinyProvider>();

builder.Services.AddControllers()
    .AddJsonOptions(
        options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddOpenApiDocument(settings =>
{
    settings.Title = "My NSwag API";
    settings.Version = "v1";
    settings.DocumentName = "v1";
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var challengesService = app.Services.GetRequiredService<IChallengesService>();
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

public partial class Program { }