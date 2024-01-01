using GoTravel.Connector.Connections.TfL.Services;
using GoTravel.Connector.Domain.Enums;
using GoTravel.Connector.Domain.Interfaces;
using GoTravel.Connector.Services.Interfaces;
using GoTravel.Connector.Services.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GoTravel.Connector.Services.ServiceCollections;

public static class ConnectionsCollection
{
    public static IServiceCollection RegisterConnectionImplementations(this IServiceCollection services)
    {

        services.AddKeyedTransient<IGenericLinesService, TfLLineService>(ConnectionOperator.TfL);


        services.AddTransient<IConnectorModeService, ConnectorModeService>();

        return services;
    }
}