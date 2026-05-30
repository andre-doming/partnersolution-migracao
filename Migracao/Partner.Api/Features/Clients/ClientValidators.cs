using FluentValidation;

namespace Partner.Api.Features.Clients;

public sealed class ClientListRequestValidator : AbstractValidator<ClientListRequest>
{
    public ClientListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.Active)
            .Must(x => x is null || x is "S" or "N")
            .WithMessage("Active must be 'S' or 'N'.");
    }
}

public sealed class ClientUpsertRequestValidator : AbstractValidator<ClientUpsertRequest>
{
    public ClientUpsertRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(120);

        RuleFor(x => x.Document)
            .NotEmpty()
            .Must(ClientDocumentNormalizer.IsValid)
            .WithMessage("Document is invalid.");

        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Gender).MaximumLength(40);
        RuleFor(x => x.BirthDate).MaximumLength(20);
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Department).MaximumLength(120);
        RuleFor(x => x.Role).MaximumLength(120);
    }
}

internal static class ClientDocumentNormalizer
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
        var value = Normalize(raw);
        if (value.Length is not 11 and not 14)
        {
            return false;
        }

        return value.Distinct().Count() > 1;
    }
}

