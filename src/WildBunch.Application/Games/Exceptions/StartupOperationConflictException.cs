namespace WildBunch.Application.Games.Exceptions;

public sealed class StartupOperationConflictException : InvalidOperationException
{
    public StartupOperationConflictException(string message) : base(message)
    {
    }
}
