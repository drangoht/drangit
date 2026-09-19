using System.Globalization;
using System.Net;
using Drangit.Tests.Fakes;
using Drangit.Web.Repositories.GitHub;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Refit;
using Shouldly;

namespace Drangit.Tests.Repositories;

public sealed class GitHubClientTests
{
    // Extrait fidèle de la réponse documentée de GET /users/{login}/repos, réduit aux
    // champs que le site lit — plus « private » et « visibility », qui ne servent qu'à
    // refuser ce qui ne serait pas public.
    private const string ReponseGitHub = """
    [
      {
        "id": 1296269,
        "name": "Algorithme-de-Huffman",
        "full_name": "drangoht/Algorithme-de-Huffman",
        "html_url": "https://github.com/drangoht/Algorithme-de-Huffman",
        "description": "Mise en pratique de l'algorithme de Huffman",
        "homepage": "https://huffman.example/",
        "language": "HTML",
        "topics": ["algorithms", " web "],
        "stargazers_count": 1,
        "forks_count": 2,
        "pushed_at": "2026-06-12T08:30:00Z",
        "created_at": "2021-03-04T10:00:00Z",
        "license": { "spdx_id": "MIT", "name": "MIT License" },
        "archived": false,
        "fork": false,
        "private": false,
        "visibility": "public"
      },
      {
        "id": 42,
        "name": "Blazorise",
        "full_name": "drangoht/Blazorise",
        "html_url": "https://github.com/drangoht/Blazorise",
        "description": "Component library",
        "homepage": "",
        "language": "C#",
        "topics": [],
        "stargazers_count": 0,
        "forks_count": 0,
        "pushed_at": "2022-01-27T00:00:00Z",
        "created_at": "2022-01-01T00:00:00Z",
        "license": { "spdx_id": "NOASSERTION", "name": "Other" },
        "archived": false,
        "fork": true,
        "private": false,
        "visibility": "public"
      },
      {
        "id": 99,
        "name": "notes-perso",
        "full_name": "drangoht/notes-perso",
        "html_url": "https://github.com/drangoht/notes-perso",
        "description": null,
        "homepage": null,
        "language": null,
        "topics": [],
        "stargazers_count": 0,
        "forks_count": 0,
        "pushed_at": "2026-01-01T00:00:00Z",
        "created_at": "2025-01-01T00:00:00Z",
        "license": null,
        "archived": true,
        "fork": false,
        "private": true,
        "visibility": "private"
      }
    ]
    """;

    private const string PageVide = "[]";

