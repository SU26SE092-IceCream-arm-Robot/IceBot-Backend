using Application.Sync.Ingestion.Commands;
using Application.Sync.Ingestion.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Sync;

public static class SyncModule
{
    public static IServiceCollection AddSyncModule(this IServiceCollection services)
    {
        services.AddScoped<IngestProductionEventCommandHandler>();
        services.AddScoped<IngestProductionEventsBatchCommandHandler>();
        services.AddScoped<IngestEdgeStateSummariesCommandHandler>();
        services.AddScoped<GetProductionEventCheckpointQueryHandler>();
        services.AddScoped<ListSyncDeadLettersQueryHandler>();
        services.AddScoped<GetSyncDeadLetterQueryHandler>();
        services.AddScoped<ResolveSyncDeadLetterCommandHandler>();
        services.AddScoped<IgnoreSyncDeadLetterCommandHandler>();
        services.AddScoped<RetrySyncDeadLetterCommandHandler>();
        return services;
    }
}
