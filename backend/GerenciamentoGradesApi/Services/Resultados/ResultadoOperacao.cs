namespace GerenciamentoGradesApi.Services.Resultados;

// Resultado de uma operação de serviço que pode falhar de formas diferentes
// (não encontrado, conflito, entrada inválida) sem recorrer a exceções para
// controlar o fluxo. O Controller só faz um switch em cima de `Status` para
// decidir o IActionResult — toda a decisão de "o que deu errado" já vem
// pronta do Service.
public class ResultadoOperacao<T>
{
    public StatusOperacao Status { get; }
    public T? Valor { get; }
    public string? MensagemErro { get; }

    private ResultadoOperacao(StatusOperacao status, T? valor, string? mensagemErro)
    {
        Status = status;
        Valor = valor;
        MensagemErro = mensagemErro;
    }

    public static ResultadoOperacao<T> ComSucesso(T valor) => new(StatusOperacao.Sucesso, valor, null);

    public static ResultadoOperacao<T> NaoEncontrado(string mensagem) => new(StatusOperacao.NaoEncontrado, default, mensagem);

    public static ResultadoOperacao<T> Conflito(string mensagem) => new(StatusOperacao.Conflito, default, mensagem);

    public static ResultadoOperacao<T> EntradaInvalida(string mensagem) => new(StatusOperacao.EntradaInvalida, default, mensagem);
}
