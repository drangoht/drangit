# Design patterns — usage en .NET

Catalogue de référence : <https://refactoring.guru/fr/design-patterns>

## Règle d'entrée

Un pattern est un **vocabulaire pour nommer une solution déjà présente**, pas un plan de
construction. Séquence obligatoire :

1. Décrire le problème **sans nommer de pattern**.
2. Vérifier YAGNI et la règle de trois. Deux implémentations réelles minimum.
3. Chercher **l'idiome .NET natif** (tableau ci-dessous). La plupart des patterns GoF
   comblent des manques de C++/Java 1.0 que C# a comblés depuis dans le langage.
4. Seulement alors, écrire le pattern — et le nommer dans le code (`PricingStrategy`,
   `OrderRepository`) pour que le lecteur reconnaisse la structure.

## Ce que le langage et le framework font déjà

| Pattern GoF | Idiome .NET | Écrire le pattern quand même si… |
|---|---|---|
| **Singleton** | `services.AddSingleton<T>()` | **jamais**. Le singleton statique GoF est un anti-pattern (état global, non testable, non substituable). |
| **Factory Method** | Conteneur DI, `ActivatorUtilities`, méthode statique `Create` | La construction dépend d'une donnée d'exécution que le conteneur ignore. |
| **Abstract Factory** | Rare. Modèle canonique : `IHttpClientFactory` | Familles d'objets réellement corrélées (multi-tenant, multi-provider). |
| **Builder** | Object initializers, `record` + `with`, `ImmutableArray.Builder` | Invariants complexes à la construction, ou **test data builders** (usage n°1, très recommandé). |
| **Prototype** | `record` + `with`, `MemberwiseClone` | Presque jamais. |
| **Adapter** | — | **Souvent justifié** : frontière avec un SDK tiers, couche anti-corruption. |
| **Decorator** | `Scrutor` : `.Decorate<IPricer, CachingPricer>()` | Cross-cutting (cache, retry, log, métriques) sur un port. Excellent usage. |
| **Composite** | — | Arbres : règles métier imbriquées, arbres d'expressions. |
| **Facade** | — | Simplifier un sous-système externe bavard, et le rendre mockable. |
| **Proxy** | `Lazy<T>`, intercepteurs (`Castle.DynamicProxy`), `DispatchProxy` | Rarement à la main. |
| **Flyweight** | `string` interning, `ArrayPool<T>`, `ReadOnlyMemory<T>` | Optimisation mesurée seulement. |
| **Bridge** | Composition + DI | Presque jamais explicitement. |
| **Chain of Responsibility** | Middleware ASP.NET Core, `IPipelineBehavior` (MediatR) | Utiliser le pipeline existant plutôt que réécrire la chaîne. |
| **Command** | `record` de requête + handler | Le socle de CQRS. Bon usage. |
| **Iterator** | `IEnumerable<T>` + `yield return`, `IAsyncEnumerable<T>` | **Ne jamais l'écrire à la main.** |
| **Mediator** | Attention : MediatR est un **dispatcher**, pas le Mediator GoF | Voir la mise en garde plus bas. |
| **Memento** | Snapshot, event sourcing | Undo/redo, reconstitution d'état. |
| **Observer** | `event`, `IObservable<T>`, `Channel<T>`, domain events | Domain events : très bon usage. |
| **State** | — | Quand un `switch (statut)` est **dupliqué** dans plusieurs méthodes. Machine à états explicite. |
| **Strategy** | `Func<TIn,TOut>`, interface + `AddKeyedScoped` | Deux algorithmes réels et interchangeables. Sinon : une méthode. |
| **Template Method** | — | **Préférer Strategy** (composition > héritage). Acceptable pour des classes de base de tests. |
| **Visitor** | `ExpressionVisitor` de la BCL, pattern matching exhaustif | Parcours d'AST, compilateurs, moteurs de règles. Coûteux ailleurs. |

## Patterns d'architecture applicative fréquents

- **Repository** — utile **par agrégat**, avec des méthodes d'intention
  (`FindPendingPaymentsFor(clientId, ct)`). Voir l'anti-pattern générique plus bas.
- **Unit of Work** — `DbContext` **est** déjà une Unit of Work. N'en réécrire une que si
  l'on doit coordonner plusieurs sources.
- **Specification** — filtres métier composables et testables sans base. Attention à ce
  qui est traduisible par EF Core : une spécification qui ne s'exprime pas en
  `Expression<Func<T,bool>>` bascule silencieusement en évaluation côté client.
- **Outbox** — publication de message et écriture en base dans **la même transaction**,
  relais asynchrone. Obligatoire dès qu'un handler écrit en base **et** publie sur un bus.
- **Result / Either** — pour les échecs métier attendus, en complément (pas en
  remplacement) des exceptions pour les défaillances techniques.

## Anti-patterns — à refuser explicitement

| Anti-pattern | Pourquoi | À la place |
|---|---|---|
| `IRepository<T>` générique sur EF Core | `DbContext` = UoW, `DbSet<T>` = repository. On rajoute une couche qui **appauvrit** l'API (perte de `Include`, projections, `AsNoTracking`) sans rien isoler. | Repository métier par agrégat, ou `DbContext` directement dans le handler. |
| Service Locator (`IServiceProvider` injecté) | Les dépendances disparaissent de la signature. Le test compile puis explose à l'exécution. | Injection par constructeur. |
| Modèle de domaine anémique | Entités = sacs de propriétés, règles éparpillées dans des `*Service`. C'est du procédural déguisé. | Invariants et comportements **dans** l'agrégat. (Anémique = acceptable pour du CRUD assumé, à écrire dans un ADR.) |
| `BaseService`, `BaseController`, `BaseEntity<T>` fourre-tout | Héritage pour partager du code = couplage maximal, LSP violé d'office. | Composition, méthodes d'extension, ou rien. |
| Classes statiques `*Helper` / `*Utils` | Non substituables, non testables, aimants à responsabilités. | Méthodes d'extension pures, ou un service injecté si effet de bord. |
| `DateTime.Now`, `Guid.NewGuid()`, `new Random()` en dur | Non déterministe → tests instables. | `TimeProvider` (.NET 8+), port dédié. |
| Injection de 8+ dépendances | Signal de violation du SRP, pas un problème de DI. | Découper la classe. |
| Pattern appliqué « pour l'exercice » | Coût d'indirection sans contrepartie. | Ne pas l'écrire. |

## Mise en garde : MediatR / dispatchers

MediatR est utile pour matérialiser des cas d'usage (Command/Query) et brancher des
comportements transverses (validation, transaction, log) via `IPipelineBehavior`.

Il devient nuisible quand il sert de **couche d'indirection systématique** : un handler
qui envoie une requête à un autre handler produit une pile d'appels intraçable, sans
typage de la dépendance, et un `Ctrl+F` pour trouver qui appelle quoi. Règle : un
contrôleur/endpoint envoie **une** requête ; un handler appelle des services typés, pas
d'autres handlers.

## Formulation attendue dans une revue

Ne pas écrire : « ajoutons une Factory ici ».
Écrire : « ces 3 branches instancient des variantes selon `TypePaiement`, et la même
sélection est dupliquée dans 2 autres fichiers. Une résolution keyed dans le conteneur
supprime la duplication ; ça coûte une interface `IPaymentProcessor` et une ligne
d'enregistrement par variante. »
