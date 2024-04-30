using MultiFamilyPortal;
using MultiFamilyPortal.Apis;
using MultiFamilyPortal.Data;
using MultiFamilyPortal.Data.Models;
using MultiFamilyPortal.Extensions;
using MultiFamilyPortal.Identity;
using MultiFamilyPortal.SaaS;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCorePortalServices(builder.Configuration)
    .AddMFContext(builder.Configuration);

builder.Services.AddAuthentication();
builder.Services.AddIdentityApiEndpoints<SiteUser>()
    .AddEntityFrameworkStores<MFPContext>();

var app = builder.Build();

app.UseMissingTenant();
app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapMultifamilyPortalIdentityApi();
app.MapFavIcons();

app.MapFallbackToFile("/index.html");

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
