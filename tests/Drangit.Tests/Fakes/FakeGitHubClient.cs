using Drangit.Web.Repositories;
using Drangit.Web.Repositories.GitHub;

namespace Drangit.Tests.Fakes;

/// <summary>
/// Doublure en mémoire du client GitHub. Un fake plutôt qu'un mock : les tests décrivent
/// ce que l'API renvoie, pas la séquence d'appels qu'on lui fait.
/// </summary>
internal sealed class FakeGitHubClient : IGitHubClient
{
    private readonly IReadOnlyList<Repository> _repositories;
    private readonly Exception? _failure;

    private FakeGitHubClient(IReadOnlyList<Repository> repositories, Exception? failure)
    {
        _repositories = repositories;
        _failure = failure;
    }

    public int CallCount { get; private set; }

    public static FakeGitHubClient Returning(params Repository[] repositories) =>
        new(repositories, failure: null);

    public static FakeGitHubClient Failing(Exception? failure = null) =>
        new([], failure ?? new HttpRequestException("GitHub est injoignable."));

    public Task<IReadOnlyList<Repository>> GetPublicRepositoriesAsync(CancellationToken cancellationToken)
    {
        CallCount++;

        return _failure is not null
            ? Task.FromException<IReadOnlyList<Repository>>(_failure)
            : Task.FromResult(_repositories);
    }
}
