using AlertHub.Api.EndPoints;
using AlertHub.Infrastructure;
using AlertHub.Infrastructure.Persistence;
using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);

//registration of infratructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

