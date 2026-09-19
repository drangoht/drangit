# CLAUDE.md

> Ce fichier est chargé **à chaque session**. Il reste court volontairement.
> Le détail vit dans `.claude/rules/` (référence) et `.claude/skills/` (procédures chargées à la demande).

## Projet

- **Nom** : `Drangit`
- **Contexte métier** : site vitrine listant les dépôts publics de <https://github.com/drangoht>.
  Les données viennent de l'API GitHub au runtime, complétées par un fichier éditorial
  versionné. Bilingue FR/EN, déployé en conteneur Docker.
- **Solution** : `src/Drangit.slnx`
- **Cible** : .NET `10.0`

## Commandes

```bash
dotnet build src/Drangit.slnx -warnaserror
dotnet test  --solution src/Drangit.slnx
dotnet format src/Drangit.slnx --verify-no-changes               # avant tout commit

# Boucle rapide : tout sauf les tests d'intégration
dotnet test --solution src/Drangit.slnx -- --filter-not-trait "Category=Integration"

# Exécution locale — aucun secret n'est requis (ADR 0003) : sans jeton, l'API GitHub
# anonyme accorde 60 requêtes par heure, très au-delà de ce que le cache consomme.
cd src/Drangit.Web
dotnet user-secrets set "GitHub:Token" "<jeton>"   # facultatif, pour lever le quota
dotnet run

# Conteneur
docker build -t drangit:local .
docker compose up -d          # lit .env (voir .env.example)
```

## Repères dans le code

| Où | Quoi |
|---|---|
| `Repositories/` | modèle (`Repository`, `RepositorySlug`, `LocalizedText`), port `IRepositoryCatalog`, filtres |
| `Repositories/GitHub/` | **seul** endroit qui parle le vocabulaire GitHub — couche anti-corruption |
| `Repositories/Editorial/` | fusion avec `data/repositories.json` (sujets, descriptions, captures, traductions, dépôts masqués) |
| `Repositories/Snapshots/` | instantané de repli quand l'API est indisponible |
| `Localization/` | préfixe de langue du chemin (`CulturePath`, `CulturePrefix`) — ADR 0006 |
| `Resources/SharedResources*.resx` | traductions FR/EN — les deux fichiers portent les mêmes clés |
| `docs/adr/` | décisions structurantes, dont la dérogation mono-projet (ADR 0002) |

## Règles non négociables

1. **Aucun code de production sans test rouge préalable.** Cycle red → green → refactor.
2. **YAGNI > SOLID > patterns.** On n'abstrait pas sur une hypothèse. Règle de trois : la 1re occurrence on écrit, la 2e on duplique, la 3e on factorise.
3. **Ne jamais nommer un design pattern comme justification.** On décrit le problème, on cherche l'idiome .NET natif, on ne sort le pattern GoF qu'après. → skill `choix-pattern`.
4. **La règle de dépendance est absolue.** Le modèle du dossier `Repositories/` ne connaît ni HTTP, ni disque, ni ASP.NET. Une infraction = un test d'architecture qui casse, pas une discussion.
5. **Une interface appartient à son consommateur**, pas à son implémentation (DIP au sens de Robert C. Martin).
6. **Nullable + warnings-as-errors + analyzers.** Un warning est une erreur. Pas de `!` sans commentaire justifiant l'invariant.
7. **`CancellationToken` propagé partout.** Pas de `.Result`, pas de `.Wait()`, pas de `async void`.
8. **Refactoring et changement fonctionnel = deux commits.** Jamais mélangés.
9. **Ne pas exposer une entité de domaine dans un contrat public** (API, message, DTO de sortie).
10. **Si une règle ci-dessus doit être violée, l'écrire dans un ADR** (`docs/adr/`, skill `adr`), pas dans un commentaire.

## Spécifique à ce projet

- **Aucune donnée de compte ne sort du dossier `Repositories/GitHub/`.** `permissions`, `owner`,
  `private`, `visibility` ne franchissent pas la couche anti-corruption ; `private` et
  `visibility` n'y vivent que le temps de **refuser** un dépôt.
  `tests/…/Architecture/DonneesPriveesTests.cs` le vérifie — ne pas le contourner.
- **Le jeton d'API est facultatif** (ADR 0003). Le site doit continuer de fonctionner sans lui :
  ne jamais introduire de code qui l'exige. Un jeton vide n'est **pas** un jeton — il produirait
  un en-tête `Bearer ` vide, que GitHub refuse par un 401.
- **Un quota épuisé n'est pas une panne.** Il a son propre type (`GitHubRateLimitException`) et
  son propre message de journal, qui porte l'heure de réarmement.
