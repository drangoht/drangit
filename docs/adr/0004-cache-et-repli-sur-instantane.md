# ADR-0004 — Cache mémoire et repli sur instantané disque

- **Statut** : accepté
- **Date** : 2026-09-17

## Contexte

Chaque page du site a besoin du catalogue. Sans cache, une visite = un appel à GitHub : le
quota (ADR-0003) serait épuisé en quelques minutes, et la moindre latence de l'API se paierait
sur chaque rendu.

Et quand l'API est indisponible — panne, quota épuisé, compte mal orthographié — le site doit
malgré tout servir quelque chose.

## Décision

Deux niveaux, distincts et complémentaires.

**1. Cache mémoire, trente minutes.** Une seule entrée, le catalogue entier. Un sémaphore
garantit qu'un démarrage à froid sous trafic ne déclenche qu'un seul appel : sans lui, dix
requêtes simultanées feraient dix appels, dont neuf inutiles et tous comptés dans le quota.

**2. Instantané sur disque.** Chaque récupération réussie et non vide est écrite en JSON dans
un volume. Quand l'appel échoue, le catalogue relit ce fichier.

### Les points qui ne sont pas négociables

- **L'échec n'est jamais mis en cache.** Le figer pour la durée du TTL transformerait une
  indisponibilité de trois secondes en panne de trente minutes.
- **Une réponse vide n'écrase pas un instantané peuplé.** Un compte renommé répond `[]` ou 404 ;
  écrire cette liste vide détruirait le filet au moment précis où il sert.
- **L'écriture est atomique** (fichier temporaire puis `File.Move`) : une coupure en cours
  d'écriture laisserait sinon un instantané tronqué, donc inutilisable au moment critique.
- **Aucune opération du magasin ne lève.** Un instantané est un confort ; le perdre ne doit pas
  dégrader une requête qui, elle, s'est bien passée.
- **Le contenu éditorial s'applique en sortie**, après le cache et après le repli. Corriger une
  description ou masquer un dépôt prend donc effet immédiatement, même quand GitHub est
  injoignable.

## Conséquences

**Positives** — le site survit à une panne de GitHub et à un quota épuisé ; le quota n'est
jamais un sujet en fonctionnement normal.

**Négatives** — une information peut avoir jusqu'à trente minutes de retard ; le volume doit
exister et appartenir à l'utilisateur non privilégié du conteneur, sinon l'instantané n'est
jamais écrit — et personne ne s'en aperçoit avant la panne.

**À surveiller** — si l'instantané devait un jour survivre à un changement de forme du modèle,
il lui faudrait un numéro de version : aujourd'hui, un instantané illisible est simplement
ignoré et reconstruit.
