---
name: refactoring
description: Refactoring discipliné de code C# existant — identification des code smells et application du refactoring correspondant, tests verts entre chaque étape. À utiliser quand le code fonctionne mais est difficile à lire, tester ou faire évoluer.
argument-hint: [fichier ou classe à refactorer]
---

# Refactoring

## Préconditions — vérifier avant de toucher au code

1. `dotnet test` est **vert**. Sinon : arrêter, réparer d'abord.
2. Le code cible est **couvert**. Sinon : écrire les tests de caractérisation d'abord
   (tests qui figent le comportement actuel, bugs compris — on les corrigera ensuite,
   séparément).
3. Le travail en cours est commité. Un refactoring ne se mélange pas à autre chose.

## Discipline

- **Une transformation à la fois.** `dotnet test` après chacune.
- Rouge → `git checkout` de l'étape, pas de rustine.
- Aucun changement de comportement. Si le comportement doit changer, ce n'est plus un
  refactoring : c'est une feature, elle passe par la skill `tdd`.
- **Commit séparé**, message `refactor: …`.

## Catalogue smell → refactoring

| Smell | Signal en C# | Refactoring |
|---|---|---|
| Long Method | Ne tient pas à l'écran, plusieurs niveaux d'abstraction | Extract Method, Replace Temp with Query |
| Large Class | Champs utilisés par des sous-ensembles disjoints de méthodes | Extract Class suivant l'axe de cohésion |
| Long Parameter List | > 3 paramètres, arguments qui voyagent ensemble | Introduce Parameter Object (`record`) |
| Primitive Obsession | `string iban`, `decimal montant`, `int statut` partout | Replace Primitive with Value Object |
| Data Clump | Les mêmes 3 paramètres partout | Extract Class / `record` |
| Feature Envy | Une méthode manipule surtout les données d'une autre classe | Move Method |
| Switch dupliqué | Le même `switch (type)` à plusieurs endroits | Replace Conditional with Polymorphism, ou dispatch keyed |
| Flag Argument | `Process(order, isDraft: true)` | Split Method |
| Temporal Coupling | `Init()` obligatoire avant `Run()` | Constructeur qui garantit l'invariant |
| Message Chain | `a.B.C.D.Do()` | Hide Delegate, Tell Don't Ask |
| Middle Man | Classe qui ne fait que déléguer | Remove Middle Man |
| Shotgun Surgery | Un changement métier touche 8 fichiers | Regrouper par feature (screaming architecture) |
| Divergent Change | Un fichier change pour 4 raisons différentes | Extract Class (SRP par acteur) |
| Speculative Generality | Interface à 1 implémentation, `<T>` pour 1 type, hook inutilisé | **Supprimer** (YAGNI rétroactif) |
| Anemic Model | Entités sans comportement, règles dans des `*Service` | Move Method vers l'agrégat |
| Comments | Le commentaire explique *quoi* | Extract Method au nom du commentaire |

## Ordre recommandé sur une classe difficile

1. **Renommer** (gratuit, aucun risque, révèle souvent la responsabilité réelle).
2. Extraire les méthodes évidentes → la structure apparaît.
3. Introduire les value objects (supprime la validation dispersée).
4. Déplacer les méthodes vers la classe qui possède les données.
5. **Alors seulement** envisager d'extraire une classe ou une abstraction —
   avec la skill `choix-pattern`.

Ne pas commencer par l'étape 5. Un découpage décidé avant d'avoir nettoyé le nommage se
fait presque toujours sur le mauvais axe.

## Supprimer aussi

Le refactoring, c'est autant enlever qu'ajouter : code mort, paramètre inutilisé,
abstraction spéculative, test redondant, option de configuration jamais changée.
Git garde l'historique — pas besoin de commenter.

## Rapport

```
Smells identifiés : <liste, par fichier>
Appliqué          : <transformations, dans l'ordre>
Tests             : verts après chaque étape (<n> exécutions)
Comportement      : inchangé
Reste à faire     : <ce qui mériterait un second passage, et pourquoi ce n'est pas fait ici>
```
