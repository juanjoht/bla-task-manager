using System.Text.Json.Serialization;
using TaskManager.Api.ErrorHandling;
using TaskManager.Api.Extensions;
using TaskManager.Api.Filters;
using TaskManager.Api.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTaskManager();
builder.Services.AddJwtAuthentication();
builder.Services.AddFrontendCors();
builder.Services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())); // "Done", not 2
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();
builder.Services.AddOpenApi(options => options
    .AddDocumentTransformer<BearerSecuritySchemeTransformer>()
    .AddOperationTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.UseExceptionHandler();

// API documentation everywhere except production: /openapi/v1.json and the Swagger UI at /swagger.
if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Task Manager API"));
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

public partial class Program;
