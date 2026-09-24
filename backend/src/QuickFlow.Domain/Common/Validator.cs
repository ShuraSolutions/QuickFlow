namespace QuickFlow.Domain.Common;

/// <summary>
/// Collects rule violations per field and throws a single
/// <see cref="DomainValidationException"/> when any were found.
/// </summary>
public sealed class Validator
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool HasErrors => _errors.Count > 0;

    public Validator Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
        {
            list = new List<string>();
            _errors[field] = list;
        }
        list.Add(message);
        return this;
    }

    public Validator Required(string field, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Add(field, $"{field} is required.");
        if (value.Trim().Length > maxLength)
            Add(field, $"{field} must be at most {maxLength} characters.");
        return this;
    }

    public Validator Optional(string field, string? value, int maxLength)
    {
        if (value is not null && value.Trim().Length > maxLength)
            Add(field, $"{field} must be at most {maxLength} characters.");
        return this;
    }

    public Validator DefinedEnum<TEnum>(string field, TEnum value) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            Add(field, $"{field} must be one of: {string.Join(", ", Enum.GetNames<TEnum>())}.");
        return this;
    }

    public Validator When(bool condition, string field, string message)
    {
        if (condition) Add(field, message);
        return this;
    }

    public void ThrowIfInvalid()
    {
        if (HasErrors)
            throw new DomainValidationException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
    }

    /// <summary>Trims a value and turns blank strings into null.</summary>
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
