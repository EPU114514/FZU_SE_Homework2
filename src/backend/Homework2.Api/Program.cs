using System.Text.Json.Serialization;
using Homework2.Api.Infrastructure;
using Homework2.Application.Abstractions;
using Homework2.Application.Services;
using Homework2.Infrastructure.Excel;
using Homework2.Infrastructure.Persistence;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddSingleton<IExcelService, ClosedXmlExcelService>();

var storageProvider = builder.Configuration["Storage:Provider"] ?? "Json";
if (storageProvider.Equals("Mongo", StringComparison.OrdinalIgnoreCase))
{
    var section = builder.Configuration.GetSection("Storage:Mongo");
    var connectionString = section["ConnectionString"] ?? "mongodb://localhost:27017";
    var databaseName = section["Database"] ?? "homework2";
    var collectionName = section["Collection"] ?? "contacts";

    builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
    builder.Services.AddSingleton<IContactRepository>(serviceProvider =>
        new MongoContactRepository(
            serviceProvider.GetRequiredService<IMongoClient>().GetDatabase(databaseName),
            collectionName));
}
else
{
    var relativePath = builder.Configuration["Storage:Json:FilePath"] ?? "data/contacts.json";
    var filePath = Path.IsPathRooted(relativePath)
        ? relativePath
        : Path.Combine(builder.Environment.ContentRootPath, relativePath);

    builder.Services.AddSingleton<IContactRepository>(_ => new JsonContactRepository(filePath));
}

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();