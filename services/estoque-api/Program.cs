using estoque_api.Data;
using estoque_api.Integrations.Faturamento;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDirectory);

var databasePath = Path.Combine(dataDirectory, "estoque.db");
var connectionString = $"Data Source={databasePath}";

builder.Services.AddDbContext<EstoqueDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddHttpClient<FaturamentoApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5133/");
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
    var dbContext = scope.ServiceProvider.GetRequiredService<EstoqueDbContext>();
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