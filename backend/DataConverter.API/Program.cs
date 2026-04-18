using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataConverter.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register services for dependency injection
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IJsonToClassService, JsonToClassService>();
builder.Services.AddScoped<IExcelCsvService, ExcelCsvService>();
builder.Services.AddScoped<IMysqlService, MysqlService>();
builder.Services.AddScoped<IDataGeneratorService, DataGeneratorService>();
builder.Services.AddScoped<IJsonToSqlService, JsonToSqlService>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IDatabaseToSqlService, DatabaseToSqlService>();
builder.Services.AddScoped<IDatabaseToJsonService, DatabaseToJsonService>();
builder.Services.AddScoped<IDataFormatService, DataFormatService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();