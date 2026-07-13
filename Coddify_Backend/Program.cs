using Microsoft.EntityFrameworkCore;
using Infrastructure.Context;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
   options.UseSqlServer(
       builder.Configuration.GetConnectionString("ConnectionString"),
       sqlOptions =>
       {
           sqlOptions.EnableRetryOnFailure(
               maxRetryCount: 5,
               maxRetryDelay: TimeSpan.FromSeconds(30),
               errorNumbersToAdd: null);
       }));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Swagger (Swashbuckle) setup for interactive API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Enable Swagger middleware in development for interactive UI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Coddify API V1");
        c.RoutePrefix = string.Empty; // Serve the UI at application's root (https://localhost:<port>/)
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
