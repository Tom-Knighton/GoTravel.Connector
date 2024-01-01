namespace GoTravel.Connector.Connections.Exceptions;

public class NoLineModesException: Exception
{
    private readonly string _operator;

    public override string Message => $"Failed to retrieve line modes for {_operator}";


    public NoLineModesException(): base() {}
    
    public NoLineModesException(string connection): base() {}
}