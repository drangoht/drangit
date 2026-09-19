# SOLID — appliqué à .NET

Pour chaque principe : la définition d'Uncle Bob, **le symptôme dans du code C# réel**,
la correction, et la contre-indication (quand l'appliquer fait plus de mal que de bien).

---

## S — Single Responsibility

> Un module doit avoir une seule raison de changer, c'est-à-dire **un seul acteur**
> (un seul groupe de personnes qui demande le changement).

Ce n'est pas « une classe = une méthode ». C'est : la logique de calcul de la paie
(DRH), le format d'export (Comptabilité) et la persistance (DBA) ne cohabitent pas.

**Symptômes**
- `OrderService` de 900 lignes, `*Manager`, `*Helper`, `*Utils`, `Common`.
- Une classe dont on ne peut pas résumer le rôle sans dire « et ».
- Un `using` d'infra (EF, HttpClient, Serilog) dans une classe qui contient du calcul métier.
- Le même fichier apparaît dans tous les PR, quelle que soit la feature.

**Correction** — découper suivant l'**axe de changement**, pas suivant la taille.
Extraire la politique métier de la mécanique (Humble Object).

**Contre-indication** — 30 classes d'une méthode chacune. Si l'extraction ne correspond
à aucun acteur distinct, on a juste éparpillé le problème.

---

## O — Open/Closed

> Ouvert à l'extension, fermé à la modification.

**Symptômes**
- Un `switch` / `if-else if` sur un type ou un enum qui grandit à chaque feature, et qui
  est **dupliqué à plusieurs endroits** (le vrai signal : c'est la duplication du switch,
  pas le switch lui-même).
- Modifier une classe stable pour ajouter un cas.

**Correction** — polymorphisme, table de dispatch (`IReadOnlyDictionary<Kind, IHandler>`),
services keyed (`AddKeyedScoped<IHandler>("virement", …)`), ou pattern matching centralisé
en un seul point.

**Contre-indication** — **ne pas ouvrir une classe avant qu'elle ait changé deux fois.**
Un `switch` à trois cas dans un seul fichier est plus lisible qu'une hiérarchie.
OCP se mérite par observation, pas par anticipation.

---

## L — Liskov Substitution

> Un sous-type doit être substituable à son type de base sans casser le contrat.

**Symptômes en C#**
- `public override void X() => throw new NotSupportedException();`
- Une précondition **renforcée** dans la redéfinition (le parent accepte `null`, l'enfant non).
- `if (x is SousType st)` dans le code appelant → la substitution ne marche pas.
- L'exemple canonique dans la BCL : `ReadOnlyCollection<T>` implémente `IList<T>` et jette
  sur `Add`. C'est une violation LSP livrée par Microsoft — ne pas la reproduire.

**Correction** — préférer la **composition** à l'héritage. Séparer les interfaces
(cf. ISP). Si le sous-type ne peut pas tout faire, il n'est pas un sous-type.

**Note** — LSP s'applique aussi aux contrats de service : une v2 d'API qui restreint les
valeurs acceptées casse Liskov pour ses appelants.

---

## I — Interface Segregation

> Aucun client ne doit dépendre de méthodes qu'il n'utilise pas.

**Symptômes**
- `IRepository<T>` avec 15 membres alors que l'appelant en utilise 2.
- Des implémentations de test remplies de `throw new NotImplementedException()`.
- Une interface dont le nom finit par `Service` et contient tout le domaine.

**Correction** — **role interfaces**, définies côté consommateur et nommées d'après le
besoin : `IFindOrderById`, `IPublishDomainEvents`. Une classe peut en implémenter
plusieurs. En C#, `Func<>` / `delegate` est souvent l'interface d'un seul rôle la plus
légère.

**Contre-indication** — l'atomisation à outrance : 12 interfaces d'une méthode toutes
implémentées par la même classe et injectées ensemble. Segmenter suivant les
**consommateurs réels**, pas suivant les méthodes.

---

## D — Dependency Inversion

> Les modules de haut niveau ne dépendent pas des modules de bas niveau ; les deux
> dépendent d'abstractions. **Les abstractions ne dépendent pas des détails.**

Le point que presque tout le monde rate : **l'interface appartient au module de haut
niveau**. `IOrderRepository` vit dans `Domain`/`Application`, pas dans `Infrastructure`.
Sinon l'inversion n'a pas eu lieu — on a juste ajouté une interface.

**Symptômes**
- `new HttpClient()`, `new SqlConnection()`, `File.ReadAllText` dans du code métier.
- `DateTime.Now`, `Guid.NewGuid()`, `Random` appelés directement (dépendances cachées et
  non testables) → `TimeProvider`, `IGuidProvider`.
- Un `static Logger.Log(...)`.
- `IServiceProvider` injecté dans une classe métier (**Service Locator** — anti-pattern,
  la dépendance devient invisible dans la signature).
- L'interface est dans le même projet que son unique implémentation d'infra.

**Correction** — le port est déclaré par le consommateur, implémenté dans l'infra,
câblé dans le **composition root** (`Program.cs`) et **seulement là**.

**Contre-indication** — inverser une dépendance stable ne sert à rien. On n'abstrait pas
`string`, `List<T>`, `Math`, ni une bibliothèque qui ne changera jamais et n'a pas
d'effet de bord. DIP sert à isoler ce qui est **volatil** ou **non déterministe**.

---

## Test de cohérence

Avant de dire « c'est SOLID », vérifier :

- [ ] Puis-je nommer **l'acteur** derrière chaque classe extraite ? (S)
- [ ] Le point d'extension a-t-il déjà **deux implémentations réelles** ? (O)
- [ ] Chaque implémentation honore-t-elle le contrat **sans exception** ? (L)
- [ ] Chaque consommateur utilise-t-il **tous** les membres de l'interface qu'il reçoit ? (I)
- [ ] L'interface est-elle dans le projet du **consommateur** ? (D)

Si une case ne coche pas, le principe n'est pas appliqué — il est invoqué.
