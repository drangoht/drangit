using Drangit.Tests.Builders;
using Drangit.Tests.Fakes;
using Drangit.Web.Repositories;
using Drangit.Web.Repositories.GitHub;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Drangit.Tests.Integration;

/// <summary>
/// Monte le site complet en mémoire, avec GitHub remplacé par une doublure.
/// </summary>
/// <remarks>
/// Ces tests vérifient le câblage — routage, localisation, codes de statut, antiforgery —
/// pas les règles métier, déjà couvertes en unitaire.
/// </remarks>
internal sealed class SiteFactory : WebApplicationFactory<Program>
{
    private readonly string _snapshotDirectory =
        Path.Combine(Path.GetTempPath(), "drangit-integration", Guid.NewGuid().ToString("N"));

    /// <summary>Dépôt complet : mis en vitrine, avec démonstration, sujets et capture.</summary>
    public static Repository SampleRepository { get; } = new RepositoryBuilder()
        .WithId(42)
        .WithName("Algorithme-de-Huffman")
        .WithLanguage("HTML")
        .WithTopics("algorithms", "web")
        .WithStars(7)
        .WithLicense("MIT")
        .WithDemo("https://huffman.example/")
        .WithDescription(new LocalizedText("Huffman coding, in the browser", "Le codage de Huffman, dans le navigateur"))
        .WithScreenshots("https://exemple.test/huffman.png")
        .Featured()
        .Build();

    /// <summary>Dépôt dépouillé : ni démonstration, ni sujet, ni licence, et archivé.</summary>
    public static Repository ArchivedRepository { get; } = new RepositoryBuilder()
        .WithId(43)
        .WithName("ASM-Gameboy-FirstProg")
        .WithLanguage("Assembly")
        .Archived()
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Production);

        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GitHub:Login"] = "drangoht",
                ["Snapshot:Directory"] = _snapshotDirectory,
                ["Site:Name"] = "Drangit",
                // Le contenu éditorial livré avec l'application désigne son propre dépôt en
                // vitrine et en masque d'autres : le laisser s'appliquer ferait dépendre ces
                // tests d'un fichier qu'on édite au fil de l'eau.
                ["Editorial:FilePath"] = "data/absent-des-tests.json",
            }));

        builder.ConfigureServices(services =>
        {
            // Aucun appel réseau ne doit partir d'une suite de tests.
            services.RemoveAll<IGitHubClient>();
            services.AddSingleton<IGitHubClient>(_ =>
                FakeGitHubClient.Returning(SampleRepository, ArchivedRepository));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(_snapshotDirectory))
        {
            Directory.Delete(_snapshotDirectory, recursive: true);
        }
    }
}
