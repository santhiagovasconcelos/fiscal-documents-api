using FiscalDocuments.Api.Interfaces;
using FiscalDocuments.Api.Services;
using FiscalDocuments.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//Registreo dos controllers da API. 
builder.Services.AddControllers();

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