
using AlertHub.Api;
using AlertHub.Application.Features.Incidents.CreateIncident;
using AlertHub.Infrastructure;
using FluentValidation;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.FluentValidation;
var builder = WebApplication.CreateBuilder(args);

//registration of infratructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddValidatorsFromAssemblyContaining<CreateIncidentCommand>();
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(CreateIncidentCommand).Assembly);
    opts.UseFluentValidation();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Interactive UI at /scalar/v1
}
app.UseHttpsRedirection();

app.MapEndpoints();

app.Run();

