# ADR-0010 — Faire du prompt la seule interface de l'accueil

- **Statut** : Accepté
- **Date** : 2026-09-19
- **Décideurs** : drangoht
- **Amende** : ADR-0008, dont le suivi exigeait que « les filtres cliquables restent présents »

## Contexte

L'accueil arrivé au bout de l'ADR-0008 empilait trois interfaces pour un même travail : une
barre de critères cliquables, un formulaire de langage, et un prompt qui savait déjà tout faire.
S'y ajoutait un bloc de vitrine avant la liste. La page disait « console » et se comportait comme
un site ordinaire repeint en vert.

La maquette qui a fait retenir cette direction, elle, ne montre qu'une fenêtre de terminal :
barre de titre, sortie, prompt. Rien d'autre.

## Options envisagées

### A — Garder la barre de critères au-dessus du prompt

Le statu quo, et ce que l'ADR-0008 avait inscrit dans son suivi.

**Avantages** — les sujets se découvrent d'un coup d'œil ; filtrer se fait à la souris ; aucune
décision à reprendre.
**Inconvénients** — deux chemins pour la même action, dont l'un rend l'autre inutile. La page
ne ressemble pas à ce qu'elle prétend être.

### B — Le prompt seul, dans une fenêtre de terminal

La barre de critères, le sélecteur de langage et la vitrine disparaissent. Tout passe par la
ligne de commande, et la page entière tient dans un cadre.

**Avantages** — un seul chemin, celui que la direction visuelle promettait ; la page se lit
enfin comme la sortie d'un programme ; moins de code à tenir — une section de balisage, un bloc
de styles, cinq méthodes de composition d'URL en moins.
**Inconvénients** — filtrer demande de savoir quoi taper. Les sujets et les langages ne
s'offrent plus à l'œil : il faut les demander.
**Coût** — faible en écriture, réel en découvrabilité.

### C — Une barre repliée, ouverte par une commande

Le meilleur des deux, en théorie.

**Inconvénients** — un état d'interface à porter, dans une page qui n'en a aucun, pour un
compromis que personne n'a demandé.

## Décision

Nous retenons **l'option B**, et nous amendons donc le suivi de l'ADR-0008.

Parce que :

1. **Deux interfaces pour une même action, c'est une de trop.** Le prompt sait déjà filtrer,
   ouvrir une fiche et changer de page ; la barre ne faisait que répéter une partie de cela.
2. **La direction visuelle n'était pas tenue.** Une console avec une barre de filtres au-dessus
   n'est pas une console : c'est un site qui en porte le costume.
3. **La perte est bornée, et rattrapée.** Ce qui disparaît, c'est la découverte au coup d'œil —
   pas la fonction. Voir plus bas.

## Ce que l'ADR-0008 exigeait, et comment nous le tenons autrement

L'ADR-0008 posait que « le prompt ne devient jamais le seul moyen de filtrer », et c'est ce qui
rendait acceptable l'ajout de JavaScript. Le prompt devient bel et bien le seul moyen. La raison
pour laquelle cela reste tenable :

- **Filtrer ne dépend toujours pas de JavaScript.** Le prompt est un `<form method="get">` ; la
  commande part au serveur, qui la traduit et redirige. Sans script, tout fonctionne.
- **Les sujets restent découvrables sans JavaScript.** `topics` et `langs` sont devenus le seul
  endroit où on les voit : le serveur sait donc les rendre lui-même, sans redirection, en plus
  du script qui les affiche sans recharger. Un test le vérifie.
- **`help` est à une frappe.** L'invite du champ nomme les commandes utiles, et `help` les
  détaille.
- **Les sujets d'un dépôt restent des liens** sur sa fiche : la navigation par sujet ne passe
  pas que par la frappe.

## Conséquences

**Positives** — une page qui tient sa promesse ; un seul chemin à tester et à maintenir ;
la barre de critères, le sélecteur de langage, la vitrine et les méthodes qui composaient leurs
adresses disparaissent du code.

**Négatives** — un visiteur qui ne lit pas l'invite ne saura pas qu'il peut filtrer. C'est le
prix assumé de la direction retenue, et il se paie surtout à la première visite. La vitrine
disparaît aussi comme bloc : le dépôt désigné par le fichier éditorial ouvre désormais la liste,
ce qui conserve la désignation mais lui donne beaucoup moins de poids.

**Neutres / à surveiller** — le signal qui ferait reconsidérer : si les pages vues montrent que
personne ne tape rien, la découverte est trop faible et il faudra rendre les sujets visibles
d'une autre manière — une ligne de sortie « topics » affichée d'emblée, par exemple, plutôt que
le retour d'une barre de filtres.

## Suivi

- [x] La barre de critères, le sélecteur de langage et la vitrine sont retirés.
- [x] `topics` et `langs` répondent sans JavaScript, rendus par le serveur.
- [x] Le dépôt désigné en vitrine ouvre la liste, et un test le vérifie.
- [x] `CLAUDE.md` est mis à jour.
