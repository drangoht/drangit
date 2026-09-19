# Clean Code — règles opérationnelles

Source : Robert C. Martin, *Clean Code* & *Clean Coder*. Adapté à C#.

## Nommage

- **Révélateur d'intention.** `int d;` → `int joursDepuisModification;`
- Classe = **nom**. Méthode = **verbe**. Booléen = **prédicat** (`EstEligible`, `HasPendingPayment`).
- Longueur : proportionnelle à la portée pour une **variable** (`i` dans une boucle de 2
  lignes est correct), **inversement** proportionnelle pour une **fonction** (une méthode
  publique très utilisée mérite un nom court, une méthode privée spécialisée un nom long).
- Bannis : `Manager`, `Processor`, `Data`, `Info`, `Common`, `Utils`, `Helper`, `Handler`
  générique. Ils signalent qu'on n'a pas trouvé la responsabilité.
- Bannis aussi : notation hongroise, `str`, `lst`, `_svc`, abréviations maison.
- **Un mot par concept** : `Get`/`Fetch`/`Retrieve`/`Load` ne coexistent pas pour la même
  opération dans une même base de code.
- Le nom ne ment jamais. `SaveAsync` qui envoie aussi un e-mail est un bug de nommage
  *et* un bug de conception.

## Fonctions

- **Petites.** Puis plus petites. Pas de seuil magique, mais au-delà d'un écran on cherche
  l'extraction.
- **SLAP** — Single Level of Abstraction Principle : une fonction ne mélange pas
  l'orchestration métier et la manipulation de chaînes.
- **≤ 3 paramètres.** Au-delà : introduire un `record` de paramètres (les arguments qui
  voyagent ensemble sont un concept qui n'a pas de nom).
- **Aucun paramètre booléen.** `Save(order, true)` est illisible. Deux méthodes, ou un enum.
- **CQS** — Command/Query Separation : une méthode change l'état **ou** retourne une
  valeur, pas les deux. (Exceptions assumées : `TryGetValue`, `Stack.Pop`.)
- **Pas d'effet de bord caché.** Si le nom ne l'annonce pas, la fonction ne le fait pas.
- Sortie par `out` : réservée au pattern `TryXxx`.

## Commentaires

> Un commentaire est un échec — celui de n'avoir pas su l'exprimer en code.

**Autorisés** : le *pourquoi* (décision non évidente, contournement d'un bug tiers avec
lien), l'avertissement de conséquence, la doc XML des API publiques, un `TODO` **avec un
numéro de ticket**.

**Interdits** : le commentaire qui paraphrase le code, le journal de modifications (git le
fait), le code commenté (git le fait aussi), les bannières ASCII, les `#region`.

## Mise en forme

- Métaphore du journal : le fichier se lit de haut en bas, du général au détail.
- La fonction appelée se place **sous** la fonction appelante.
- Les variables sont déclarées au plus près de leur usage.
- Un fichier = un type public.

## Gestion d'erreurs

- **Exceptions, pas de codes retour.** Un code retour ignorable finit ignoré.
- **Ne jamais retourner `null`** — retourner une collection vide, ou modéliser l'absence
  (`Result<T>`, `Option<T>`, `TryGet`). Ne jamais **passer** `null` en argument.
- `catch { }` vide : interdit. `catch (Exception)` non journalisé et non rethrown : interdit.
- `throw;` pour relancer, **jamais** `throw ex;` (écrase la stack trace).
- Exceptions métier typées (`InsufficientFundsException`) portant le contexte
  (identifiants, montants) — pas un `Exception("erreur")`.
- **Les exceptions ne servent pas de contrôle de flux.** Un cas métier attendu (solde
  insuffisant, validation) se modélise en type de retour, pas en exception.
- Séparer le try du reste : le corps d'un `try` est idéalement un seul appel de méthode.

## Classes

- Petites, mesurées en **responsabilités**, pas en lignes.
- **Cohésion élevée** : si un sous-ensemble des champs n'est utilisé que par un
  sous-ensemble des méthodes, la classe demande à être coupée là.
- `sealed` par défaut. On ouvre à l'héritage sciemment, en documentant le contrat (LSP).
- Champs privés, `readonly` quand possible. Pas d'état mutable public.

## Frontières

- Ne pas laisser fuir un type tiers (client HTTP, ORM, SDK) dans le domaine : l'encapsuler
  derrière un port (couche anti-corruption).
- Écrire des **learning tests** sur les bibliothèques externes : ils documentent l'usage et
  détectent les régressions lors des montées de version.

## Le Boy Scout et sa limite

Améliorer un nom, extraire une méthode, supprimer un commentaire mort : oui, à chaque
passage. Mais dans un **commit dédié**, jamais dans le commit qui corrige le bug.
