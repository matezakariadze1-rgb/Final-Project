using Microsoft.EntityFrameworkCore;
using SpaceHub.Infrastructure.Data;
using SpaceHub.Application.Auth;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using SpaceHub.Api.Configuration;
using SpaceHub.Api.DTOs;
using SpaceHub.Api.Middleware;
using SpaceHub.Application;
using SpaceHub.Infrastructure;

var seedDemo = args.Contains("--seed-demo", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(x => x != "--seed-demo").ToArray());
if (seedDemo)
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Demo data can only be imported in Development.");
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is not configured.")));
    builder.Services.AddScoped<IPasswordService, SpaceHub.Infrastructure.Auth.PasswordService>();
    using var seedApp = builder.Build();
    await using var scope = seedApp.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    var imported = await new DemoDataSeeder(db, scope.ServiceProvider.GetRequiredService<IPasswordService>()).SeedAsync();
    Console.WriteLine(imported
        ? "Demo data imported: 3 users, 2 spaces, 3 services, 1 package, 4 bookings, 3 events, 3 tickets."
        : "Demo import skipped: a reserved demo account already exists. No demo records were changed.");
    return;
}
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new SpaceHub.Api.Serialization.UtcDateTimeConverter())).ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(new ErrorResponse(
        400, "Validation failed", context.ModelState.Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(x => JsonNamingPolicy.CamelCase.ConvertName(x.Key),
                x => x.Value!.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid request value." : e.ErrorMessage).ToArray())));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the access token returned by register or login."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.AddApplication();
builder.Services.AddHostedService<SpaceHub.Api.Background.BookingMaintenanceWorker>();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured."));
builder.Services.AddJwtAuthentication(builder.Configuration);
var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    await response.WriteAsJsonAsync(ErrorResponse.Create(response.StatusCode, "Request could not be completed."));
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapGet("/", () => Results.Redirect("/web/"));
app.MapGet("/web/", (IWebHostEnvironment environment) =>
{
    var index = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "web", "index.html");
    return File.Exists(index)
        ? Results.File(index, "text/html; charset=utf-8")
        : Results.NotFound(ErrorResponse.Create(404, "Frontend is not built. Run npm ci and npm run build in frontend."));
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
