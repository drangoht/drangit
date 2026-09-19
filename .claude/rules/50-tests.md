# Tests — TDD et discipline

## Les trois lois du TDD (Uncle Bob)

1. N'écrire aucun code de production tant qu'un test **qui échoue** ne l'exige.
2. N'écrire d'un test unitaire que ce qui suffit à le faire échouer (ne pas compiler = échouer).
3. N'écrire de code de production que ce qui suffit à faire passer le test.

Le cycle est **red → green → refactor**. L'étape *refactor* n'est pas optionnelle : c'est
là que le design émerge. Passer directement au test suivant produit du code qui marche et
qu'on ne peut plus faire évoluer.

## FIRST

| | |
|---|---|
| **F**ast | Millisecondes. Une suite unitaire lente n'est plus lancée. |
| **I**ndependent | Aucun ordre d'exécution, aucun état partagé, aucun fichier commun. |
| **R**epeatable | Même résultat hors ligne, sur le poste et en CI. |
| **S**elf-validating | Vert ou rouge. Pas d'inspection manuelle de logs. |
| **T**imely | Écrit juste avant le code de production. |

## Structure et nommage

- **AAA** : Arrange / Act / Assert, séparés par une ligne vide. Un seul *Act*.
- **Une seule raison d'échouer** par test (pas forcément une seule assertion — plusieurs
  assertions sur le même comportement sont acceptables).
- Convention de nommage, choisie une fois et tenue :
  `Methode_Contexte_ResultatAttendu` — `Debit_QuandSoldeInsuffisant_LeveInsufficientFunds`.
- Le test doit se lire comme une spécification. Si on doit lire l'implémentation pour
  comprendre le test, le test est mauvais.

## Quoi tester

- **Le comportement observable**, jamais l'implémentation. Un test qui casse à chaque
  refactoring sans changement de comportement teste la mauvaise chose — le supprimer ou le
  remonter d'un niveau.
- Les **règles métier** : couverture proche de l'exhaustif dans `Domain`.
- Les **cas limites** et les erreurs, autant que le chemin nominal.
- Ne pas tester : les getters/setters, le framework, le mapping trivial, les DTOs.

## Doublures de test

- **Ne mocker que ce qu'on possède.** Nos ports, oui. `DbContext`, `HttpClient`,
  `IConfiguration`, les types de la BCL : non.
- Pour HTTP : un `HttpMessageHandler` de test ou WireMock.Net, pas un mock de `HttpClient`.
- Pour la base : Testcontainers. **Pas** de provider InMemory d'EF Core — il ne respecte
  ni les contraintes, ni les transactions, ni la traduction LINQ ; un test vert dessus ne
  prouve rien.
- **Préférer un fake en mémoire à un mock** pour un port simple (`InMemoryOrderRepository`) :
  plus lisible, réutilisable, et il ne couple pas le test à la séquence d'appels.
- Un test qui vérifie l'**ordre des appels** sur un mock teste l'implémentation. Signal
  d'alerte.

## Données de test

- **Test data builders** : `new OrderBuilder().WithStatus(Paid).Build()`. Le builder porte
  les valeurs par défaut valides ; le test ne déclare que ce qui compte pour lui.
- Interdit : `new Order(null, 0, "", DateTime.Now, ...)` répété dans 40 tests. Un
  changement de constructeur devient un chantier.
- Pas de données de production, pas de données réelles de clients (RGPD).

## Tests d'intégration

- `WebApplicationFactory<Program>` + Testcontainers (PostgreSQL/SQL Server, RabbitMQ).
- Base **par exécution**, jamais partagée entre développeurs ou entre tests parallèles.
- Marqués (`[Trait("Category","Integration")]`) pour rester hors de la boucle rapide.
- Ils testent le **câblage** et l'I/O réel, pas les règles métier — celles-ci sont déjà
  couvertes en unitaire.

## Le code de test est du code de production

Même exigence de nommage et de lisibilité. Une nuance : **la duplication lisible est
préférable à l'abstraction opaque** dans les tests. Une hiérarchie de classes de base de
tests avec du setup implicite rend les échecs impossibles à diagnostiquer.

## Métriques

- La **couverture est un outil de diagnostic**, jamais un objectif. Un seuil imposé produit
  des tests sans assertion.
- Ce qui compte : **mutation testing** (Stryker.NET) sur le cœur métier. Un mutant survivant
  est un test qui n'assure rien.

## Interdits

- `Thread.Sleep` dans un test → attente conditionnelle avec timeout.
- Tests dépendants de l'ordre d'exécution ou d'un état statique.
- Assertions sur des messages de log.
- `[Fact(Skip = "…")]` sans numéro de ticket.
- Modifier un test pour le faire passer alors que le comportement attendu n'a pas changé.
- Commit avec la suite rouge.
