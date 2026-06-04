namespace Partner.Api.Infrastructure.Import;

public sealed class ImportStorageOptions
{
    public const string SectionName = "Import";

    public string StorageRoot { get; init; } = "imports";
}