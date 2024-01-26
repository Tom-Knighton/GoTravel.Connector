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
        #region Line Modes
        services.AddKeyedTransient<IGenericLinesService, TfLLineService>(ConnectionOperator.TfL);
        #endregion
        services.AddTransient<IConnectorModeService, ConnectorModeService>();


        #region Stop Points
        services.AddKeyedTransient<IGenericStopPointService, TfLStopPointService>(ConnectionOperator.TfL);
        #endregion
        services.AddTransient<IConnectorStopService, ConnectorStopService>();

        #region General Arrivals
        services.AddKeyedTransient<IGenericGeneralArrivalService, TfLArrivalService>(ConnectionOperator.TfL);
        #endregion
        services.AddTransient<IConnectorGeneralArrivalService, ConnectorGeneralArrivalService>();
        
        #region Journey
        services.AddKeyedTransient<IGenericJourneyService, TflJourneyService>(ConnectionOperator.TfL);
        #endregion
        
        
        return services;
    }
}