using FiscalDocuments.Api.Data;
using FiscalDocuments.Worker;
using Microsoft.EntityFrameworkCore;


var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<FiscalDocumentsDbContext>(
    options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString(
                "DefaultConnection"
            )
        )
);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