- **Refit : `AliasAs` pour renommer un paramètre, jamais `Query`.** Avec `Query`, le générateur
  émet `perPage` au lieu de `per_page` : GitHub l'ignore, la page retombe à 30, et la pagination
  s'arrête en croyant avoir vu une page incomplète — sans aucune erreur.
- **L'illustration d'un dépôt se joue sur deux couches** (`RepositoryCover.razor`) : dessous,
  un repli dessiné qui **imite la carte de GitHub** — même fond clair, même titre
  « compte/dépôt » en haut à gauche, plus une pastille de langage ; dessus, la capture du
  fichier éditorial si elle existe, sinon la carte elle-même. Un `onerror` retire l'image qui
  n'a pas répondu. Le repli n'a d'intérêt que s'il **ne se remarque pas** : une tuile d'une
  autre facture au milieu de quarante-cinq se verrait plus qu'une image manquante.
- **La carte de GitHub est recadrée** (`.cover__image--framed` : élargie de 39 %, calée en
  **haut à gauche**). Deux défauts d'un coup : l'avatar du compte, incrusté en haut à droite,
  et la ligne de compteurs du bas, qu'un calage au centre tranchait en deux sur chaque
  vignette. Les proportions viennent de mesures sur les cartes réelles — avatar de 76,7 % à
  93,2 % de la largeur, colonne de titre jusqu'à 68 %, compteurs à partir de 72 % de la
  hauteur. **Toute retouche se vérifie à l'œil**, en recadrant une carte réelle et en la
  regardant, pas au calcul.
- **La vitrine n'affiche jamais la carte de GitHub** (`UsePreviewCard="false"`) : sa bande est
  bien plus large que haute, et le recadrage y couperait le titre de la carte en deux.
- **La carte de GitHub ne sort jamais du site.** Un aperçu de partage s'affiche chez autrui,
  où aucun recadrage ne s'applique : `og:image` prend la capture du fichier éditorial, ou à
  défaut `wwwroot/og-share.png`, l'image du site (`SeoHead.ShareImageUrl`). `ShowcaseImageUrl`
  ne rend donc qu'une capture, jamais la carte — `PreviewImageUrl` reste réservée aux pages.
- **Toute chaîne affichée passe par `IStringLocalizer<SharedResources>`.** Un texte en dur
  dans un composant casse la moitié du site.
- **Ajouter une clé de traduction, c'est l'ajouter dans les deux `.resx`.**
- **La langue est portée par le chemin** (`/en/…`, `/fr/…`, ADR 0006), détachée en
  `PathBase` à l'entrée du pipeline : aucune route `@page` ne la connaît. Conséquence à ne
  pas perdre de vue — **tout lien interne doit être relatif** (`href="repos/x"`), car
  `<base href>` porte le préfixe. Un `href="/repos/x"` absolu échappe à la base et renvoie
  le visiteur en anglais. Seuls les liens du sélecteur de langue sont absolus, à dessein.
- **Ajouter une page indexable, c'est l'ajouter au plan du site** (`Seo/Sitemap.cs`, une
  entrée par langue) **et lui donner un `SeoHead`**, qui pose sa canonique et ses `hreflang`
  réciproques. Une réciprocité manquante fait ignorer tout le groupe, sans rien signaler.
- **Le contenu éditorial s'applique en sortie du catalogue**, après le cache et après le repli
  sur instantané : une correction prend effet même quand GitHub est injoignable. Ne pas
  l'appliquer avant la mise en cache.
- Le rendu est **Blazor SSR statique** : pas d'interactivité côté client, les formulaires
  sont de vrais `<form>` HTML (GET pour les filtres).

## Réflexes attendus

| Situation | Faire |
|---|---|
| Je vais créer une interface / une abstraction | skill `choix-pattern` |
| Je vais implémenter un comportement | skill `tdd` |
| Je viens de modifier du code | skill `revue-clean-code` |
| Le code sent mauvais mais marche | skill `refactoring` |
| Choix structurant / arbitrage | skill `adr` |
| Feature complète | skill `nouvelle-feature` |

## Attitude

- Si une demande est ambiguë ou si l'exigence sent le sur-design, **le dire avant d'écrire du code**.
- Proposer la solution la plus simple qui passe les tests, puis mentionner l'évolution possible — sans l'implémenter.
- Pas de commentaire qui paraphrase le code. Le commentaire explique le **pourquoi**, jamais le **quoi**.
- Ne jamais désactiver un test ou un analyzer pour faire passer la CI.

## Référence détaillée (à charger si besoin)

@.claude/rules/00-principes.md

Les autres fichiers de `.claude/rules/` ne sont **pas** importés automatiquement (coût de contexte).
Les lire à la demande : `10-solid.md`, `20-clean-code.md`, `30-design-patterns.md`,
`40-architecture.md`, `50-tests.md`, `60-csharp.md`, `70-async-perf.md`.
