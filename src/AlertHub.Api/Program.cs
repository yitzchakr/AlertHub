
using AlertHub.Api;
using AlertHub.Api.Middlewares;
using AlertHub.Api.OpenApi;
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
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddRateLimitPolicies();

builder.Services.AddValidatorsFromAssemblyContaining<CreateIncidentCommand>();
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(CreateIncidentCommand).Assembly);
    opts.UseFluentValidation();
});

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous(); // Interactive UI at /scalar/v1
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();

app.Run();


