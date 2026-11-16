using System.Text.Json.Serialization;
using Insurance.Api.Application;
using Insurance.Api.Application.Abstractions;
using Insurance.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));

        // Don't echo serializer exception messages (they contain .NET type names) in 400 responses.
        options.AllowInputFormatterExceptionMessages = false;
    });
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
builder.Services.AddSingleton<IPolicyRepository, InMemoryPolicyRepository>();
builder.Services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();
builder.Services.AddSingleton<WriteLock>();
builder.Services.AddSingleton<CustomerService>();
builder.Services.AddSingleton<PolicyService>();
builder.Services.AddSingleton<ClaimService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Insurance API v1"));

app.MapControllers();

await app.RunAsync();
