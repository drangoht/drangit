# Asynchrone & performance

## Règles dures

- **`async void` interdit**, sauf gestionnaire d'événement UI. Une exception y est
  non rattrapable et tue le processus.
- **Jamais `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`** dans du code applicatif :
  interblocage potentiel et thread bloqué. Asynchrone de bout en bout.
- **`CancellationToken` propagé jusqu'à l'appel d'I/O terminal**, en dernier paramètre,
  nommé `ct` ou `cancellationToken` (choisir et tenir). Le recevoir sans le passer plus
  bas est un bug silencieux.
- `ConfigureAwait(false)` dans le code de **bibliothèque**. Inutile dans ASP.NET Core
  (pas de `SynchronizationContext`) — ne pas polluer le code applicatif avec.
- Ne pas envelopper du code synchrone dans `Task.Run` pour « le rendre async » : cela
  déplace le blocage sur un thread du pool, ça ne l'élimine pas.

## Parallélisme

- I/O indépendantes → `await Task.WhenAll(...)`.
- Volume important → `Parallel.ForEachAsync` avec `MaxDegreeOfParallelism` **explicite**
  (sans plafond, on écrase la base ou l'API en aval).
- Producteur/consommateur → `System.Threading.Channels`.
- Attention à `Task.WhenAll` : il n'agrège pas les exceptions dans ce que voit `await`
  (seule la première remonte). Inspecter `task.Exception` si toutes comptent.

## ValueTask

Réservé à un chemin **chaud et mesuré** qui se termine souvent de façon synchrone (cache
hit). Contraintes : un `ValueTask` ne s'attend **qu'une seule fois**, ne se stocke pas, ne
se passe pas à `Task.WhenAll` sans `.AsTask()`. En cas de doute : `Task`.

## Streaming

`IAsyncEnumerable<T>` + `await foreach` pour ne pas matérialiser un gros jeu de résultats.
Propager le token avec `[EnumeratorCancellation]`.

## EF Core

- `AsNoTracking()` sur toute lecture non suivie de modification.
- **Projeter avec `Select`** vers le DTO : ne charger que les colonnes utiles.
- Lazy loading désactivé. `Include` explicite, en connaissant le coût du produit cartésien
  (`AsSplitQuery` quand il explose).
- Traquer le **N+1** : requête dans une boucle. Activer les logs de commandes en dev.
- Pas de `.ToList()` prématuré : il bascule la suite du filtrage côté client.
- Une écriture = une transaction = un agrégat. `SaveChangesAsync(ct)`.
- Migrations relues à la main avant commit.

## Mesurer avant d'optimiser

- **BenchmarkDotNet** pour toute affirmation de performance. Une intuition n'est pas une mesure.
- Métriques et traces (OpenTelemetry) avant de toucher au code : l'essentiel du temps est
  presque toujours dans une requête SQL ou un appel réseau, pas dans une allocation C#.
- Optimisations d'allocation (`Span<T>`, `ArrayPool<T>`, `stackalloc`, `struct`) : uniquement
  sur un chemin chaud profilé. Elles dégradent la lisibilité — le gain doit être chiffré.
- Cas particulier des boucles temps réel (jeu, traitement à haute fréquence) : les
  allocations par frame comptent réellement. La règle « mesurer » reste, le seuil change.

## Résilience réseau

- **Timeout explicite** sur chaque appel sortant. Le défaut de `HttpClient` (100 s) est
  inacceptable en production.
- `IHttpClientFactory` + Polly (`Microsoft.Extensions.Http.Resilience`) : retry avec
  back-off exponentiel **et jitter**, circuit breaker.
- Ne **jamais** retenter une opération non idempotente sans clé d'idempotence.
- Journaliser le nombre de tentatives et la latence, pas seulement l'échec final.
