using System.Globalization;
using System.Text.Json;
using Drangit.Web.Repositories;

namespace Drangit.Web.Seo;

/// <summary>
/// Décrit un dépôt au vocabulaire schema.org, pour que les moteurs de recherche en fassent
/// une fiche plutôt qu'un lien bleu.
/// </summary>
public static class RepositoryStructuredData
{
    /// <summary>Construit le document JSON-LD d'un dépôt.</summary>
    /// <param name="repository">Dépôt décrit.</param>
    /// <param name="repositoryUrl">Adresse de sa fiche sur le site.</param>
    /// <param name="description">Description dans la langue de la page, si elle existe.</param>
    public static string ToJsonLd(Repository repository, Uri repositoryUrl, string? description)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(repositoryUrl);

        var document = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["@context"] = "https://schema.org",
            // SoftwareSourceCode, et non SoftwareApplication : ce que la page présente est du
            // code source consultable, pas un logiciel installable. Un dépôt qui publie une
            // démonstration reste du code — c'est la démonstration qui serait l'application.
            ["@type"] = "SoftwareSourceCode",
            ["name"] = repository.Name,
            ["url"] = repositoryUrl.ToString(),
            ["codeRepository"] = repository.HtmlUrl.ToString(),
        };

        // Une propriété absente décrit mieux le dépôt qu'une propriété vide, qui affirmerait
        // qu'il n'a ni description, ni langage, ni licence.
        if (!string.IsNullOrWhiteSpace(description))
        {
            document["description"] = description;
        }

        if (repository.Language is { } language)
        {
            document["programmingLanguage"] = language;
        }

        if (repository.License is { } license)
        {
            document["license"] = $"https://spdx.org/licenses/{license}";
        }

        if (repository.ShowcaseImageUrl is { } image)
        {
            document["image"] = image.ToString();
        }

        if (repository.CreatedAt is { } createdAt)
        {
            document["dateCreated"] = createdAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (repository.PushedAt is { } pushedAt)
        {
            document["dateModified"] = pushedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (repository.Topics.Count > 0)
        {
            document["keywords"] = string.Join(", ", repository.Topics);
        }

        return JsonSerializer.Serialize(document);
    }
}
