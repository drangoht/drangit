---
name: nouvelle-feature
description: Workflow complet d'implémentation d'une fonctionnalité .NET, de la conception à la revue, en orchestrant les sous-agents architecte, dev-tdd et revieweur. À utiliser pour toute feature qui touche plus d'un fichier.
argument-hint: [description de la fonctionnalité]
---

# Workflow feature

Enchaînement en 5 phases. **Ne pas passer à la phase suivante sans validation explicite
de l'utilisateur aux points marqués STOP.**

## Phase 1 — Comprendre (aucun code)

- Reformuler le besoin en une phrase, du point de vue de l'utilisateur final.
- Lister les **règles métier** et les **cas limites** — c'est là que sont les vraies
  questions.
- Identifier l'agrégat / le module concerné. Lire le code existant avoisinant
  (déléguer à `Explore` pour ne pas saturer le contexte).
- Poser les questions ouvertes **maintenant**. Ne pas deviner une règle métier.

**STOP** — valider la compréhension avant de concevoir.

## Phase 2 — Concevoir (déléguer à `@architecte`)

Sortie attendue : frontières touchées, ports nécessaires, contrats (entrée/sortie),
impact sur le modèle, et la **liste des tests à écrire** dans l'ordre.

- Appliquer la skill `choix-pattern` pour toute abstraction envisagée.
- Si la décision est structurante ou déroge à une règle → skill `adr`.
- Proposer la solution la plus simple qui répond au besoin d'aujourd'hui.

**STOP** — valider le plan avant d'écrire.

## Phase 3 — Implémenter (déléguer à `@dev-tdd`)

Skill `tdd`, un comportement à la fois, en partant du **domaine vers l'extérieur** :

1. Règles métier dans `Domain` (tests purs, rapides)
2. Cas d'usage dans `Application` (ports en double de test)
3. Adaptateurs dans `Infrastructure` (tests d'intégration, Testcontainers)
4. Exposition dans `Api` (DTO dédié, jamais l'entité)

Après chaque comportement : suite verte, phase refactor effectuée.

## Phase 4 — Vérifier

```bash
dotnet build  -warnaserror
dotnet test
dotnet format --verify-no-changes
```

Puis skill `revue-clean-code` (ou `@revieweur`) sur le diff complet.
Traiter tous les points **Bloquant** et **Majeur**. Les **Mineurs** sont arbitrés avec
l'utilisateur.

## Phase 5 — Livrer

- Commits séparés : `refactor:` (préparation) → `feat:` (comportement) → `test:` si isolé.
  Jamais un commit qui mélange les trois.
- Message de commit : **pourquoi**, pas quoi. Le diff dit déjà le quoi.
- Signaler explicitement à l'utilisateur :
  - ce qui a été **volontairement écarté** (YAGNI) et son déclencheur futur
  - les cas limites **non couverts** et pourquoi
  - toute dette introduite sciemment

## Garde-fous

- Aucune modification hors du périmètre annoncé sans le dire.
- Aucun test désactivé, aucun warning supprimé, aucun analyzer contourné pour faire
  passer la CI.
- En cas de blocage (règle métier ambiguë, dépendance manquante) : **s'arrêter et
  demander**, ne pas inventer une règle plausible.
