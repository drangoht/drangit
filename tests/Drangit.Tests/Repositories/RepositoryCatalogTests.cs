using Drangit.Tests.Builders;
using Drangit.Tests.Fakes;
using Drangit.Web.Repositories;
using Drangit.Web.Repositories.Editorial;
using Drangit.Web.Repositories.GitHub;
using Drangit.Web.Repositories.Snapshots;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Drangit.Tests.Repositories;

public sealed class RepositoryCatalogTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "drangit-tests", Guid.NewGuid().ToString("N"));

    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    private RepositoryCatalogSnapshotStore CreateSnapshotStore() =>
        new(
            Options.Create(new SnapshotOptions { Directory = _directory }),
            NullLogger<RepositoryCatalogSnapshotStore>.Instance);

    private RepositoryCatalog CreateCatalog(
        IGitHubClient client,
        EditorialCatalog? editorial = null,
        RepositoryCatalogSnapshotStore? snapshotStore = null) =>
        new(
            client,
            editorial ?? EditorialCatalog.Empty,
            snapshotStore ?? CreateSnapshotStore(),
            _cache,
            Options.Create(new GitHubOptions { Login = "drangoht", CacheDuration = TimeSpan.FromMinutes(30) }),
            NullLogger<RepositoryCatalog>.Instance);

    [Fact]
    public async Task GetRepositoriesAsync_RetourneLesDepotsDeLApi()
    {
        var catalogue = CreateCatalog(
            FakeGitHubClient.Returning(new RepositoryBuilder().WithName("katas").Build()));

        var depots = await catalogue.GetRepositoriesAsync(CancellationToken.None);

        depots.ShouldHaveSingleItem().Slug.Value.ShouldBe("katas");
    }

    [Fact]
    public async Task GetRepositoriesAsync_AppliqueLeContenuEditorial()
    {
        var editorial = EditorialCatalog.FromJson(
            """{ "repositories": { "katas": { "featured": true, "topics": ["tdd"] } } }""");

        var catalogue = CreateCatalog(
            FakeGitHubClient.Returning(new RepositoryBuilder().WithName("katas").Build()),
            editorial);

        var depot = (await catalogue.GetRepositoriesAsync(CancellationToken.None)).ShouldHaveSingleItem();

        depot.IsFeatured.ShouldBeTrue();
        depot.Topics.ShouldBe(["tdd"]);
    }

    [Fact]
    public async Task GetRepositoriesAsync_EcarteLesDepotsMasquesParLeFichierEditorial()
    {
        var editorial = EditorialCatalog.FromJson("""{ "repositories": { "drangoht": { "hidden": true } } }""");

        var catalogue = CreateCatalog(
            FakeGitHubClient.Returning(
                new RepositoryBuilder().WithName("drangoht").Build(),
                new RepositoryBuilder().WithName("katas").Build()),
            editorial);

        var depots = await catalogue.GetRepositoriesAsync(CancellationToken.None);

        depots.ShouldHaveSingleItem().Name.ShouldBe("katas");
    }

    [Fact]
    public async Task GetRepositoriesAsync_SignaleUneEntreeEditorialeSansDepotCorrespondant()
    {
        var journal = new ListeDeMessages();
        var editorial = EditorialCatalog.FromJson(
            """{ "repositories": { "katas": {}, "depot-renomme": { "hidden": true } } }""");

        var catalogue = new RepositoryCatalog(
            FakeGitHubClient.Returning(new RepositoryBuilder().WithName("katas").Build()),
            editorial,
            CreateSnapshotStore(),
            _cache,
            Options.Create(new GitHubOptions()),
            journal);

        await catalogue.GetRepositoriesAsync(CancellationToken.None);

        // Sans ce signalement, un « hidden » posé sur un slug mal orthographié ne masque
        // rien et personne ne s'en aperçoit.
        journal.Messages.ShouldContain(message => message.Contains("depot-renomme", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetRepositoriesAsync_NAppelleLApiQuUneFoisPendantLaDureeDuCache()
    {
        var client = FakeGitHubClient.Returning(new RepositoryBuilder().Build());
        var catalogue = CreateCatalog(client);

        await catalogue.GetRepositoriesAsync(CancellationToken.None);
        await catalogue.GetRepositoriesAsync(CancellationToken.None);

        // Le quota d'API GitHub fait de cette économie plus qu'une optimisation.
        client.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetRepositoriesAsync_QuandLApiTombe_SertLeDernierInstantane()
    {
        var store = CreateSnapshotStore();
        var premier = CreateCatalog(
            FakeGitHubClient.Returning(new RepositoryBuilder().WithName("katas").Build()),
            snapshotStore: store);

        await premier.GetRepositoriesAsync(CancellationToken.None);

        using var cacheVide = new MemoryCache(new MemoryCacheOptions());
        var apresPanne = new RepositoryCatalog(
            FakeGitHubClient.Failing(),
            EditorialCatalog.Empty,
            store,
            cacheVide,
            Options.Create(new GitHubOptions()),
            NullLogger<RepositoryCatalog>.Instance);

        var depots = await apresPanne.GetRepositoriesAsync(CancellationToken.None);

        depots.ShouldHaveSingleItem().Name.ShouldBe("katas");
    }

    [Fact]
    public async Task GetRepositoriesAsync_QuandLeQuotaEstEpuise_SertAussiLInstantane()
    {
        var store = CreateSnapshotStore();
        await store.SaveAsync([new RepositoryBuilder().WithName("katas").Build()], CancellationToken.None);

        var catalogue = CreateCatalog(
            FakeGitHubClient.Failing(new GitHubRateLimitException(DateTimeOffset.UtcNow.AddMinutes(20))),
            snapshotStore: store);

        var depots = await catalogue.GetRepositoriesAsync(CancellationToken.None);

        depots.ShouldHaveSingleItem().Name.ShouldBe("katas");
    }

    [Fact]
    public async Task GetRepositoriesAsync_QuandLInstantaneEstServi_LeContenuEditorialSAppliqueQuandMeme()
    {
        var store = CreateSnapshotStore();
        await store.SaveAsync([new RepositoryBuilder().WithName("katas").Build()], CancellationToken.None);

        var editorial = EditorialCatalog.FromJson("""{ "repositories": { "katas": { "featured": true } } }""");
        var catalogue = CreateCatalog(FakeGitHubClient.Failing(), editorial, store);

        // C'est tout l'intérêt d'appliquer l'éditorial en sortie : une correction prend
        // effet même quand GitHub est injoignable.
        var depot = (await catalogue.GetRepositoriesAsync(CancellationToken.None)).ShouldHaveSingleItem();

        depot.IsFeatured.ShouldBeTrue();
    }

    [Fact]
    public async Task GetRepositoriesAsync_QuandLApiTombeSansInstantane_RetourneUneListeVide()
    {
        var catalogue = CreateCatalog(FakeGitHubClient.Failing());

        // Un site vide vaut mieux qu'une page d'erreur : le reste du site reste navigable.
        (await catalogue.GetRepositoriesAsync(CancellationToken.None)).ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRepositoriesAsync_NEcrasePasUnInstantanePeupleParUneReponseVide()
    {
        var store = CreateSnapshotStore();
        await store.SaveAsync([new RepositoryBuilder().WithName("katas").Build()], CancellationToken.None);

        var catalogue = CreateCatalog(FakeGitHubClient.Returning(), snapshotStore: store);

        await catalogue.GetRepositoriesAsync(CancellationToken.None);

        var instantane = await store.LoadAsync(CancellationToken.None);
        instantane.ShouldNotBeNull().ShouldHaveSingleItem().Name.ShouldBe("katas");
    }

    [Fact]
    public async Task GetRepositoriesAsync_QuandLeVisiteurSeDeconnecte_NeConfondPasCelaAvecUnePanne()
    {
        var catalogue = CreateCatalog(FakeGitHubClient.Returning(new RepositoryBuilder().Build()));
        using var annule = new CancellationTokenSource();
        await annule.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => catalogue.GetRepositoriesAsync(annule.Token));
    }

    public void Dispose()
    {
        _cache.Dispose();

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
