using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Todo API", Version = "v1" });
});

var connectionString = builder.Configuration.GetConnectionString("ToDoDB");
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.Parse("8.0.36-mysql")), ServiceLifetime.Singleton);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseCors("AllowAll");

app.MapGet("/", () => "hello");
app.MapGet("/tasks", GetAllTasks);
app.MapGet("/task/{id}", GetTaskById);
app.MapPost("/task", AddTask);
app.MapPut("/task/{id}", UpdateTask);
app.MapDelete("/task/{id}", DeleteTask);

app.Run();

async Task GetAllTasks(ToDoDbContext dbContext, HttpContext context)
{
    var tasks = await dbContext.Items.ToListAsync();
    await context.Response.WriteAsJsonAsync(tasks);
}

async Task GetTaskById(ToDoDbContext dbContext, HttpContext context, int id)
{
    var task = await dbContext.Items.FindAsync(id);
    if (task == null)
    {
        await context.Response.WriteAsJsonAsync($"Task with id {id} not found");
    }
    await context.Response.WriteAsJsonAsync(task);
}

async Task AddTask(ToDoDbContext dbContext, HttpContext context, Item item)
{   item.IsComplete=false;
    dbContext.Items.Add(item);
    await dbContext.SaveChangesAsync();
    context.Response.StatusCode = StatusCodes.Status201Created;
    await context.Response.WriteAsJsonAsync(item);
}

async Task UpdateTask(ToDoDbContext dbContext, HttpContext context, int id, Item updatedItem)
{
    if (updatedItem == null)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Invalid task data");
        return;
    }

    var existingItem = await dbContext.Items.FindAsync(id);
    if (existingItem == null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync($"Task with ID {id} not found");
        return;
    }

    if (updatedItem.Name != null)
    {
        existingItem.Name = updatedItem.Name;
    }

    existingItem.IsComplete = updatedItem.IsComplete;

    await dbContext.SaveChangesAsync();
    context.Response.StatusCode = StatusCodes.Status200OK;
    await context.Response.WriteAsJsonAsync(existingItem);
}

async Task DeleteTask(ToDoDbContext dbContext, HttpContext context, int id)
{
    var existingItem = await dbContext.Items.FindAsync(id);
    if (existingItem == null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    dbContext.Items.Remove(existingItem);
    await dbContext.SaveChangesAsync();
    context.Response.StatusCode = StatusCodes.Status200OK;
}
