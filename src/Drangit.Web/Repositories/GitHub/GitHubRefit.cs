using System.Text.Json;
using Microsoft.Extensions.Options;
using Refit;

namespace Drangit.Web.Repositories.GitHub;

/// <summary>
/// Branche le client Refit vers GitHub et l'enveloppe de sa couche anti-corruption.
/// </summary>
/// <remarks>
/// Les réglages de sérialisation vivent ici et non dans le composition root : la convention
/// de nommage de GitHub est une connaissance du contrat externe, elle n'a pas à remonter
/// dans <c>Program.cs</c>. Les tests réutilisent <see cref="Settings"/> pour éprouver le
/// même câblage que la production.
/// </remarks>
internal static class GitHubRefit
{
    /// <summary>Réglages Refit du contrat GitHub.</summary>
    public static RefitSettings Settings { get; } =
        new(new SystemTextJsonContentSerializer(CreateSerializerOptions()));

    /// <summary>Enregistre l'appel HTTP à GitHub et le catalogue qui le traduit.</summary>
    public static IServiceCollection AddGitHubCatalog(this IServiceCollection services)
    {
        // AddRefitGeneratedClient, et non AddRefitClient : le second résout un constructeur
        // de requêtes par réflexion, absent du paquet depuis Refit 15. L'implémentation
        // générée à la compilation est la seule disponible ici.
        services.AddRefitGeneratedClient<IGitHubApi>(Settings)
            .ConfigureHttpClient((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<GitHubOptions>>().Value;

                client.BaseAddress = new Uri("https://api.github.com/");
                client.Timeout = options.Timeout;
                // GitHub rejette par un 403 toute requête sans agent utilisateur identifiable.
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Drangit/1.0 (+https://github.com/drangoht)");
            })
            // Reprise avec back-off et jitter, plus un disjoncteur : une lecture est
            // idempotente, la retenter est sans risque.
            .AddStandardResilienceHandler();

        services.AddTransient<IGitHubClient, GitHubClient>();

        return services;
    }

    private static JsonSerializerOptions CreateSerializerOptions() =>
        new(JsonSerializerDefaults.Web)
        {
            // GitHub nomme ses champs en serpent (`full_name`, `stargazers_count`). La
            // politique évite une centaine d'attributs JsonPropertyName sur le DTO, et
            // surtout le champ qu'on oublierait d'annoter — il arriverait nul sans rien dire.
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
}
