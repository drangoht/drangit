namespace Drangit.Web.Repositories;

/// <summary>
/// Une valeur proposée en filtre, avec le nombre de dépôts qu'elle retient.
/// </summary>
/// <remarks>
/// Le compte n'est pas décoratif : sans lui, un visiteur clique sur un critère sans savoir
/// s'il va obtenir vingt dépôts ou un seul, et un catalogue dont les sujets sont rares donne
/// l'impression d'un filtre cassé. Affiché, il transforme la barre de filtres en carte du
/// contenu.
/// </remarks>
/// <param name="Value">Valeur du critère, telle qu'elle s'écrit dans l'URL.</param>
/// <param name="Count">Nombre de dépôts du catalogue qui la portent.</param>
public sealed record RepositoryFacet(string Value, int Count);
