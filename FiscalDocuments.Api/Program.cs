using FiscalDocuments.Api.Interfaces;
using FiscalDocuments.Api.Services;
using FiscalDocuments.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

//Registro dos controllers da API e conversão para não apresentar número no json retornado pela API. 
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

// Registro do serviço para injeção de dependência.
builder.Services.AddScoped<IFiscalDocumentService, FiscalDocumentService>();

// A connection string é obtida da configuração da aplicação.
// Em desenvolvimento, as credenciais são armazenadas via User Secrets.
builder.Services.AddDbContext<FiscalDocumentsDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();