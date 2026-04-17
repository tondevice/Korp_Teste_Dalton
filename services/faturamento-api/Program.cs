using faturamento_api.Data;
using faturamento_api.Integrations.Estoque;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDirectory);

var databasePath = Path.Combine(dataDirectory, "faturamento.db");
var connectionString = $"Data Source={databasePath}";

builder.Services.AddDbContext<FaturamentoDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddHttpClient<EstoqueApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5119/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FaturamentoDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

app.UseAuthorization();

app.MapControllers();

app.Run();