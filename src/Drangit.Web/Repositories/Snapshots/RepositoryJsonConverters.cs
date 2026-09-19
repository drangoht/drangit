using System.Text.Json;
using System.Text.Json.Serialization;

namespace Drangit.Web.Repositories.Snapshots;

/// <summary>
/// Sérialise un <see cref="RepositorySlug"/> comme une simple chaîne.
/// </summary>
/// <remarks>
/// <see cref="RepositorySlug"/> se construit par une fabrique, pas par ses propriétés :
/// sans ce convertisseur, il ne se relit pas.
/// </remarks>
internal sealed class RepositorySlugJsonConverter : JsonConverter<RepositorySlug>
{
    public override RepositorySlug Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString()
            ?? throw new JsonException("Slug de dépôt absent dans l'instantané.");

        // Le slug est déjà normalisé à l'écriture ; l'identifiant de repli est sans objet ici.
        return RepositorySlug.FromName(value, repositoryId: 0);
    }

    public override void Write(Utf8JsonWriter writer, RepositorySlug value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStringValue(value.Value);
    }
}
