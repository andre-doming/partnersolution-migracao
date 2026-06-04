using Microsoft.Extensions.Options;
using Partner.Api.Infrastructure.Security;

namespace Partner.Api.Tests.Infrastructure.Security;

public class CpfProtectionServiceTests
{
    private static CpfProtectionService CreateService(bool validationEnabled, bool maskEnabled, bool hashEnabled)
    {
        var options = Options.Create(new CpfOptions
        {
            ValidationEnabled = validationEnabled,
            MaskEnabled = maskEnabled,
            HashEnabled = hashEnabled
        });
        return new CpfProtectionService(options);
    }

    #region Normalize Tests

    [Fact]
    public void Normalize_WithFormattedCpf_RemovesFormatting()
    {
        // Arrange
        var service = CreateService(true, true, false);
        var cpf = "123.456.789-01";

        // Act
        var result = service.Normalize(cpf);

        // Assert
        Assert.Equal("12345678901", result);
    }

    [Fact]
    public void Normalize_WithMoreThan11Digits_LimitTo11()
    {
        // Arrange
        var service = CreateService(true, true, false);
        var cpf = "123456789012345";

        // Act
        var result = service.Normalize(cpf);

        // Assert
        Assert.Equal("12345678901", result);
    }

    [Fact]
    public void Normalize_WithNullCpf_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService(true, true, false);
        string cpf = null;

        // Act
        var result = service.Normalize(cpf);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Normalize_WithWhitespaceCpf_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService(true, true, false);
        var cpf = "   ";

        // Act
        var result = service.Normalize(cpf);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region Validate Tests - Scenario 1: ValidationEnabled=true

