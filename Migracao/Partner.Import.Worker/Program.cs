using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Import;
using Partner.Api.Infrastructure.Integrations.Vtex;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<ImportRabbitMqOptions>(builder.Configuration.GetSection(ImportRabbitMqOptions.SectionName));
builder.Services.Configure<ImportStorageOptions>(builder.Configuration.GetSection(ImportStorageOptions.SectionName));
builder.Services.Configure<VtexOptions>(builder.Configuration.GetSection(VtexOptions.SectionName));
builder.Services.Configure<VtexRabbitMqOptions>(builder.Configuration.GetSection(VtexRabbitMqOptions.SectionName));

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IConfiguration>()
        .GetSection(ImportRabbitMqOptions.SectionName)
        .Get<ImportRabbitMqOptions>() ?? new ImportRabbitMqOptions();
    return new ImportRabbitMqConnectionFactory(options);
});

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IConfiguration>()
        .GetSection(VtexRabbitMqOptions.SectionName)
        .Get<VtexRabbitMqOptions>() ?? new VtexRabbitMqOptions();
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<VtexRabbitMqConnectionFactory>();
    return new VtexRabbitMqConnectionFactory(options, logger);
});

builder.Services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<ImportJobRepository>();

builder.Services.AddHostedService<Partner.Import.Worker.ImportWorker>();

var host = builder.Build();
host.Run();
