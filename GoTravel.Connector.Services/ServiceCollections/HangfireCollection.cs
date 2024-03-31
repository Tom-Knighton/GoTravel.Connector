using GoTravel.Connector.Services.Interfaces;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using HangfireBasicAuthenticationFilter;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GoTravel.Connector.Services.ServiceCollections;

public class HangfireJob
{
    public string Id { get; set; }
    public string? Cron { get; set; }
}

public static class HangfireCollection
{
    public static IServiceCollection AddHangfireJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var migrationOptions = new MongoMigrationOptions
        {
            MigrationStrategy = new DropMongoMigrationStrategy(),
            BackupStrategy = new CollectionMongoBackupStrategy()
        };
        var storageOptions = new MongoStorageOptions
        {
            MigrationOptions = migrationOptions,
            CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection
        };

        services.AddHangfire(cfg =>
        {
            cfg.UseSimpleAssemblyNameTypeSerializer();
            cfg.UseRecommendedSerializerSettings();
            cfg.UseMongoStorage(configuration["Host"], storageOptions);
        });

        services.AddHangfireServer();
        
        return services;
    }

    public static IApplicationBuilder UseHangfire(this IApplicationBuilder app, IConfiguration configuration)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireCustomBasicAuthenticationFilter
            {
                User = configuration["AdminUser"],
                Pass = configuration["AdminPassword"]
            } }
        });

        app.UseEndpoints(e => e.MapHangfireDashboard());

        var jobs = configuration.GetSection("Jobs").Get<List<HangfireJob>>()?.ToList() ?? new();
        foreach (var job in jobs)
        {
            var cron = !string.IsNullOrWhiteSpace(job.Cron) ? job.Cron : Cron.Never();
            Console.WriteLine($"Adding Hangfire Job '{job.Id}' with timing: '{cron}'");
            switch (job.Id)
            {
                case "FetchModesAndLines":
                    RecurringJob.AddOrUpdate<IConnectorModeService>(job.Id, x => x.FetchAndSendAllModesAndLines(default), cron);
                    break;
                case "FetchNonBusStopPoints":
                    RecurringJob.AddOrUpdate<IConnectorStopService>(job.Id, x => x.FetchAndSendNonBusStopPointUpdates(default), cron);
                    break;
                case "FetchBusStopPoints":
                    RecurringJob.AddOrUpdate<IConnectorStopService>(job.Id, x => x.FetchAndSendBusStopPointUpdates(default), cron);
                    break;
                case "FetchGeneralArrivals":
                    RecurringJob.AddOrUpdate<IConnectorGeneralArrivalService>(job.Id, x => x.FetchAllGeneralArrivals(default), cron);
                    break;
                case "FetchStopPointInfo":
                    RecurringJob.AddOrUpdate<IConnectorStopService>(job.Id, x => x.FetchAndSendStopPointInfo(default), cron);
                    break;
                case "FetchLineRoutes":
                    RecurringJob.AddOrUpdate<IConnectorModeService>(job.Id, x => x.FetchAndSendAllRouteStrings(default), cron);
                    break;
                default:
                    Console.WriteLine($"No hangfire job with name {job.Id}");
                    break;
            }
        }

        return app;
    }
}