using System.Globalization;

namespace Drangit.Web.Repositories.GitHub;

/// <summary>
/// GitHub a refusé la requête parce que le quota d'appels est épuisé.
/// </summary>
/// <remarks>
/// Ce cas mérite son propre type parce qu'il ne se traite pas comme une panne : réessayer
/// n'y changera rien avant <see cref="ResetsAt"/>, et le journal doit dire cette heure —
/// sans quoi une vitrine servie depuis son instantané pendant une heure ressemble à une
/// indisponibilité de GitHub.
/// </remarks>
public sealed class GitHubRateLimitException : Exception
{
    /// <summary>Construit l'exception à partir de l'heure de réarmement annoncée par GitHub.</summary>
    public GitHubRateLimitException(DateTimeOffset? resetsAt)
        : base(FormatMessage(resetsAt)) => ResetsAt = resetsAt;

    /// <inheritdoc />
    public GitHubRateLimitException()
        : this(resetsAt: null)
    {
    }

    /// <inheritdoc />
    public GitHubRateLimitException(string message)
        : base(message)
    {
    }

    /// <inheritdoc />
    public GitHubRateLimitException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Heure à laquelle le quota est réarmé, si GitHub l'a annoncée.</summary>
    public DateTimeOffset? ResetsAt { get; }

    private static string FormatMessage(DateTimeOffset? resetsAt) =>
        resetsAt is { } reset
            ? string.Format(
                CultureInfo.InvariantCulture,
                "Le quota d'appels à l'API GitHub est épuisé jusqu'à {0:u}.",
                reset)
            : "Le quota d'appels à l'API GitHub est épuisé.";
}
