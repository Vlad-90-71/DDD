using DDD.Domain.Common;
using DDD.Infrastructure.Persistence;
using DDD.Presentation.Common.Json;
using Scalar.AspNetCore;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Подключаем глобальную фабрику для всех SmartEnum
        options.JsonSerializerOptions.Converters.Add(new SmartEnumJsonConverterFactory());
    });

// 2. Регистрируем AppDbContext через созданный метод расширения
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    // Добавляем трансформер схем для авто-документирования SmartEnum
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        var type = context.JsonTypeInfo.Type;

        // Проверяем, реализует ли тип интерфейс ISmartEnum<>
        var smartEnumInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISmartEnum<>));

        if (smartEnumInterface != null)
        {
            // Получаем метод GetAll через рефлексию
            var getAllMethod = type.GetMethod("GetAll", BindingFlags.Public | BindingFlags.Static);
            if (getAllMethod != null && getAllMethod.Invoke(null, null) is System.Collections.IEnumerable values)
            {
                var descriptionBuilder = new StringBuilder("Доступные значения:<br/>");

                foreach (var item in values)
                {
                    var valueProp = item.GetType().GetProperty("Value")?.GetValue(item);
                    var name = item.ToString();

                    descriptionBuilder.Append($"• <b>{valueProp}</b> = {name}<br/>");
                }

                // Переопределяем описание поля и тип в документации
                schema.Description = descriptionBuilder.ToString();
                schema.Type = "integer";
            }
        }

        return Task.CompletedTask;
    });
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    // app.UseSwagger();
    // app.UseSwaggerUI();

}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

