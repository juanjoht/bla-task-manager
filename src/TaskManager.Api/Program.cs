using TaskManager.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTaskManager();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

await app.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

public partial class Program;
