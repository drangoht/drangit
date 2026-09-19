using Microsoft.Extensions.Logging;

namespace Drangit.Tests.Fakes;

/// <summary>
/// Journal de test qui retient ce qui a été écrit.
/// </summary>
/// <remarks>
/// Un journal est parfois le seul effet observable d'un comportement voulu : le
/// signalement d'une entrée éditoriale orpheline ne change rien à la page rendue, mais
/// c'est précisément lui qui évite de croire un dépôt masqué alors qu'il ne l'est pas.
/// </remarks>
internal sealed class ListeDeMessages : ILogger<Drangit.Web.Repositories.RepositoryCatalog>
{
    public List<string> Messages { get; } = [];

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        Messages.Add(formatter(state, exception));
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
