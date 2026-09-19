using System.Globalization;

namespace Drangit.Web.Repositories;

/// <summary>
/// Texte disponible en français et/ou en anglais, avec repli sur l'autre langue.
/// </summary>
/// <remarks>
/// GitHub ne stocke qu'une seule description par dépôt, sans langue déclarée : le repli
/// garantit qu'un visiteur francophone voit l'anglais plutôt qu'un blanc quand la
/// traduction manque.
/// </remarks>
/// <param name="English">Version anglaise, ou <c>null</c> si absente.</param>
/// <param name="French">Version française, ou <c>null</c> si absente.</param>
public sealed record LocalizedText(string? English, string? French)
{
    /// <summary>Texte vide dans les deux langues.</summary>
    public static LocalizedText Empty { get; } = new(null, null);

    /// <summary>Indique qu'aucune des deux langues n'est renseignée.</summary>
    public bool IsEmpty => Normalize(English) is null && Normalize(French) is null;

    /// <summary>Construit un texte disponible uniquement en anglais.</summary>
    public static LocalizedText FromEnglish(string? english) => new(english, null);

    /// <summary>
    /// Retourne la version correspondant à <paramref name="culture"/>, ou l'autre langue
    /// si elle manque, ou <c>null</c> si aucune n'est renseignée.
    /// </summary>
    public string? For(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var (preferred, fallback) = culture.TwoLetterISOLanguageName is "fr"
            ? (French, English)
            : (English, French);

        return Normalize(preferred) ?? Normalize(fallback);
    }

    /// <summary>
    /// Superpose <paramref name="overrides"/> à ce texte : chaque langue renseignée dans
    /// <paramref name="overrides"/> remplace la nôtre, les autres sont conservées.
    /// </summary>
    /// <remarks>
    /// C'est ainsi qu'une traduction française rédigée localement s'ajoute à la description
    /// anglaise du dépôt sans l'effacer.
    /// </remarks>
    public LocalizedText OverlaidWith(LocalizedText? overrides) =>
        overrides is null
            ? this
            : new LocalizedText(
                Normalize(overrides.English) ?? English,
                Normalize(overrides.French) ?? French);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
