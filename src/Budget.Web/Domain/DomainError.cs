namespace Budget.Web.Domain;

/// <summary>Represents an error raised by domain logic when an operation violates a domain rule.</summary>
public class DomainError : Exception
{
    /// <summary>Initializes a new <see cref="DomainError"/> with the given message.</summary>
    /// <param name="message">The message describing the domain rule violation.</param>
    public DomainError(string message)
        : base(message)
    {
    }
}
