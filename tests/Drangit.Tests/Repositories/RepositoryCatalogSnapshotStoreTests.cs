using Drangit.Tests.Builders;
using Drangit.Web.Repositories.Snapshots;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Drangit.Tests.Repositories;

public sealed class RepositoryCatalogSnapshotStoreTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "drangit-snapshot", Guid.NewGuid().ToString("N"));

    private RepositoryCatalogSnapshotStore CreateStore(string? directory = null) =>
        new(
            Options.Create(new SnapshotOptions { Directory = directory ?? _directory }),
            NullLogger<RepositoryCatalogSnapshotStore>.Instance);

    [Fact]
    public async Task LoadAsync_QuandAucunInstantaneNExiste_RetourneNull()
    {
        (await CreateStore().LoadAsync(CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task SaveAsync_PuisLoadAsync_RestitueLeCatalogue()
    {
        var store = CreateStore();
        var depot = new RepositoryBuilder()
            .WithName("Algorithme-de-Huffman")
            .WithStars(3)
            .WithLicense("MIT")
            .WithDemo("https://huffman.example/")
            .Build();

        await store.SaveAsync([depot], CancellationToken.None);

        var relu = (await store.LoadAsync(CancellationToken.None)).ShouldNotBeNull().ShouldHaveSingleItem();

        // Le slug se construit par une fabrique : sans son convertisseur, il ne se relit pas.
        relu.Slug.Value.ShouldBe("algorithme-de-huffman");
        relu.Name.ShouldBe("Algorithme-de-Huffman");
        relu.Stars.ShouldBe(3);
        relu.License.ShouldBe("MIT");
        relu.HomepageUrl!.ToString().ShouldBe("https://huffman.example/");
    }

    [Fact]
    public async Task SaveAsync_RemplaceLInstantanePrecedent()
    {
        var store = CreateStore();

        await store.SaveAsync([new RepositoryBuilder().WithName("premier").Build()], CancellationToken.None);
        await store.SaveAsync([new RepositoryBuilder().WithName("second").Build()], CancellationToken.None);

        (await store.LoadAsync(CancellationToken.None))
            .ShouldNotBeNull()
            .ShouldHaveSingleItem()
            .Name.ShouldBe("second");
    }

    [Fact]
    public async Task LoadAsync_QuandLInstantaneEstTronque_RetourneNullSansLever()
    {
        var store = CreateStore();
        await store.SaveAsync([new RepositoryBuilder().Build()], CancellationToken.None);

        var fichier = Path.Combine(_directory, "repositories-snapshot.json");
        await File.WriteAllTextAsync(fichier, "[{ \"name\": ", CancellationToken.None);

        // Un instantané est un confort : le perdre ne doit pas dégrader la requête en cours.
        (await store.LoadAsync(CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task SaveAsync_QuandLeRepertoireEstInaccessible_NeLevePas()
    {
        // Sur un volume en lecture seule, l'écriture échoue : le site doit continuer de
        // servir la réponse qu'il vient pourtant d'obtenir.
        var chemin = Path.Combine(Path.GetTempPath(), "drangit-snapshot", Guid.NewGuid().ToString("N"));

        // Le dossier parent est créé explicitement : sans cela, ce test ne passe que si un
        // autre l'a créé avant lui. Il passait sous Windows et échouait en CI, au gré de
        // l'ordre d'exécution.
        Directory.CreateDirectory(Path.GetDirectoryName(chemin)!);
        await File.WriteAllTextAsync(chemin, "ceci est un fichier, pas un répertoire", CancellationToken.None);

        try
        {
            var store = CreateStore(chemin);

            await Should.NotThrowAsync(
                () => store.SaveAsync([new RepositoryBuilder().Build()], CancellationToken.None));
        }
        finally
        {
            File.Delete(chemin);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
