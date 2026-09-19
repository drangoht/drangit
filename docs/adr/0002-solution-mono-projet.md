# ADR-0002 — Un seul projet applicatif, des dossiers plutôt que des assemblies

- **Statut** : accepté
- **Date** : 2026-09-17

## Contexte

La règle de dépendance (`.claude/rules/40-architecture.md`) demande que le modèle ne connaisse
ni HTTP, ni disque, ni ASP.NET. La façon habituelle de l'imposer mécaniquement est de séparer
`Domain`, `Application`, `Infrastructure` et `Web` en quatre assemblies : le compilateur refuse
alors physiquement une dépendance interdite.

Le site est une vitrine en lecture seule : un appel sortant, un modèle de cinq types, aucune
écriture métier, aucune transaction. Quatre projets pour cela, ce sont quatre `.csproj` à tenir
à jour, quatre fois la restauration, et une indirection à traverser pour lire trois lignes.

## Décision

Un seul projet applicatif, `Drangit.Web`, découpé **par dossiers** :

| Dossier | Rôle |
|---|---|
| `Repositories/` | le modèle : `Repository`, `RepositorySlug`, `LocalizedText`, les filtres |
| `Repositories/GitHub/` | la couche anti-corruption — seul endroit qui parle GitHub |
| `Repositories/Editorial/` | la fusion avec le fichier éditorial versionné |
| `Repositories/Snapshots/` | l'instantané de repli sur disque |
| `Localization/`, `Seo/`, `Components/` | la présentation |

C'est une **dérogation assumée** à la règle de dépendance telle qu'elle s'écrit d'ordinaire.

## Ce qui la rend tenable

La règle n'est plus imposée par le compilateur : elle l'est par des **tests d'architecture**
(`tests/Drangit.Tests/Architecture/`), qui vérifient par réflexion que le modèle n'expose
aucune donnée de compte et que le composition root se résout réellement.

## Conséquences

**Positives** — une solution qu'on lit d'un coup d'œil ; pas de projet créé « au cas où ».

**Négatives** — rien n'empêche *physiquement* un `using` fautif : la discipline et les tests
sont le seul garde-fou.

**À surveiller** — si le site cesse d'être en lecture seule, ou si un second consommateur
apparaît (une API, un travail de fond), reconsidérer : la séparation en assemblies redevient
alors moins chère que la discipline.
