using System.Text.Json;
using Partner.Api.Features.Import;
using Partner.Api.Infrastructure.Import;

namespace Partner.Api.Tests.Import;

public sealed class ImportAsyncFoundationTests
{
    [Fact]
    public void NotificationMessages_ShouldMatchSpec()
    {
        var completed = BuildNotification("Completed", "clientes.csv");
        Assert.NotNull(completed);
        Assert.Equal("Importação concluída", completed?.Title);
        Assert.Equal("A importação do arquivo clientes.csv foi concluída com sucesso.", completed?.Message);

        var withErrors = BuildNotification("CompletedWithErrors", "clientes.csv");
        Assert.NotNull(withErrors);
        Assert.Equal("Importação concluída com erros", withErrors?.Title);
        Assert.Equal("A importação do arquivo clientes.csv foi concluída com erros. Consulte os detalhes.", withErrors?.Message);

        var failed = BuildNotification("Failed", "clientes.csv");
        Assert.NotNull(failed);
        Assert.Equal("Importação falhou", failed?.Title);
        Assert.Equal("A importação do arquivo clientes.csv falhou. Consulte os detalhes.", failed?.Message);

        var cancelled = BuildNotification("Cancelled", "clientes.csv");
        Assert.NotNull(cancelled);
        Assert.Equal("Importação cancelada", cancelled?.Title);
        Assert.Equal("A importação do arquivo clientes.csv foi cancelada.", cancelled?.Message);
    }

    private static (string Title, string Message)? BuildNotification(string status, string fileName)
    {
        return status switch
        {
            "Completed" => (
                "Importação concluída",
                $"A importação do arquivo {fileName} foi concluída com sucesso."),
            "CompletedWithErrors" => (
                "Importação concluída com erros",
                $"A importação do arquivo {fileName} foi concluída com erros. Consulte os detalhes."),
            "Failed" => (
                "Importação falhou",
                $"A importação do arquivo {fileName} falhou. Consulte os detalhes."),
            "Cancelled" => (
                "Importação cancelada",
                $"A importação do arquivo {fileName} foi cancelada."),
            _ => null
        };
    }
    [Fact]
    public void ComputeSha256_ShouldMatchExpectedHash()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("abc");

        var hash = ImportFileStorage.ComputeSha256(bytes);

        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", hash);
    }

    [Fact]
    public void BuildPublishCommand_ShouldSerializeMessage()
    {
        var options = new ImportRabbitMqOptions
        {
            Exchange = "partner.events"
        };

        var request = new ImportJobCreateRequest
        {
            PublicId = Guid.NewGuid(),
            Feature = "clients-csv",
            FileName = "file.csv",
            FilePath = "imports/1/file.csv",
            FileHashSha256 = "hash",
            CompanyId = 1,
            CreatedByUserId = 10,
            Status = ImportJobStatus.Queued,
            StartedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        var message = ImportJobMessageFactory.Create(42, request, "corr-1");
        var command = ImportJobPublisher.BuildPublishCommand(options, message);
        var parsed = JsonSerializer.Deserialize<ImportJobMessage>(command.Body.Span);

        Assert.Equal(options.Exchange, command.Exchange);
        Assert.Equal(ImportJobPublisher.JobQueuedRoutingKey, command.RoutingKey);
        Assert.NotNull(parsed);
        Assert.Equal(message.MessageId, parsed!.MessageId);
        Assert.Equal(message.CorrelationId, parsed.CorrelationId);
        Assert.Equal(message.Job.PublicId, parsed.Job.PublicId);
    }

    [Fact]
    public void JobStateMachine_ShouldAllowQueuedToRunning()
    {
        var allowed = ImportJobStateMachine.CanTransition(ImportJobStatus.Queued, ImportJobStatus.Running);

        Assert.True(allowed);
    }

    [Fact]
    public void JobRequestFactory_ShouldCreateQueuedJob()
    {
        var request = ImportJobRequestFactory.CreateQueued(
            Guid.NewGuid(),
            "clients-csv",
            "file.csv",
            "imports/1/file.csv",
            "hash",
            1,
            10,
            DateTime.UtcNow,
            "corr-1");

        Assert.Equal(ImportJobStatus.Queued, request.Status);
        Assert.Equal("clients-csv", request.Feature);
        Assert.Equal(1, request.CompanyId);
    }
}