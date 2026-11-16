namespace Homework2.Application.Exceptions;

public sealed class AppValidationException : Exception
{
    public AppValidationException(string message)
        : this(new Dictionary<string, string[]> { ["request"] = [message] })
    {
    }

    public AppValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}