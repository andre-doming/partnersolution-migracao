using FluentValidation;

namespace Partner.Api.Features.Companies;

public sealed class CompanyListRequestValidator : AbstractValidator<CompanyListRequest>
{
    public CompanyListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.Active)
            .Must(x => x is null || x is "S" or "N")
            .WithMessage("Active must be 'S' or 'N'.");
    }
}

public sealed class CompanyUpsertRequestValidator : AbstractValidator<CompanyUpsertRequest>
{
    public CompanyUpsertRequestValidator()
    {
        RuleFor(x => x.Cnpj)
            .NotEmpty()
            .Must(CompanyCnpjValidator.IsValid)
            .WithMessage("CNPJ is invalid.");

        RuleFor(x => x.TradeName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CorporateName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ManagerName).NotEmpty().MaximumLength(120);
    }
}

internal static class CompanyCnpjValidator
{
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        return new string(raw.Where(char.IsDigit).ToArray());
    }

    public static bool IsValid(string? raw)
    {
        var cnpj = Normalize(raw);
        if (cnpj.Length != 14)
        {
            return false;
        }

        if (cnpj.Distinct().Count() == 1)
        {
            return false;
        }

        var numbers = cnpj.Select(c => c - '0').ToArray();
        var firstWeights = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var secondWeights = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var firstDigit = ComputeDigit(numbers.Take(12).ToArray(), firstWeights);
        var secondDigit = ComputeDigit(numbers.Take(12).Append(firstDigit).ToArray(), secondWeights);

        return numbers[12] == firstDigit && numbers[13] == secondDigit;
    }

    private static int ComputeDigit(IReadOnlyList<int> values, IReadOnlyList<int> weights)
    {
        var sum = 0;
        for (var i = 0; i < values.Count; i++)
        {
            sum += values[i] * weights[i];
        }

        var mod = sum % 11;
        return mod < 2 ? 0 : 11 - mod;
    }
}

