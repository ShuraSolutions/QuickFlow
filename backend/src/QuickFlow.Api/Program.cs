using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using QuickFlow.Api.Data;
using QuickFlow.Api.Infrastructure;
using QuickFlow.Domain.Common;

var builder = WebApplication.CreateBuilder(args);

// Database: SQLite file next to the project (content root), migrated at startup.
var dbFile = builder.Configuration["Database:File"] ?? "quickflow.db";
var dbPath = Path.IsPathRooted(dbFile) ? dbFile : Path.Combine(builder.Environment.ContentRootPath, dbFile);
builder.Services.AddDbContext<QuickFlowDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Location")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QuickFlow API",
        Version = "v1",
        Description = "Tasks, habits, learning resources, todo plans, settings and dashboard for the QuickFlow personal productivity app.",
    });
    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) o.IncludeXmlComments(xml);
    o.SupportNonNullableReferenceTypes();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<QuickFlowDbContext>().Database.Migrate();
}
app.Logger.LogInformation("QuickFlow database: {DbPath}", dbPath);

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "QuickFlow API v1"));

app.MapControllers();

app.Run();
