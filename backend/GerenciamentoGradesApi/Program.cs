using GerenciamentoGradesApi.Data;
using GerenciamentoGradesApi.Middleware;
using GerenciamentoGradesApi.Repositories;
using GerenciamentoGradesApi.Repositories.Interfaces;
using GerenciamentoGradesApi.Services;
using GerenciamentoGradesApi.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "Gerenciamento de Grades API", Version = "v1" }));

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()));

builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IGradeRepository, GradeRepository>();
builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
builder.Services.AddScoped<IGradeService, GradeService>();
builder.Services.AddScoped<IPlanilhaGradeService, PlanilhaGradeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gerenciamento de Grades API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("Frontend");

app.UseAuthorization();

// Stand-in enquanto não existe login/SSO: exige e valida a matrícula do
// usuário (cabeçalho X-Matricula) em toda escrita sob /api/grades.
app.UseMiddleware<MatriculaMiddleware>();

app.MapControllers();

app.Run();
