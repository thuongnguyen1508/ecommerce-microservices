using Ardalis.GuardClauses;
using BuildingBlocks.Abstractions.Messaging.PersistMessage;
using BuildingBlocks.Core.Messaging.MessagePersistence;
using BuildingBlocks.Core.Web.Extenions.ServiceCollection;
using BuildingBlocks.Messaging.Persistence.Postgres.MessagePersistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.Messaging.Persistence.Postgres.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddPostgresMessagePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        services.AddValidatedOptions<MessagePersistenceOptions>(nameof(MessagePersistenceOptions));

        services.AddScoped<IMessagePersistenceConnectionFactory>(sp =>
        {
            var postgresOptions = sp.GetService<MessagePersistenceOptions>();
            Guard.Against.NullOrEmpty(postgresOptions?.ConnectionString);

            return new NpgsqlMessagePersistenceConnectionFactory(postgresOptions.ConnectionString);
        });

        services.AddDbContext<MessagePersistenceDbContext>((sp, options) =>
        {
            var postgresOptions = sp.GetRequiredService<MessagePersistenceOptions>();

            options.UseNpgsql(postgresOptions.ConnectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(postgresOptions.MigrationAssembly ??
                                              typeof(MessagePersistenceDbContext).Assembly.GetName().Name);
                sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }).UseSnakeCaseNamingConvention();
        });

        services.ReplaceScoped<IMessagePersistenceRepository, PostgresMessagePersistenceRepository>();
    }
}
