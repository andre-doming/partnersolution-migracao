using FluentValidation;
using FluentValidation.Results;
using Partner.Api.Features.Import;

namespace Partner.Api.Tests.Import;

public sealed class ImportValidationTests
{
    [Fact]
    public void ProcessSelectedRequest_WithoutLines_ShouldFailValidation()
    {
        var request = new ImportProcessSelectedRequest
        {
            CompanyId = 1,
            FileName = "sample.csv",
            SelectedLines = []
        };

        var errors = Validate(request);

        Assert.Contains(errors, e =>
            string.Equals(e.PropertyName, "selectedLines", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProcessSelectedRequest_WithInvalidLine_ShouldFailValidation()
    {
        var request = new ImportProcessSelectedRequest
        {
            CompanyId = 1,
            FileName = "sample.csv",
            SelectedLines =
            [
                new ImportSelectedLineRequest
                {
                    LineNumber = 2,
                    Action = string.Empty,
                    FirstName = "",
                    LastName = "",
                    Document = "",
                    Email = ""
                }
            ]
        };

        var errors = Validate(request);

        Assert.Contains(errors, e =>
            string.Equals(e.PropertyName, "selectedLines", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyCollection<ValidationFailure> Validate(ImportProcessSelectedRequest request)
    {
        var errors = new List<ValidationFailure>();

        if (request.CompanyId <= 0)
        {
            errors.Add(new ValidationFailure("companyId", "companyId is required and must be greater than zero."));
        }

        if (request.SelectedLines.Count == 0)
        {
            errors.Add(new ValidationFailure("selectedLines", "At least one line must be selected."));
            return errors;
        }

        foreach (var line in request.SelectedLines)
        {
            if (line.LineNumber <= 1
                || string.IsNullOrWhiteSpace(line.Action)
                || string.IsNullOrWhiteSpace(line.FirstName)
                || string.IsNullOrWhiteSpace(line.LastName)
                || string.IsNullOrWhiteSpace(line.Document)
                || string.IsNullOrWhiteSpace(line.Email))
            {
                errors.Add(new ValidationFailure("selectedLines", "Each selected line must include action, name, document and email."));
                break;
            }
        }

        return errors;
    }
}

