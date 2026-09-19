# ADR-0005 — Refit pour le transport HTTP vers GitHub

- **Statut** : accepté
- **Date** : 2026-09-17

## Contexte

`GitHubClient` pourrait faire deux choses : parler HTTP (construire la requête, poser l'en-tête
`Authorization`, désérialiser, vérifier le code de statut) et traduire la réponse de GitHub en
`Repository`. Le premier bloc est de la mécanique sans décision ; le second porte toutes les
règles du site.

Cette cohabitation a un coût concret : chaque test de traduction — licence, sujets, slug,
démonstration — devrait monter un `HttpClient`, un handler de test et des
`JsonSerializerOptions` pour atteindre trois lignes de logique.

Le projet n'a **qu'un seul** appel sortant, vers **un seul** point d'accès.

## Décision

Le transport passe par **Refit** (`Refit.HttpClientFactory`).

- `IGitHubApi` déclare le contrat HTTP — la route, les en-têtes, la forme de la réponse — et
  rien de plus. C'est un *Humble Object* : aucune décision ne s'y prend.
- `GitHubClient` consomme `IGitHubApi` et ne contient plus que la traduction. Il reste la
  couche anti-corruption : c'est le seul type qui connaît le vocabulaire de GitHub.
- `GitHubRefit` porte le câblage (adresse de base, délai d'expiration, `User-Agent`,
  résilience) et les réglages de sérialisation. La convention de nommage en serpent de GitHub
  est une connaissance du contrat externe : elle n'a pas à remonter dans `Program.cs`, qui se
  contente d'un `AddGitHubCatalog()`.

## Deux pièges, tous deux payés une fois

**Le type d'exception de Refit ne sort pas du dossier.** Refit signale les codes HTTP d'erreur
par `ApiException`, qui **ne dérive pas** de `HttpRequestException`. Laisser ce type remonter
ferait sortir le SDK de la couche anti-corruption *et* priverait le catalogue de son repli sur
instantané, qui guette `HttpRequestException`. `GitHubClient` traduit donc, systématiquement.

**`AliasAs`, et non `Query`, pour renommer un paramètre.** Avec `[Query("per_page")]`, le
générateur envoie `perPage=100`, que GitHub ignore : la page retombe à sa taille par défaut de
30, et la pagination s'arrête sur la première page en la croyant incomplète — un compte de
soixante dépôts n'en montrerait que trente, sans aucune erreur. Un test vérifie la chaîne de
requête réellement émise.

## Conséquences

**Positives** — la traduction se teste sans serveur HTTP ; le contrat externe se lit d'un coup
d'œil ; la résilience (reprise avec back-off et disjoncteur) est déclarée en une ligne.

**Négatives** — une dépendance NuGet et un générateur de source de plus ; des pièges propres au
générateur, qu'on ne découvre qu'en observant la requête émise.

**À noter** — `AddRefitGeneratedClient`, et non `AddRefitClient` : le second résout un
constructeur de requêtes par réflexion, absent du paquet depuis Refit 15.
