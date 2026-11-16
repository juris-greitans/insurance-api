using System.Text.Json.Serialization;
using Insurance.Api.Application;
using Insurance.Api.Application.Abstractions;
using Insurance.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
builder.Services.AddSingleton<IPolicyRepository, InMemoryPolicyRepository>();
builder.Services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();
builder.Services.AddSingleton<WriteLock>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<PolicyService>();
builder.Services.AddScoped<ClaimService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Insurance API v1"));

app.MapControllers();

await app.RunAsync();
