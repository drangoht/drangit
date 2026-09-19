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
- **La carte de GitHub n'est affichée nulle part** (ADR 0009). Elle réécrit en anglais le nom,
  la description et les compteurs déjà affichés sous la vignette, elle impose son fond clair et
  l'avatar du compte, et elle coûte une requête vers un tiers par vignette. `og:image` prend la
  capture du fichier éditorial, ou à défaut `wwwroot/og-share.png` (`SeoHead.ShareImageUrl`).
- **L'illustration d'un dépôt se joue sur deux couches** (`RepositoryCover.razor`) : dessous,
  une vignette **dessinée** — la teinte vient du langage, l'inclinaison du dégradé vient du nom,
  et le nom complet s'y écrit comme une ligne de terminal ; dessus, la capture du fichier
  éditorial si elle existe. Un `onerror` retire l'image qui n'a pas répondu. Sans cette
  vignette, les quarante et un dépôts sans capture laisseraient un trou dans la grille.
- **Deux teintes, pas une** (`RepositoryVisual`) : `AccentOf` colore le fond, `InkOf` écrit —
  le prompt et la pastille. Les couleurs de `linguist` sont faites pour un fond clair, et les
  plus sombres (PowerShell, Lua, Ruby) disparaissent sur la vignette : sous un seuil de
  luminance, `InkOf` les éclaircit. **Ajouter un langage à la table demande de regarder le
  résultat**, pas seulement de lire la valeur.
- **Pas de padding en pourcentage sur une tuile.** Il se calcule sur la largeur du *bloc
  conteneur*, pas sur celle de l'élément : sur la fiche, 7 % valaient 78 px dans une tuile de
  350 et n'y laissaient plus de place au titre.
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
- Le rendu est **Blazor SSR statique** : aucun composant interactif, aucun circuit, les
  formulaires sont de vrais `<form>` HTML (GET pour les filtres). Seule exception, écrite dans
  l'ADR 0008 : un script de `wwwroot/` peut **enrichir** ce qui fonctionne déjà sans lui.
  Le critère n'est pas la quantité de JavaScript, c'est ce qui disparaît quand il est absent —
  si c'est une fonction du site, le script est au mauvais endroit. Il ne connaît donc aucune
  règle métier : il présente et il navigue.
- **Le prompt a deux moitiés, et une seule connaît le vocabulaire.** `RepositoryCommand.Parse`
  (serveur, testé) traduit une ligne en filtre, en fiche ou en page ; `wwwroot/console.js`
  n'intercepte **que** les commandes dont la page contient déjà la sortie, et laisse partir
  tout le reste. **Ajouter une commande d'information, c'est ajouter un `<div data-output="…">`
  dans `Home.razor`** — le script n'a pas à le savoir, et le texte reste dans les `.resx`.
  Ajouter une commande qui filtre ou qui navigue, c'est un cas dans le parseur, avec son test.
- **`console.js` n'est couvert par aucun test** (ADR 0008) : toute retouche se vérifie dans le
  navigateur — `help`, `stats`, `eggs`, la complétion `Tab`, l'historique, et surtout qu'une
  commande de filtre **n'est pas** interceptée.

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
