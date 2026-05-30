using FluentValidation;

namespace Partner.Api.Features.Users;

public sealed class UserListRequestValidator : AbstractValidator<UserListRequest>
{
    public UserListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.Active)
            .Must(x => x is null || x is "S" or "N")
            .WithMessage("Active must be 'S' or 'N'.");
    }
}

public sealed class UserUpsertRequestValidator : AbstractValidator<UserUpsertRequest>
{
    public UserUpsertRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Login).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Password)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Length is >= 8 and <= 120)
            .WithMessage("Password must have between 8 and 120 chars when provided.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);

        RuleFor(x => x.CompanyIds)
            .Must(x => x is not null)
            .WithMessage("CompanyIds is required.");

        RuleFor(x => x.FunctionIds)
            .Must(x => x is not null)
            .WithMessage("FunctionIds is required.");
    }
}

