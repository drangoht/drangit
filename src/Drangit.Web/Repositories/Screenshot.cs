namespace Drangit.Web.Repositories;

/// <summary>
/// Illustration d'un dépôt, décrite dans le fichier éditorial.
/// </summary>
/// <param name="Url">Adresse de l'image.</param>
/// <param name="Caption">Légende traduite, vide si le fichier éditorial n'en donne pas.</param>
public sealed record Screenshot(Uri Url, LocalizedText Caption);
