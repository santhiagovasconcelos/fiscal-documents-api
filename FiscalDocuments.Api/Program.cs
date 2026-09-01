using FiscalDocuments.Api.Interfaces;
using FiscalDocuments.Api.Services;

var builder = WebApplication.CreateBuilder(args);

//Registreo dos controllers da API. 
builder.Services.AddControllers();

// Registro do serviço para injeção de dependência.
builder.Services.AddScoped<IFiscalDocumentService, FiscalDocumentService>();

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