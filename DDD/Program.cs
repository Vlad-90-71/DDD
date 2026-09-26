using Scalar.AspNetCore;
using DDD.Extensions;
using DDD.Application.Extensions;
using DDD.Infrastructure.Extensions;
using DDD.MessageBroker.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPresentationControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddInMemoryMessageBroker();

var app = builder.Build();

// Самый первый
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

