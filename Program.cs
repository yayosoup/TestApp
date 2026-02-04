using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TestApp.Data;

var builder = WebApplication.CreateBuilder(args);

var secretJson = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
if (string.IsNullOrWhiteSpace(secretJson))
    throw new Exception("Missing env var: ConnectionStrings__Default (Secrets Manager JSON)");

var host = Environment.GetEnvironmentVariable("DB_HOST");
if (string.IsNullOrWhiteSpace(host))
    throw new Exception("Missing env var: DB_HOST (RDS endpoint)");

var secret = JsonSerializer.Deserialize<DbSecret>(secretJson)
             ?? throw new Exception("Could not parse ConnectionStrings__Default as JSON");

var dbName = "database-1"; // or whatever you set as initial DB name
var connString = $"Host={host};Database={dbName};Username={secret.username};Password={secret.password}";

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connString));
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("ok"));

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.CanConnect(); // will throw if SG is wrong
}

app.Run();

public sealed class DbSecret
{
    public string username { get; set; } = "";
    public string password { get; set; } = "";
}
