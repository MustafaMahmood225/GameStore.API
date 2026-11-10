using GameStore.Api.Data;
using GameStore.Api.Endpoints;
using Scalar.AspNetCore;
using Serilog;

// WHY: We removed "using GenresEnpoints" (it had a typo) because GenresEndpoints now lives
// in the GameStore.Api.Endpoints namespace — the same one already imported above.
// Keeping one consistent namespace means fewer using statements and less confusion.

// The builder object is used to configure services for the program.
var builder = WebApplication.CreateBuilder(args);

// WHY: UseSerilog replaces the built-in logging system with Serilog. Everything that
// already calls ILogger (your endpoints, EF Core, ASP.NET itself) now flows through it.
// ReadFrom.Configuration lets us configure levels and destinations in appsettings.json
// instead of hard-coding them, so you can change logging without recompiling.
// ReadFrom.Services lets Serilog use anything registered in dependency injection.
builder.Host.UseSerilog((context,services,configuration) => 
configuration.ReadFrom.Configuration(context.Configuration)
.ReadFrom.Services(services));

builder.Services.AddOpenApi();
// WHY: AddValidation() registers the validation services that power your [Required],
// [Range], and [StringLength] attributes on DTOs.
builder.Services.AddValidation();

// WHY: AddProblemDetails() tells ASP.NET to format unhandled errors as structured JSON
// (RFC 7807 "Problem Details") instead of a raw HTML error page.
// This makes errors easy for clients (mobile apps, React, etc.) to parse and display.
builder.Services.AddProblemDetails();

builder.AddGameStoreDb();

var app = builder.Build();

// WHY: UseExceptionHandler() is middleware — it sits between the client and your endpoints
// and catches any unhandled exception, returning a clean JSON error response instead of crashing.
// WHY: UseStatusCodePages() converts bare HTTP errors (like a plain 404) into the same
// structured JSON format, so error responses are consistent throughout your API.
app.UseExceptionHandler();
app.UseStatusCodePages();

// WHY: By default ASP.NET writes several log lines per request. This middleware replaces
// them with a single summary line, such as "HTTP GET /games responded 200 in 12.3 ms".
// It goes after UseExceptionHandler so that crashed requests are logged as 500 and not
// as success.
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Dependency Injection (DI) is a design pattern where a class receives its required tools (dependencies)
// from an external container (service container) rather than creating them itself.
// This approach decouples your code, making it easier to swap out dependencies,
// manage configurations centrally in Program.cs, and safely mock external services during testing.

// The container also needs to know how long each tool should live:
// - Transient:  A brand new instance is created every time a class asks for it.
// - Scoped:     A new instance is created once per HTTP request, shared within that request.
// - Singleton:  A single instance is created once and shared by everyone for the app's lifetime.

app.MapGamesEndpoints();
app.MapGenresEndpoints();

app.MigrateDb();

app.Run();
