# ADR-0008 — Faire du prompt de commande une amélioration progressive

- **Statut** : Accepté
- **Date** : 2026-09-19
- **Décideurs** : drangoht

## Contexte

Le site emprunte aujourd'hui la palette de GitHub : fond ardoise, accent bleu. Correct, et
anonyme — il ressemble à la page d'où viennent ses données. Quatre directions visuelles ont été
maquettées ; celle retenue, **« Console phosphore »**, présente le catalogue comme la sortie
d'un programme : colonnes alignées, quota d'API affiché comme une donnée, ligne de commande en
bas d'écran.

Cette direction pose une question que les trois autres ne posaient pas : **si le site affiche un
prompt, on doit pouvoir y taper**. Un prompt décoratif se remarque immédiatement, et pour la
mauvaise raison.

Or la contrainte du projet est explicite : le rendu est du Blazor SSR statique. Aucun
`@rendermode` n'existe dans le code, `wwwroot/` ne contient que `app.css`, et les filtres sont
des liens et un `<form method="get">` — ce qui les rend indexables, partageables et
fonctionnels sans JavaScript.

Un prototype jouable du jeu de commandes a servi à trancher : `docs/prototypes/console-phosphore.html`.

## Options envisagées

### A — Tout côté serveur, aucun JavaScript

Le prompt est un `<form method="get">` habillé en ligne de commande. Le serveur analyse la
ligne, la traduit en filtre et rend la page. Les commandes d'information (`help`, `whoami`,
`stats`) rendent un bloc de sortie.

**Avantages** — aucune dérogation ; chaque commande produit une URL partageable ; entièrement
couvert par la suite de tests existante.
**Inconvénients** — un aller-retour réseau par commande, donc pas de sortie qui s'empile, pas
d'historique `↑ ↓`, pas de complétion `Tab`. Le prompt fonctionne mais ne ressemble pas à un
terminal, ce qui était le but.
**Coût** — faible.

### B — Socle serveur, enrichissement côté client

L'option A reste le socle et fonctionne seule. Un script vanilla dans `wwwroot/` (~150 lignes,
aucun composant interactif, aucun WebSocket) ajoute par-dessus : sortie empilée sans
rechargement, historique, complétion, commandes d'information et commandes sans utilité.
Les commandes de filtre, elles, délèguent à la navigation normale.

**Avantages** — le terminal se comporte comme un terminal ; la dégradation sans JavaScript est
complète, pas partielle ; le rendu reste statique et indexable.
**Inconvénients** — un premier fichier JavaScript dans un projet qui n'en avait aucun ; il
n'est couvert par aucun test de la suite actuelle ; le vocabulaire des filtres est désormais
connu à deux endroits.
**Coût** — moyen.

### C — `@rendermode InteractiveServer`

Le shell est écrit en C#, dans un composant interactif.

**Avantages** — un seul langage, un seul jeu de tests, l'état vit là où sont les données.
**Inconvénients** — un circuit SignalR ouvert en permanence par visiteur et un état serveur
maintenu, pour un gadget. Le site cesse de répondre si le circuit tombe. Sans commune mesure
avec le bénéfice sur un site vitrine.
**Coût** — élevé, et permanent à l'exécution.

### D — Ne rien faire

Garder les filtres actuels et dessiner un prompt qui ne réagit pas, ou pas de prompt du tout.

**Avantages** — zéro travail, zéro dérogation.
**Inconvénients** — un prompt décoratif est pire que pas de prompt ; sans lui, la direction
visuelle retenue perd ce qui la justifiait.

## Décision

Nous retenons **l'option B**.

Parce que :

1. **La dégradation est complète, pas partielle.** Sans JavaScript, le site ne perd aucune
   fonction : les commandes de filtre sont des navigations, les filtres cliquables restent.
   Ce qui disparaît — l'historique, la complétion, les commandes sans utilité — n'est pas une
   fonction du site.
2. **Le coût à l'exécution est nul.** Un fichier statique de plus, pas de connexion ouverte,
   pas d'état serveur : l'option C fait payer à chaque visiteur un confort qui n'en vaut pas
   le prix.
3. **Le prompt n'est jamais le seul chemin.** Il double une interface qui existe déjà, il ne
   la remplace pas. C'est ce qui rend la dérogation supportable.

## Conséquences

**Positives** — la direction visuelle tient sa promesse ; le rendu reste statique, indexable et
partageable ; le site continue de fonctionner sans JavaScript, y compris pour un robot
d'indexation ; le jeu de commandes est validé avant d'être écrit, prototype à l'appui.

**Négatives** — c'est une **dérogation assumée à deux règles du projet** :

- `CLAUDE.md`, « pas d'interactivité côté client » — amendée par cet ADR ;
- « aucun code de production sans test rouge préalable » : **le script client n'est couvert par
  aucun test**. La suite est en xUnit ; le couvrir demanderait Playwright, c'est-à-dire un
  navigateur dans la CI pour valider un confort de saisie. Nous ne le faisons pas. La
  conséquence est donc explicite : **une régression du script ne sera pas détectée par la CI,
  seulement à l'œil.** Le parseur de commandes du serveur, lui, est couvert, et c'est lui qui
  porte les fonctions réelles.

Deuxième coût : **le vocabulaire des filtres vit désormais à deux endroits** — le parseur C# et
le script. Ils peuvent diverger sans que rien ne le signale. Le script reste donc volontairement
pauvre : il n'interprète aucun filtre, il laisse le navigateur suivre l'URL.

**Neutres / à surveiller** — le signal qui ferait reconsidérer : le jour où le script doit
connaître une règle métier (trier, agréger, décider ce qui est affichable), la frontière est
franchie et il faut revenir à l'option A ou basculer en C. Tant qu'il ne fait que présenter
et naviguer, il reste à sa place.

## Suivi

- [x] Le parseur de ligne de commande est écrit en TDD, côté serveur, et traduit vers
      `RepositoryFilter` — il est le seul à connaître le vocabulaire des commandes.
- [x] Un test d'intégration vérifie qu'une commande de filtre soumise sans JavaScript rend la
      même page que le lien de filtre correspondant.
- [x] Les filtres cliquables restent présents et utilisables au clavier : le prompt ne devient
      jamais le seul moyen de filtrer.
- [x] `CLAUDE.md` est amendé et renvoie à cet ADR.
