namespace GoTravel.Connector.Connections.Exceptions;

public class NoLinesException: Exception
{
    private string _operator;
    private string _mode;
    
    public NoLinesException(): base() {}

    public NoLinesException(string connection, string mode) : base()
    {
        _operator = connection;
        _mode = mode;
    }
}