namespace GoTravel.Connector.Connections.Exceptions;

public class NoStopPointsException: Exception
{
    private string _operator;
    private string _mode;
    
    public NoStopPointsException(): base() {}

    public NoStopPointsException(string connection, string mode) : base()
    {
        _operator = connection;
        _mode = mode;
    }
}