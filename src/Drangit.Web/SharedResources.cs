namespace Drangit.Web;

/// <summary>
/// Type marqueur des chaînes traduites du site.
/// </summary>
/// <remarks>
/// Il vit dans le namespace racine — et non dans <c>Drangit.Web.Resources</c> — parce
/// que le localisateur compose le nom de ressource comme
/// <c>{ResourcesPath}.{nom du type sans le préfixe de l'assembly}</c> : un type placé dans
/// <c>.Resources</c> ferait chercher <c>Resources.Resources.SharedResources</c>, qui n'existe pas.
/// Les traductions elles-mêmes vivent dans <c>Resources/SharedResources.{culture}.resx</c>.
/// </remarks>
public sealed class SharedResources;
