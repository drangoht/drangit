---
name: adr
description: Rédiger un Architecture Decision Record dans docs/adr/. À utiliser pour tout choix structurant, tout arbitrage entre plusieurs options, ou toute dérogation assumée aux règles du projet.
argument-hint: [sujet de la décision]
---

# Architecture Decision Record

## Quand en écrire un

- Choix d'une bibliothèque, d'un découpage, d'un protocole, d'un modèle de données
- Arbitrage entre plusieurs options défendables
- **Dérogation assumée** à une règle de `.claude/rules/` — c'est le cas le plus important :
  une entorse documentée est une décision, une entorse silencieuse est de la dette
- Décision qu'on regrettera de ne pas pouvoir expliquer dans 18 mois

Pas d'ADR pour : un choix de nommage, une correction de bug, un refactoring local.

## Règles

- Numérotation séquentielle : `docs/adr/0007-outbox-pour-les-evenements-paiement.md`
- **Un ADR est immuable.** On ne le modifie pas : on en écrit un nouveau avec
  `Statut: Accepté` et `Supersède: ADR-0007`, et l'ancien passe à `Remplacé par ADR-0012`.
- Écrit au présent, en français, sans jargon inutile.
- Les alternatives écartées sont la partie **la plus utile** — c'est ce qu'on oublie.
- Les conséquences incluent les **négatives**. Un ADR sans inconvénient est un ADR
  malhonnête.

## Gabarit

```markdown
# ADR-NNNN — <Titre à l'impératif : "Utiliser X pour Y">

- **Statut** : Proposé | Accepté | Remplacé par ADR-XXXX | Abandonné
- **Date** : AAAA-MM-JJ
- **Décideurs** : <qui>

## Contexte

Le problème, les contraintes (techniques, réglementaires, délai, équipe), ce qui est
déjà en place. Factuel. Pas de solution ici.

## Options envisagées

### A — <nom>
Description. Avantages. Inconvénients. Coût estimé.

### B — <nom>
…

### C — Ne rien faire
Toujours l'évaluer explicitement.

## Décision

Nous retenons **<option>**.

Parce que : <les 2-3 critères qui ont réellement tranché>.

## Conséquences

**Positives** — …

**Négatives** — ce que ça nous coûte, ce que ça ferme comme porte.

**Neutres / à surveiller** — le signal qui nous ferait reconsidérer.

## Suivi

- [ ] Test d'architecture / garde-fou qui rend la décision exécutable
- [ ] Mise à jour de `.claude/rules/` ou de `CLAUDE.md` si la règle change
```

## Après rédaction

Si l'ADR modifie une règle du projet, **mettre à jour le fichier de règle correspondant**
et y référencer l'ADR. Un ADR qui contredit `CLAUDE.md` sans le corriger crée une
contradiction qui sera arbitrée au hasard.
