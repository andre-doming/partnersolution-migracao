namespace Partner.Api.Infrastructure.Security;

/// <summary>
/// Opções de configuração centralizadas para comportamento de CPF.
/// Substitui decisões hardcoded por feature flags parametrizáveis.
/// </summary>
public class CpfOptions
{
    /// <summary>
    /// Se verdadeiro, valida CPF pelos dígitos verificadores (algoritmo modulo 11).
    /// Se falso, aceita qualquer CPF com 11 dígitos sem validar algoritmo.
    /// </summary>
    public bool ValidationEnabled { get; set; } = true;

    /// <summary>
    /// Se verdadeiro, mascara CPF em logs e responses (mostra apenas últimos 4 dígitos).
    /// Se falso, exibe CPF completo em logs e responses.
    /// </summary>
    public bool MaskEnabled { get; set; } = true;

    /// <summary>
    /// Se verdadeiro, prepara infraestrutura para hash de CPF.
    /// Nesta fase: não altera banco, não migra dados, apenas centraliza decisão.
    /// </summary>
    public bool HashEnabled { get; set; } = false;
}
