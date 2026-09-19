namespace Drangit.Web.Repositories.GitHub;

internal sealed partial class GitHubClient
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "GitHub a renvoyé {ReceivedCount} dépôts pour {Login}, dont {ListedCount} exposés.")]
    private static partial void LogRepositoriesFetched(
        ILogger logger,
        string login,
        int receivedCount,
        int listedCount);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Quota d'API GitHub épuisé : le site sert son instantané jusqu'à {ResetsAt}.")]
    private static partial void LogRateLimited(ILogger logger, string resetsAt);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Critical,
        Message = "GitHub ne connaît pas le compte {Login} : vérifier la clé GitHub__Login.")]
    private static partial void LogAccountNotFound(ILogger logger, string login);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Le compte {Login} dépasse {MaxPages} pages de dépôts : la liste est tronquée.")]
    private static partial void LogPaginationTruncated(ILogger logger, string login, int maxPages);
}
