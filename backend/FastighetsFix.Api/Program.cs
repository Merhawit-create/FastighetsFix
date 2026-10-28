using FastighetsFix.Infrastructure;
using FastighetsFix.Infrastructure.Persistence.Data;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration
                           .GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException(
                           "Anslutningssträngen DefaultConnection saknas.");

builder.Services.AddInfrastructure(connectionString);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
var app = builder.Build();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    var seedPassword = app.Configuration["Seed:Password"];
    if (string.IsNullOrWhiteSpace(seedPassword))
    {
        throw new InvalidOperationException("Seed:Password saknas. Lägg till värdet i User Secrets.");
    }
    await DevelopmentDataSeeder.SeedAsync(app.Services, seedPassword);
}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.Run();