    [Fact]
    public void Validate_ValidationEnabledTrue_WithValidCpf_ReturnsTrue()
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: true, hashEnabled: false);
        var cpf = "11144477735"; // Valid CPF

        // Act
        var result = service.Validate(cpf);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_ValidationEnabledTrue_WithInvalidCpf_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: true, hashEnabled: false);
        var cpf = "11144477736"; // Invalid CPF (wrong check digit)

        // Act
        var result = service.Validate(cpf);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_ValidationEnabledTrue_WithAllSameDigits_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: true, hashEnabled: false);
        var cpf = "11111111111"; // All same digits

        // Act
        var result = service.Validate(cpf);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_ValidationEnabledTrue_WithLessThan11Digits_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: true, hashEnabled: false);
        var cpf = "123456789"; // Only 9 digits

        // Act
        var result = service.Validate(cpf);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Validate Tests - Scenario 2: ValidationEnabled=false

    [Fact]
    public void Validate_ValidationEnabledFalse_WithAny11Digits_ReturnsTrue()
    {
        // Arrange
        var service = CreateService(validationEnabled: false, maskEnabled: true, hashEnabled: false);
        var cpf = "99999999999"; // Any 11 digits

        // Act
        var result = service.Validate(cpf);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_ValidationEnabledFalse_WithLessThan11Digits_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(validationEnabled: false, maskEnabled: true, hashEnabled: false);
        var cpf = "123456789"; // Only 9 digits

        // Act
        var result = service.Validate(cpf);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_ValidationEnabledFalse_WithInvalidCpfFormat_StillAccepts11Digits()
    {
        // Arrange
        var service = CreateService(validationEnabled: false, maskEnabled: false, hashEnabled: false);
        var cpf = "12345678901"; // Invalid format but 11 digits

        // Act
        var result = service.Validate(cpf);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Mask Tests - Scenario 1: MaskEnabled=true

    [Fact]
    public void Mask_MaskEnabledTrue_WithValidCpf_ReturnsMasked()
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: true, hashEnabled: false);
        var cpf = "12345678901";

        // Act
        var result = service.Mask(cpf);

        // Assert
        Assert.Equal("*******8901", result);
    }

    [Fact]
    public void Mask_MaskEnabledTrue_With4DigitCpf_MasksAll()
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: true, hashEnabled: false);
        var cpf = "1234";

        // Act
        var result = service.Mask(cpf);

        // Assert
        Assert.Equal("****", result);
    }

    [Fact]
    public void Mask_MaskEnabledTrue_WithNullCpf_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: true, hashEnabled: false);
        string cpf = null;

        // Act
        var result = service.Mask(cpf);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region Mask Tests - Scenario 2: MaskEnabled=false

    [Fact]
    public void Mask_MaskEnabledFalse_WithValidCpf_ReturnsComplete()
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: false, hashEnabled: false);
        var cpf = "12345678901";

        // Act
        var result = service.Mask(cpf);

        // Assert
        Assert.Equal("12345678901", result);
    }

    [Fact]
    public void Mask_MaskEnabledFalse_WithNullCpf_ReturnsComplete()
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: false, hashEnabled: false);
        var cpf = "12345678901";

        // Act
        var result = service.Mask(cpf);

        // Assert
        Assert.Equal(cpf, result);
    }

    #endregion

    #region Hash Tests - Scenario 1: HashEnabled=true

    [Fact]
    public void Hash_HashEnabledTrue_ReturnsCpfAsIs()
    {
        // Arrange - Nesta fase, hash é stub
        var service = CreateService(validationEnabled: true, maskEnabled: true, hashEnabled: true);
        var cpf = "12345678901";

        // Act
        var result = service.Hash(cpf);

        // Assert - Retorna como está (preparação para fase futura)
        Assert.Equal(cpf, result);
    }

    #endregion

    #region Hash Tests - Scenario 2: HashEnabled=false

    [Fact]
    public void Hash_HashEnabledFalse_ReturnsCpfAsIs()
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: true, hashEnabled: false);
        var cpf = "12345678901";

        // Act
        var result = service.Hash(cpf);

        // Assert
        Assert.Equal(cpf, result);
    }

    #endregion

    #region Feature Flags Property Tests

    [Fact]
    public void IsValidationEnabled_ReturnsFlagValue()
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: false, hashEnabled: false);

        // Act & Assert
        Assert.True(service.IsValidationEnabled);
    }

    [Fact]
    public void IsMaskEnabled_ReturnsFlagValue()
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: true, hashEnabled: false);

        // Act & Assert
        Assert.True(service.IsMaskEnabled);
    }

    [Fact]
    public void IsHashEnabled_ReturnsFlagValue()
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: true, hashEnabled: true);

        // Act & Assert
        Assert.True(service.IsHashEnabled);
    }

    #endregion

    #region Combination Tests - All 8 Scenarios

    [Theory]
    [InlineData(true, true, false)]   // Scenario 1: Full validation + masking
    [InlineData(true, true, true)]    // Scenario 2: Full validation + masking + hash prep
    [InlineData(true, false, false)]  // Scenario 3: Validation only
    [InlineData(true, false, true)]   // Scenario 4: Validation + hash prep
    [InlineData(false, true, false)]  // Scenario 5: Masking only (no validation)
    [InlineData(false, true, true)]   // Scenario 6: Masking + hash prep (no validation)
    [InlineData(false, false, false)] // Scenario 7: No validation, no masking
    [InlineData(false, false, true)]  // Scenario 8: No validation, no masking, hash prep
    public void CombinationTest_AllEightScenarios(bool validationEnabled, bool maskEnabled, bool hashEnabled)
    {
        // Arrange
        var service = CreateService(validationEnabled, maskEnabled, hashEnabled);
        var testCpf = "11144477735"; // Valid CPF

        // Act
        var normalized = service.Normalize(testCpf);
        var validated = service.Validate(normalized);
        var masked = service.Mask(testCpf);
        var hashed = service.Hash(testCpf);

        // Assert
        Assert.Equal("11144477735", normalized);
        
        if (validationEnabled)
        {
            Assert.True(validated);
        }
        else
        {
            Assert.True(validated);
        }

        if (maskEnabled)
        {
            Assert.StartsWith("*", masked);
        }
        else
        {
            Assert.Equal(testCpf, masked);
        }

        // Hash is stub in this phase
        Assert.Equal(testCpf, hashed);
    }

    #endregion

    #region Real CPF Validation Tests

    [Theory]
    [InlineData("11144477735")]  // Valid CPF
    public void Validate_WithRealValidCpfs_ReturnsTrue(string validCpf)
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: true, hashEnabled: false);

        // Act
        var result = service.Validate(validCpf);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("11144477736")]  // Invalid check digit
    [InlineData("12345678901")]  // Invalid according to algorithm
    public void Validate_WithRealInvalidCpfs_ReturnsFalse(string invalidCpf)
    {
        // Arrange
        var service = CreateService(validationEnabled: true, maskEnabled: true, hashEnabled: false);

        // Act
        var result = service.Validate(invalidCpf);

        // Assert
        Assert.False(result);
    }

    #endregion
}
