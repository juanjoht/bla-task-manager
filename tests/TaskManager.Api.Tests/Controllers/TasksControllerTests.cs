using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Tests.Common;
using TaskManager.Application.Dtos.Auth;
using TaskManager.Application.Dtos.Tasks;
using TaskManager.Domain.Enums;

namespace TaskManager.Api.Tests.Controllers;

public sealed class TasksControllerTests : IDisposable
{
    private readonly TaskManagerApiFactory _factory = new();
    private readonly HttpClient _client;

    public TasksControllerTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    // Two days ahead so the date is in the future in any time zone.
    private static DateOnly Soon => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2);

    private async Task<IReadOnlyList<TaskDto>> ListAsync(string query = "") =>
        (await _client.GetFromJsonAsync<List<TaskDto>>($"/api/tasks{query}", ApiJson.Options))!;

    private async Task<TaskDto> CreateAsync(string title = "New task")
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest(title, "Details", Soon), ApiJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskDto>(ApiJson.Options))!;
    }

    // ---------- Authorization ----------

    [Fact]
    public async Task Tasks_RequireAToken()
    {
        var response = await _client.GetAsync("/api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- List ----------

    [Fact]
    public async Task List_ReturnsTheDemoUsersTasks()
    {
        await _client.AuthenticateAsync();

        var tasks = await ListAsync();

        tasks.Should().HaveCount(5);
    }

    [Fact]
    public async Task List_WithStatusFilter_ReturnsOnlyMatchingTasks()
    {
        await _client.AuthenticateAsync();

        var tasks = await ListAsync("?status=Done");

        tasks.Should().ContainSingle().Which.Status.Should().Be(TaskItemStatus.Done);
    }

    [Fact]
    public async Task List_SerializesStatusAsText()
    {
        await _client.AuthenticateAsync();

        var json = await _client.GetStringAsync("/api/tasks?status=Done");

        json.Should().Contain("\"status\":\"Done\"");
    }

    // ---------- Get ----------

    [Fact]
    public async Task Get_AnExistingTask_Returns200()
    {
        await _client.AuthenticateAsync();
        var created = await CreateAsync("Find me");

        var task = await _client.GetFromJsonAsync<TaskDto>($"/api/tasks/{created.Id}", ApiJson.Options);

        task!.Title.Should().Be("Find me");
    }

    [Fact]
    public async Task Get_AnUnknownTask_Returns404()
    {
        await _client.AuthenticateAsync();

        var response = await _client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail.Should().Be("Task not found.");
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_WithValidData_Returns201WithLocationOfTheNewTask()
    {
        await _client.AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Prepare demo", null, Soon), ApiJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await response.Content.ReadFromJsonAsync<TaskDto>(ApiJson.Options);
        task!.Status.Should().Be(TaskItemStatus.Pending);
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/tasks/{task.Id}");
    }

    [Fact]
    public async Task Create_WithInvalidData_Returns400WithFieldErrors()
    {
        await _client.AuthenticateAsync();
        var past = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);

        var response = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("", null, past), ApiJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Keys.Should().Contain(["Title", "DueDate"]);
    }

    // ---------- Update ----------

    [Fact]
    public async Task Update_WithValidData_Returns200WithTheChanges()
    {
        await _client.AuthenticateAsync();
        var created = await CreateAsync();

        var response = await _client.PutAsJsonAsync($"/api/tasks/{created.Id}",
            new UpdateTaskRequest("Renamed", "New details", TaskItemStatus.Done, Soon), ApiJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.Content.ReadFromJsonAsync<TaskDto>(ApiJson.Options);
        task!.Title.Should().Be("Renamed");
        task.Status.Should().Be(TaskItemStatus.Done);
    }

    [Fact]
    public async Task Update_WithInvalidData_Returns400()
    {
        await _client.AuthenticateAsync();
        var created = await CreateAsync();

        var response = await _client.PutAsJsonAsync($"/api/tasks/{created.Id}",
            new UpdateTaskRequest("", null, TaskItemStatus.Done, Soon), ApiJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_AnUnknownTask_Returns404()
    {
        await _client.AuthenticateAsync();

        var response = await _client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}",
            new UpdateTaskRequest("Title", null, TaskItemStatus.Done, Soon), ApiJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_AnExistingTask_Returns204AndTheTaskIsGone()
    {
        await _client.AuthenticateAsync();
        var created = await CreateAsync();

        var response = await _client.DeleteAsync($"/api/tasks/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/tasks/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- Ownership ----------

    [Fact]
    public async Task AnotherUser_CannotSeeChangeOrDeleteTheDemoUsersTasks()
    {
        await _client.AuthenticateAsync();
        var demoTask = (await ListAsync()).First();

        var intruder = _factory.CreateClient();
        await intruder.PostAsJsonAsync("/api/auth/register", new RegisterRequest("intruder@ballastlane.test", "Intruder", "Secret123"));
        await intruder.AuthenticateAsync("intruder@ballastlane.test", "Secret123");

        (await intruder.GetFromJsonAsync<List<TaskDto>>("/api/tasks", ApiJson.Options)).Should().BeEmpty();
        (await intruder.GetAsync($"/api/tasks/{demoTask.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await intruder.PutAsJsonAsync($"/api/tasks/{demoTask.Id}",
                new UpdateTaskRequest("Hacked", null, TaskItemStatus.Done, Soon), ApiJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await intruder.DeleteAsync($"/api/tasks/{demoTask.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var stillThere = await _client.GetFromJsonAsync<TaskDto>($"/api/tasks/{demoTask.Id}", ApiJson.Options);
        stillThere!.Title.Should().Be(demoTask.Title);
    }
}
