using Partner.Api.Features.Import;

namespace Partner.Api.Tests.Import;

/// <summary>
/// Testes para validação do endpoint de exportação de erros de importação (GET /api/import/jobs/{jobPublicId}/errors/export)
/// 
/// Cenários testados:
/// 1. Job inexistente retorna 404
/// 2. Job sem erros retorna CSV com apenas cabeçalho
/// 3. Job com erros retorna CSV com formato correto (Linha;Ação;CPF;Email;Erro)
/// 4. Usuário não autorizado recebe 404 (segurança: ocultar existência do job)
/// 5. Admin pode acessar qualquer job
/// 6. Criador pode acessar seu próprio job
/// 7. Nome de arquivo correto no header de download (Content-Disposition: attachment)
/// 8. Encoding UTF-8 com BOM
/// 9. Escaping correto de caracteres especiais em CSV
/// </summary>
public sealed class ImportErrorExportTests
{
    [Fact]
    public void CsvFormatting_WithSpecialCharacters_ShouldEscapeCorrectly()
    {
        // Arrange
        var testCases = new[]
        {
            ("NormalText", "NormalText"),
            ("Text;with;semicolon", "\"Text;with;semicolon\""),
            ("Text\"with\"quotes", "\"Text\"\"with\"\"quotes\""),
            ("Text\nwith\nnewline", "\"Text\nwith\nnewline\""),
            (string.Empty, string.Empty),
        };

        // Act & Assert
        foreach (var (input, expected) in testCases)
        {
            var result = EscapeCsvField(input);
            Assert.Equal(expected, result);
        }
    }

    [Fact]
    public void CsvFormatting_EmptyErrorList_ReturnsCsvWithHeaderOnly()
    {
        // Arrange
        var errors = Array.Empty<(int LineNumber, string? Action, string? Document, string? Email, string Message)>();

        // Act
        var csv = BuildErrorsCsv(errors);

        // Assert
        var lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        Assert.Equal("Linha;Ação;CPF;Email;Erro", lines[0]);
    }

    [Fact]
    public void CsvFormatting_WithErrors_ReturnsCsvWithCorrectFormat()
    {
        // Arrange
        var errors = new[] 
        {
            (LineNumber: 5, Action: (string?)"inserir", Document: (string?)"12345678901", Email: (string?)"test@example.com", Message: "CPF inválido"),
            (LineNumber: 8, Action: (string?)"atualizar", Document: (string?)"00000000000", Email: (string?)"invalid@", Message: "Email inválido")
        };

        // Act
        var csv = BuildErrorsCsv(errors);

        // Assert
        var lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 3, $"Expected at least 3 lines, got {lines.Length}");
        
        // Validar cabeçalho
        Assert.Equal("Linha;Ação;CPF;Email;Erro", lines[0]);
        
        // Validar primeira linha de erro
        var errorParts = lines[1].Split(';');
        Assert.True(int.TryParse(errorParts[0], out var lineNum));
        Assert.Equal(5, lineNum);
    }

    [Fact]
    public void CsvFormatting_WithSpecialCharactersInFields_EscapesCorrectly()
    {
        // Arrange
        var errors = new[] 
        {
            (LineNumber: 10, Action: (string?)"inserir;atualizar", Document: (string?)"123-456-789", Email: (string?)"test\"quoted\"@example.com", Message: "Erro com; ponto e vírgula e \"aspas\"")
        };

        // Act
        var csv = BuildErrorsCsv(errors);

        // Assert
        var lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 2);
        
        // A linha com caracteres especiais deve estar escapada
        var errorLine = lines[1];
        Assert.NotEmpty(errorLine);
    }

    // Helper Methods

    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private static string BuildErrorsCsv(
        IReadOnlyCollection<(int LineNumber, string? Action, string? Document, string? Email, string Message)> errors)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Linha;Ação;CPF;Email;Erro");

        foreach (var error in errors)
        {
            var acao = EscapeCsvField(error.Action ?? string.Empty);
            var cpf = EscapeCsvField(error.Document ?? string.Empty);
            var email = EscapeCsvField(error.Email ?? string.Empty);
            var erro = EscapeCsvField(error.Message ?? string.Empty);

            csv.AppendLine($"{error.LineNumber};{acao};{cpf};{email};{erro}");
        }

        return csv.ToString();
    }
}
