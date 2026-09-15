namespace GerenciamentoGradesApi.Middleware;

// Enquanto não existe captura automática da matrícula (login/SSO), toda
// requisição que altera dados (POST/PUT/DELETE/PATCH) em /api/grades precisa
// informar o cabeçalho X-Matricula. O front-end intercepta essas chamadas e
// pede a matrícula ao usuário antes de completá-las (ver src/auth/matricula.ts).
public class MatriculaMiddleware(RequestDelegate next)
{
    public const string CabecalhoMatricula = "X-Matricula";
    public const string ChaveContexto = "Matricula";

    private static readonly HashSet<string> MetodosQueExigemMatricula = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete, HttpMethods.Patch
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var precisaMatricula = context.Request.Path.StartsWithSegments("/api/grades")
            && MetodosQueExigemMatricula.Contains(context.Request.Method);

        if (precisaMatricula)
        {
            var matricula = context.Request.Headers[CabecalhoMatricula].FirstOrDefault()?.Trim();

            if (string.IsNullOrWhiteSpace(matricula) || matricula.Length is < 3 or > 20 || !matricula.All(char.IsDigit))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    mensagem = $"Matrícula do usuário é obrigatória e deve ser numérica (cabeçalho '{CabecalhoMatricula}')."
                });
                return;
            }

            context.Items[ChaveContexto] = matricula;
        }

        await next(context);
    }
}

public static class HttpContextMatriculaExtensions
{
    // Só deve ser chamado em ações POST/PUT/DELETE/PATCH de /api/grades — o MatriculaMiddleware
    // garante que a matrícula já foi validada e está no contexto antes da ação rodar.
    public static string ObterMatricula(this HttpContext context)
    {
        return context.Items[MatriculaMiddleware.ChaveContexto] as string
            ?? throw new InvalidOperationException(
                "Matrícula não encontrada no contexto — o endpoint não está protegido pelo MatriculaMiddleware.");
    }
}
