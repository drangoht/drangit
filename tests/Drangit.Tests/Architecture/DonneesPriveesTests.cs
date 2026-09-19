using System.Reflection;
using Drangit.Web.Repositories;
using Drangit.Web.Repositories.GitHub;
using Shouldly;

namespace Drangit.Tests.Architecture;

/// <summary>
/// L'API GitHub renvoie, pour chaque dépôt, des informations qui relèvent du compte et non
/// de la vitrine : permissions du porteur du jeton, adresse électronique du propriétaire,
/// confidentialité. Un commentaire ne tient pas six mois ; ce test, oui.
/// </summary>
public sealed class DonneesPriveesTests
{
    private static readonly string[] TermesInterdits =
    [
        "permission",
        "owner",
        "email",
        "secret",
        "subscriber",
        "collaborator",
        "invitation",
    ];

    // Le jeton d'API a sa propre règle, plus bas : il ne se traite pas comme une donnée
    // de compte venue de l'API, et son unique porteur légitime est sa configuration.
    private const string TermeJeton = "token";

    [Fact]
    public void Repository_NExposeAucuneDonneeDeCompte()
    {
        var membresSuspects = MembresSuspectsDe(typeof(Repository));

        membresSuspects.ShouldBeEmpty(
            $"Repository est le contrat des vues : {string.Join(", ", membresSuspects)} exposerait des données de compte.");
    }

    [Fact]
    public void Repository_NePorteNiConfidentialiteNiVisibilite()
    {
        // Ces deux notions n'existent que le temps de refuser un dépôt, dans la couche
        // anti-corruption. Les faire entrer dans le modèle, c'est ouvrir la possibilité
        // qu'une page les affiche — ou qu'un dépôt privé y arrive et soit simplement marqué.
        var noms = typeof(Repository)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(propriete => propriete.Name)
            .ToArray();

        noms.ShouldNotContain("Private");
        noms.ShouldNotContain("Visibility");
    }

    [Fact]
    public void LeDtoDeLApiNeDeserialiseAucuneDonneeDeCompte()
    {
        // Ce qui n'est pas désérialisé ne peut pas être exposé par accident plus tard.
        var dto = typeof(GitHubClient).Assembly
            .GetType("Drangit.Web.Repositories.GitHub.GitHubRepositoryDto", throwOnError: true)!;

        var membresSuspects = MembresSuspectsDe(dto);

        membresSuspects.ShouldBeEmpty(
            $"Le DTO ne doit pas capter {string.Join(", ", membresSuspects)} depuis la réponse GitHub.");
    }

    [Fact]
    public void AucunTypePublicDuNamespaceRepositoriesNExposeDeDonneeDeCompte()
    {
        var membresSuspects = typeof(Repository).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace?.StartsWith("Drangit.Web.Repositories", StringComparison.Ordinal) == true)
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(propriete => $"{type.Name}.{propriete.Name}"))
            .Where(nom => EstUnTermeInterdit(nom.Split('.')[1]))
            .ToArray();

        membresSuspects.ShouldBeEmpty(string.Join(", ", membresSuspects));
    }

    [Fact]
    public void LeJetonDApi_NestExposeParAucunePropriete_HorsSaConfiguration()
    {
        // GitHubOptions le porte, c'est son rôle. Ailleurs, un jeton qui traverse le modèle
        // finit tôt ou tard dans un instantané sur disque ou dans une page.
        var porteurs = typeof(Repository).Assembly
            .GetExportedTypes()
            .Where(type => type != typeof(GitHubOptions))
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(propriete => $"{type.Name}.{propriete.Name}"))
            .Where(nom => nom.Contains(TermeJeton, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        porteurs.ShouldBeEmpty(string.Join(", ", porteurs));
    }

    private static string[] MembresSuspectsDe(Type type) =>
    [
        .. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(propriete => propriete.Name)
            .Where(EstUnTermeInterdit),
    ];

    private static bool EstUnTermeInterdit(string nomDeMembre) =>
        TermesInterdits.Any(terme => nomDeMembre.Contains(terme, StringComparison.OrdinalIgnoreCase));
}
