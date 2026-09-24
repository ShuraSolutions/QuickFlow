namespace QuickFlow.Domain.Common;

/// <summary>Base type for errors raised by business rules.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

/// <summary>One or more fields break a business rule (maps to HTTP 400).</summary>
public sealed class DomainValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public DomainValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }

    public DomainValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = new[] { message } }) { }
}

/// <summary>The requested resource does not exist (maps to HTTP 404).</summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string resource, object key)
        : base($"{resource} '{key}' was not found.") { }
}

/// <summary>The request conflicts with the current state, e.g. a duplicate (maps to HTTP 409).</summary>
public sealed class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}
