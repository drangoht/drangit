# ADR-0011 — Empiler les sorties du prompt sans recharger la page

- **Statut** : Accepté
- **Date** : 2026-09-19
- **Décideurs** : drangoht
- **Amende** : ADR-0008, qui voulait que le script « laisse le navigateur suivre l'URL »

## Contexte

L'ADR-0008 a coupé le prompt en deux : le serveur décide, le script ajoute du confort. Les
commandes d'information — `help`, `stats`, `topics` — s'affichent sans recharger ; les
commandes qui filtrent ou qui naviguent partent au serveur en requête ordinaire.

À l'usage, la console ne se comporte pas comme une console. Filtrer recharge la page, et
l'écran repart de zéro : la sortie précédente disparaît au lieu de rester au-dessus. Or ce
sont précisément les commandes les plus tapées. On tape `ls --topic=unity`, puis `find rust`,
et à chaque fois l'écran est balayé — un terminal, lui, empile.

## Options envisagées

### A — S'en tenir à l'ADR-0008

Chaque commande de filtre recharge la page.

**Avantages** — rien à écrire ; le script reste minuscule.
**Inconvénients** — la console n'en est pas une. L'empilement est ce qui distingue un
terminal d'un formulaire, et c'est justement ce qui manque.

### B — Filtrer dans le navigateur

Le script connaît les critères, filtre la liste déjà chargée et écrit le résultat.

**Avantages** — instantané, aucune requête.
**Inconvénients** — le script gagne une règle métier, ce que l'ADR-0008 désignait comme la
frontière à ne pas franchir : le vocabulaire des filtres vivrait vraiment à deux endroits,
et les deux pourraient diverger en silence. La liste affichée est par ailleurs tronquée
(ADR-0010) : filtrer côté client ne verrait pas les dépôts absents de la page.

### C — Demander la sortie au serveur, et l'empiler

Le script envoie la commande à l'adresse qu'aurait suivie le formulaire, récupère la page
rendue, en extrait le bloc de sortie et l'ajoute sous les précédents. L'adresse suit par
`pushState`.

**Avantages** — le serveur reste seul à savoir ce qu'est un sujet et comment filtrer ; le
script fait ce qu'il faisait déjà, recopier un bloc rendu, à ceci près qu'il va le chercher.
L'écran empile comme un terminal. L'adresse reste partageable et rechargeable.
**Inconvénients** — une page entière transite à chaque commande pour n'en garder qu'un bloc ;
le bouton « précédent » demande un traitement ; et c'est du code non couvert de plus.

## Décision

Nous retenons **l'option C**.

Parce que :

1. **Le script ne gagne aucune règle.** Il ne sait toujours pas ce qu'est un sujet, ni
   filtrer, ni trier. La frontière posée par l'ADR-0008 tient : ce qui change, c'est qu'il va
   chercher le bloc au lieu de le trouver dans la page.
2. **Le socle est intact.** Sans JavaScript, le formulaire part comme avant, vers la même
   adresse, et rend la même page. La dégradation reste complète.
3. **L'adresse continue de dire ce qui est affiché.** `pushState` la met à jour, et le bouton
   « précédent » recharge — ce que le serveur rend fait foi, toujours.

## Conséquences

**Positives** — la console se comporte comme une console ; l'historique visuel d'une session
reste à l'écran ; `clear` retrouve son sens.

**Négatives** — chaque commande transporte une page entière pour n'en garder qu'un bloc. À
cette taille c'est sans conséquence, mais ce serait le premier poste à revoir si la liste
grossissait — un fragment rendu à part coûterait moins cher, au prix d'une route de plus.

Deuxième coût : **le bouton « précédent » recharge la page** au lieu de restaurer l'écran
empilé. C'est un choix : restaurer l'empilement demanderait de le mémoriser, donc de tenir un
état d'affichage que personne n'a demandé.

Troisième coût, hérité : tout ceci reste **hors de la couverture de tests** (ADR-0008). Le
filet, c'est que le chemin sans JavaScript — lui testé — rend exactement la même chose.

**Neutres / à surveiller** — si une commande devait un jour modifier quelque chose, ce
transport ne conviendrait plus : on ne rejoue pas une écriture en allant chercher une page.
Tant que les commandes ne font que lire, il est à sa place.

## Suivi

- [x] Le script n'extrait qu'un bloc de sortie, jamais l'amorce : la bannière ne se répète pas.
- [x] Une réponse sans bloc de sortie — `open`, `cd` — rend la main au navigateur.
- [x] Une requête en échec retombe sur la navigation ordinaire.
- [x] `popstate` recharge l'adresse retrouvée.
