namespace GoTravel.Connector.Domain.Enums;

public enum ConnectionOperator
{
    TfL
}

public static class ConnectionOperatorExtensions
{
    public static string ToName(this ConnectionOperator v)
    {
        return v switch
        {
            ConnectionOperator.TfL => "TfL",
            _ => ""
        };
    }
}