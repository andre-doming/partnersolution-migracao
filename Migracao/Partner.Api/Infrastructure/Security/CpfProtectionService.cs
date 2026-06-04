using Microsoft.Extensions.Options;

namespace Partner.Api.Infrastructure.Security;

/// <summary>
/// Implementação do serviço centralizado de proteção de CPF.
/// Todas as operações respeitam as feature flags configuradas.
/// </summary>
public sealed class CpfProtectionService : ICpfProtectionService
{
    private readonly CpfOptions _options;

    public CpfProtectionService(IOptions<CpfOptions> options)
    {
        _options = options.Value;
    }

    public bool IsValidationEnabled => _options.ValidationEnabled;
    public bool IsMaskEnabled => _options.MaskEnabled;
    public bool IsHashEnabled => _options.HashEnabled;

    public string Normalize(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return string.Empty;
        }

        var digits = new string(cpf.Where(char.IsDigit).ToArray());
        if (digits.Length > 11)
        {
            digits = digits[..11];
        }

        return digits;
    }

    public bool Validate(string cpf)
    {
        // Se validação está desabilitada, aceita qualquer CPF com 11 dígitos
        if (!_options.ValidationEnabled)
        {
            return cpf.Length == 11;
        }

        // Caso contrário, valida pelo algoritmo
        return IsValidCpf(cpf);
    }

    public string Mask(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return string.Empty;
        }

        // Se mascaramento está desabilitado, retorna CPF completo
        if (!_options.MaskEnabled)
        {
            return cpf;
        }

        // Caso contrário, mascara: mostra apenas últimos 4 dígitos
        if (cpf.Length <= 4)
        {
            return new string('*', cpf.Length);
        }

        return new string('*', cpf.Length - 4) + cpf[^4..];
    }

    public string Hash(string cpf)
    {
        // Nesta fase: apenas infraestrutura preparada
        // Não altera banco, não migra dados
        // Se HashEnabled=true: stub retorna input (preparando para futura implementação)
        // Se HashEnabled=false: retorna input normalmente

        if (!_options.HashEnabled)
        {
            return cpf;
        }

        // TODO (FUTURO): Implementar hash real quando migrar para armazenagem hashada
        // Por enquanto, apenas retorna input
        return cpf;
    }

    /// <summary>
    /// Valida um CPF pelo algoritmo modulo 11 dos dígitos verificadores.
    /// </summary>
    private static bool IsValidCpf(string cpf)
    {
        if (cpf.Length != 11)
        {
            return false;
        }

        // Rejeita CPF com todos os dígitos iguais
        if (cpf.Distinct().Count() == 1)
        {
            return false;
        }

        var numbers = cpf.Select(c => c - '0').ToArray();
        var sum = 0;

        // Validar primeiro dígito verificador
        for (var i = 0; i < 9; i++)
        {
            sum += numbers[i] * (10 - i);
        }

        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;
        if (numbers[9] != digit1)
        {
            return false;
        }

        // Validar segundo dígito verificador
        sum = 0;
        for (var i = 0; i < 10; i++)
        {
            sum += numbers[i] * (11 - i);
        }

        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;
        return numbers[10] == digit2;
    }
}
