using System.Text.Json.Serialization;
using Insurance.Api.Application;
using Insurance.Api.Application.Abstractions;
using Insurance.Api.Controllers.Json;
using Insurance.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Exact member names only; see StrictEnumConverter.
        options.JsonSerializerOptions.Converters.Add(new StrictEnumConverterFactory());

        // Don't echo serializer exception messages (they contain .NET type names) in 400 responses.
        options.AllowInputFormatterExceptionMessages = false;
    });

// The OpenAPI generator reads these (minimal-API) options, not the MVC ones above. Without a string enum converter
// here, the document would describe enums as integers.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
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
