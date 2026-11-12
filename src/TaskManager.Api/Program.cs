using TaskManager.Api.ErrorHandling;
using TaskManager.Api.Extensions;
using TaskManager.Api.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTaskManager();
builder.Services.AddJwtAuthentication();
builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

public partial class Program;
