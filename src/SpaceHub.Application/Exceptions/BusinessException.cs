namespace SpaceHub.Application.Exceptions;

public sealed class BusinessException : Exception
{
    public FailureKind Kind
    {
        get;
    }
    public IReadOnlyDictionary<string, string[]> Errors
    {
        get;
    }

    public BusinessException(FailureKind kind, string message,
        IReadOnlyDictionary<string, string[]>? errors = null) : base(message)
    {
        Kind = kind;
        if (errors == null)
        {
            Errors = new Dictionary<string, string[]>();
        }
        else
        {
            Errors = errors;
        }
    }
}
