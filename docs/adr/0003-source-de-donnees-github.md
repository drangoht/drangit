# ADR-0003 — L'API GitHub publique comme source, avec un jeton facultatif

- **Statut** : accepté
- **Date** : 2026-09-17

## Contexte

Le site liste les dépôts publics d'un compte. Trois façons d'obtenir cette liste :

1. la figer dans un fichier versionné ;
2. l'interroger au runtime sur l'API GitHub ;
3. la générer à la construction de l'image.

L'API impose un quota : **60 requêtes par heure et par adresse IP** en anonyme, **5000** avec
un jeton. Sur un VPS partagé, cette IP est partagée avec tout ce qui y tourne.

## Décision

**Option 2**, sur `GET /users/{login}/repos`, avec un **jeton facultatif**.

- Sans jeton, le site fonctionne : le cache de trente minutes ramène le besoin à deux appels
  par heure au pire, très loin des 60 accordés, et l'instantané de repli (ADR-0004) couvre le
  cas où le quota serait malgré tout épuisé par un voisin de palier.
- Avec un jeton, le quota cesse d'être un sujet. Aucune portée n'est nécessaire : le site ne
  lit que du public. Un jeton *fine-grained* sans aucune permission convient, et c'est ce qu'il
  faut demander — un jeton classique avec la portée `repo` donnerait accès aux dépôts privés,
  qu'on ne veut surtout pas voir passer.

### Pourquoi pas la liste figée (1)

Elle vieillit sans qu'on s'en aperçoive : un dépôt renommé, archivé ou créé n'apparaît pas, et
rien ne le signale. Le fichier éditorial existe déjà pour ce qui relève d'un choix rédactionnel ;
la liste des dépôts, elle, est un fait.

### Pourquoi pas la génération à la construction (3)

Elle rendrait chaque mise à jour de contenu dépendante d'un redéploiement, pour économiser un
appel toutes les trente minutes.

## Ce que la décision impose

- **Le point d'accès choisi ne renvoie que du public**, même authentifié — c'est la raison pour
  laquelle c'est lui et non `/user/repos`. Le client refuse malgré tout tout dépôt marqué
  `private` ou dont la `visibility` n'est pas `public` : la garantie est extérieure au projet,
  on ne s'y fie pas seule.
- **Un jeton vide n'est pas un jeton.** Le déploiement écrit toutes les clés du modèle dans
  l'environnement, renseignées ou non : sans normalisation, l'absence de jeton produirait un
  en-tête `Authorization: Bearer ` que GitHub refuse par un 401 — soit exactement le contraire
  de l'appel anonyme voulu.
- **Le quota épuisé n'est pas une panne.** Il a son propre type d'exception, qui porte l'heure
  de réarmement annoncée par GitHub, parce que le journal doit dire *jusqu'à quand* le site sert
  son instantané.
- **Les bifurcations sont écartées par défaut.** Une vitrine montre ce qu'on a écrit ; vingt
  dépôts bifurqués pour y corriger une virgule noieraient le reste. `GitHub:IncludeForks` permet
  d'en décider autrement.

## Conséquences

**Positives** — la vitrine suit le compte sans intervention ; aucun secret n'est *requis* pour
faire tourner le site, y compris en local.

**Négatives** — une dépendance réseau au rendu ; un quota à surveiller ; un champ `homepage`
saisi à la main côté GitHub, donc parfois vide ou non-URL, qu'il faut valider.

**À surveiller** — si le compte dépasse quelques centaines de dépôts, la pagination (bornée à
cinq pages) devra être revue, ou la liste préchargée à la construction.
