namespace Partner.Api.Infrastructure.Import;

public static class ImportJobMessageFactory
{
    public static ImportJobMessage Create(int internalJobId, Partner.Api.Features.Import.ImportJobCreateRequest request, string correlationId)
    {
        return new ImportJobMessage
        {
            MessageId = request.PublicId.ToString(),
            CorrelationId = correlationId,
            Job = new ImportJobPayload
            {
                PublicId = request.PublicId,
                InternalId = internalJobId,
                Feature = request.Feature,
                CompanyId = request.CompanyId,
                CreatedByUserId = request.CreatedByUserId,
                FileName = request.FileName,
                FilePath = request.FilePath,
                FileHashSha256 = request.FileHashSha256,
                QueuedAtUtc = request.CreatedAtUtc
            }
        };
    }
}