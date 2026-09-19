# ADR-0001 — Consigner les décisions d'architecture dans des ADR

- **Statut** : accepté
- **Date** : 2026-09-17

## Contexte

Les décisions structurantes (découpage, choix de bibliothèque, dérogation à une règle de
codage) se prennent en revue et se perdent. Six mois plus tard, personne ne sait si un choix
était réfléchi ou accidentel, et on le remet en cause sans connaître les contraintes
d'origine.

## Options envisagées

### A — ADR versionnés dans le dépôt
Fichiers Markdown numérotés dans `docs/adr/`, relus en pull request comme du code.

### B — Wiki
Plus confortable à rédiger, mais découplé du code : personne ne le lit au bon moment et il
n'est pas revu.

### C — Ne rien faire
Statu quo. Les décisions restent dans les têtes et les fils de discussion.

## Décision

Nous retenons **A**.

Parce que : la décision est versionnée avec le code qu'elle concerne, elle passe par la
revue de PR, et elle reste lisible hors ligne.

## Conséquences

**Positives** — historique traçable ; une dérogation devient une décision explicite au lieu
d'une dette silencieuse.

**Négatives** — friction à l'écriture ; risque d'ADR rédigés pour la forme, sans les
alternatives réellement pesées.

**À surveiller** — si les ADR ne sont plus relus en revue, ils ne valent pas mieux qu'un
wiki : reconsidérer.