    // On branche le vrai client Refit sur un handler de test : la route, les en-têtes et la
    // sérialisation traversés ici sont exactement ceux de production.
    private static GitHubClient CreateClient(StubHttpMessageHandler handler, GitHubOptions? options = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        var api = RestService.ForGenerated<IGitHubApi>(http, GitHubRefit.Settings);

        return new GitHubClient(
            api,
            Options.Create(options ?? new GitHubOptions { Login = "drangoht" }),
            NullLogger<GitHubClient>.Instance);
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_EcarteLesDepotsPrives()
    {
        var client = CreateClient(StubHttpMessageHandler.ReturningJson(ReponseGitHub));

        var depots = await client.GetPublicRepositoriesAsync(CancellationToken.None);

        depots.ShouldNotContain(depot => depot.Name == "notes-perso");
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_EcarteLesBifurcationsParDefaut()
    {
        var client = CreateClient(StubHttpMessageHandler.ReturningJson(ReponseGitHub));

        var depots = await client.GetPublicRepositoriesAsync(CancellationToken.None);

        depots.ShouldHaveSingleItem().Name.ShouldBe("Algorithme-de-Huffman");
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_GardeLesBifurcationsQuandLaConfigurationLeDemande()
    {
        var client = CreateClient(
            StubHttpMessageHandler.ReturningJson(ReponseGitHub),
            new GitHubOptions { Login = "drangoht", IncludeForks = true });

        var depots = await client.GetPublicRepositoriesAsync(CancellationToken.None);

        depots.Select(depot => depot.Name).ShouldBe(["Algorithme-de-Huffman", "Blazorise"]);
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_TraduitLesChampsEnSerpentDeGitHub()
    {
        var client = CreateClient(StubHttpMessageHandler.ReturningJson(ReponseGitHub));

        var depot = (await client.GetPublicRepositoriesAsync(CancellationToken.None)).ShouldHaveSingleItem();

        depot.FullName.ShouldBe("drangoht/Algorithme-de-Huffman");
        depot.Stars.ShouldBe(1);
        depot.Forks.ShouldBe(2);
        depot.PushedAt.ShouldBe(new DateTimeOffset(2026, 6, 12, 8, 30, 0, TimeSpan.Zero));
        depot.License.ShouldBe("MIT");
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_NormaliseLesSujetsEtLeSlug()
    {
        var client = CreateClient(StubHttpMessageHandler.ReturningJson(ReponseGitHub));

        var depot = (await client.GetPublicRepositoriesAsync(CancellationToken.None)).ShouldHaveSingleItem();

        depot.Slug.Value.ShouldBe("algorithme-de-huffman");
        depot.Topics.ShouldBe(["algorithms", "web"]);
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_RetientLaDemoDeclareeParLeDepot()
    {
        var client = CreateClient(StubHttpMessageHandler.ReturningJson(ReponseGitHub));

        var depot = (await client.GetPublicRepositoriesAsync(CancellationToken.None)).ShouldHaveSingleItem();

        depot.HasDemo.ShouldBeTrue();
        depot.HomepageUrl!.ToString().ShouldBe("https://huffman.example/");
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_IgnoreUneLicenceQueGitHubNAPasSuAttribuer()
    {
        var client = CreateClient(
            StubHttpMessageHandler.ReturningJson(ReponseGitHub),
            new GitHubOptions { Login = "drangoht", IncludeForks = true });

        var fork = (await client.GetPublicRepositoriesAsync(CancellationToken.None))
            .Single(depot => depot.Name == "Blazorise");

        // « NOASSERTION » ne dirait rien au visiteur : mieux vaut ne rien afficher.
        fork.License.ShouldBeNull();
        // Un « homepage » vide n'est pas une démonstration.
        fork.HasDemo.ShouldBeFalse();
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_NeProposeAucuneImagePourUnDepotSansCapture()
    {
        var client = CreateClient(StubHttpMessageHandler.ReturningJson(ReponseGitHub));

        var depot = (await client.GetPublicRepositoriesAsync(CancellationToken.None)).ShouldHaveSingleItem();

        // Les seules images du site viennent du fichier éditorial (ADR 0009) : GitHub n'en
        // fournit aucune que le site accepte d'afficher.
        depot.ShowcaseImageUrl.ShouldBeNull();
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_DemandeLaPremierePageAuCompteConfigure()
    {
        var handler = StubHttpMessageHandler.ReturningJson(ReponseGitHub);

        await CreateClient(handler).GetPublicRepositoriesAsync(CancellationToken.None);

        var requete = handler.Requests.ShouldHaveSingleItem();
        requete.RequestUri!.AbsolutePath.ShouldBe("/users/drangoht/repos");
        requete.RequestUri.Query.ShouldContain("per_page=100");
        requete.RequestUri.Query.ShouldContain("sort=pushed");
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_SansJeton_NEnvoieAucunEnTeteDAutorisation()
    {
        var handler = StubHttpMessageHandler.ReturningJson(ReponseGitHub);

        await CreateClient(handler).GetPublicRepositoriesAsync(CancellationToken.None);

        // Un en-tête « Bearer » vide vaudrait un 401, là où l'absence d'en-tête vaut un
        // appel anonyme parfaitement valide (ADR 0003).
        handler.Requests.ShouldHaveSingleItem().Headers.Authorization.ShouldBeNull();
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_AvecJeton_LePasseEnEnTeteDAutorisation()
    {
        var handler = StubHttpMessageHandler.ReturningJson(ReponseGitHub);
        var client = CreateClient(handler, new GitHubOptions { Login = "drangoht", Token = "jeton-secret" });

        await client.GetPublicRepositoriesAsync(CancellationToken.None);

        var autorisation = handler.Requests.ShouldHaveSingleItem().Headers.Authorization.ShouldNotBeNull();
        autorisation.Scheme.ShouldBe("Bearer");
        autorisation.Parameter.ShouldBe("jeton-secret");
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_QuandUnJetonVideEstConfigure_AppelleEnAnonyme()
    {
        // Le déploiement écrit toutes les clés dans l'environnement, renseignées ou non :
        // un jeton absent arrive en chaîne vide.
        var handler = StubHttpMessageHandler.ReturningJson(ReponseGitHub);
        var client = CreateClient(handler, new GitHubOptions { Login = "drangoht", Token = "   " });

        await client.GetPublicRepositoriesAsync(CancellationToken.None);

        handler.Requests.ShouldHaveSingleItem().Headers.Authorization.ShouldBeNull();
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_SuitLaPaginationJusquALaPageIncomplete()
    {
        var pagePleine = "[" + string.Join(",", Enumerable.Range(1, 100).Select(Depot)) + "]";
        var handler = StubHttpMessageHandler.ReturningJsonSequence(pagePleine, ReponseGitHub, PageVide);

        var depots = await CreateClient(handler).GetPublicRepositoriesAsync(CancellationToken.None);

        handler.Requests.Count.ShouldBe(2);
        depots.Count.ShouldBe(101);
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_NeDepassePasLeNombreDePagesAutorise()
    {
        // Une réponse inattendue ne doit pas consommer le quota d'API en entier.
        var pagePleine = "[" + string.Join(",", Enumerable.Range(1, 100).Select(Depot)) + "]";
        var handler = StubHttpMessageHandler.ReturningJson(pagePleine);
        var client = CreateClient(handler, new GitHubOptions { Login = "drangoht", MaxPages = 2 });

        await client.GetPublicRepositoriesAsync(CancellationToken.None);

        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_QuandLeQuotaEstEpuise_LeveUneExceptionQuiLeDit()
    {
        var reset = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
        var handler = StubHttpMessageHandler.ReturningStatus(
            HttpStatusCode.Forbidden,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["x-ratelimit-remaining"] = "0",
                ["x-ratelimit-reset"] = reset.ToString(CultureInfo.InvariantCulture),
            });

        var exception = await Should.ThrowAsync<GitHubRateLimitException>(
            () => CreateClient(handler).GetPublicRepositoriesAsync(CancellationToken.None));

        exception.ResetsAt.ShouldBe(DateTimeOffset.FromUnixTimeSeconds(reset));
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_QuandLe403NEstPasUnQuota_LeSignaleCommeUnePanne()
    {
        // Un 403 couvre aussi les requêtes sans agent utilisateur et les comptes bloqués :
        // les confondre ferait annoncer une heure de réarmement qui n'existe pas.
        var handler = StubHttpMessageHandler.ReturningStatus(
            HttpStatusCode.Forbidden,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["x-ratelimit-remaining"] = "57" });

        await Should.ThrowAsync<HttpRequestException>(
            () => CreateClient(handler).GetPublicRepositoriesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetPublicRepositoriesAsync_QuandLeCompteEstInconnu_SignaleUnePanne()
    {
        // Le catalogue guette HttpRequestException pour se replier sur son instantané :
        // laisser remonter l'ApiException de Refit l'en priverait.
        var handler = StubHttpMessageHandler.ReturningStatus(HttpStatusCode.NotFound);

        await Should.ThrowAsync<HttpRequestException>(
            () => CreateClient(handler).GetPublicRepositoriesAsync(CancellationToken.None));
    }

    private static string Depot(int id) =>
        $$"""
        {
          "id": {{id}},
          "name": "depot-{{id}}",
          "full_name": "drangoht/depot-{{id}}",
          "html_url": "https://github.com/drangoht/depot-{{id}}",
          "topics": [],
          "pushed_at": "2020-01-01T00:00:00Z",
          "private": false,
          "visibility": "public",
          "fork": false,
          "archived": false
        }
        """;
}
