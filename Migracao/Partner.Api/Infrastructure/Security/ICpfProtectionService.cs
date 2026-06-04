namespace Partner.Api.Infrastructure.Security;

/// <summary>
/// Serviço centralizado para operações de proteção e validação de CPF.
/// Centraliza todas as decisões relacionadas a CPF em um único ponto.
/// </summary>
public interface ICpfProtectionService
{
    /// <summary>
    /// Normaliza um CPF removendo caracteres não-numéricos e limitando a 11 dígitos.
    /// </summary>
    /// <param name="cpf">CPF com possíveis formatações (ex: "123.456.789-01")</param>
    /// <returns>CPF normalizado com apenas dígitos (ex: "12345678901")</returns>
    string Normalize(string cpf);

    /// <summary>
    /// Valida um CPF conforme configuração de feature flag.
    /// Se ValidationEnabled=true: valida pelo algoritmo modulo 11.
    /// Se ValidationEnabled=false: aceita qualquer CPF com 11 dígitos.
    /// </summary>
    /// <param name="cpf">CPF já normalizado (somente dígitos)</param>
    /// <returns>true se CPF é válido conforme configuração, false caso contrário</returns>
    bool Validate(string cpf);

    /// <summary>
    /// Mascara um CPF conforme configuração de feature flag.
    /// Se MaskEnabled=true: retorna "***45678901" (últimos 4 dígitos visíveis).
    /// Se MaskEnabled=false: retorna CPF completo "12345678901".
    /// </summary>
    /// <param name="cpf">CPF a ser mascarado</param>
    /// <returns>CPF mascarado ou completo conforme configuração</returns>
    string Mask(string cpf);

    /// <summary>
    /// Computa hash de um CPF para futura armazenagem.
    /// Nesta fase: apenas stub, retorna input.
    /// Preparando infraestrutura para HashEnabled flag.
    /// </summary>
    /// <param name="cpf">CPF a ser hashado</param>
    /// <returns>Hash do CPF (ou input se HashEnabled=false)</returns>
    string Hash(string cpf);

    /// <summary>
    /// Indica se validação de CPF está habilitada.
    /// </summary>
    bool IsValidationEnabled { get; }

    /// <summary>
    /// Indica se mascaramento de CPF está habilitado.
    /// </summary>
    bool IsMaskEnabled { get; }

    /// <summary>
    /// Indica se infraestrutura de hash de CPF está habilitada.
    /// </summary>
    bool IsHashEnabled { get; }
}
