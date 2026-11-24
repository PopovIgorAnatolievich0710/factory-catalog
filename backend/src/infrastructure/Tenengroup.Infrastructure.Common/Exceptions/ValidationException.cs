namespace Tenengroup.Infrastructure.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string field, string message)
        : this(new Dictionary<string, List<string>> { [field] = [message] })
    {
    }

    public ValidationException(Dictionary<string, List<string>> validationMessages) : base("Validation failed")
    {
        ValidationMessages = validationMessages;
    }

    public Dictionary<string, List<string>> ValidationMessages { get; }
}
