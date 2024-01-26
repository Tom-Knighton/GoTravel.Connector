namespace GoTravel.Connector.Domain.Exceptions;

public class ConnectionDoesntExistException: Exception
{
    public string ConnectionName { get; private set; }
    
    public ConnectionDoesntExistException(string connection) : base($"Connection '{connection}' doesn't exist")
    {
        ConnectionName = connection;
    }
    
    public ConnectionDoesntExistException(string connectionName, Exception innerException)
        : base($"Connection '{connectionName}' doesn't exist", innerException)
    {
        ConnectionName = connectionName;
    }

}