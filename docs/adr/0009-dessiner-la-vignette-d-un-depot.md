# ADR-0009 — Dessiner la vignette d'un dépôt plutôt qu'afficher la carte de GitHub

- **Statut** : Accepté
- **Date** : 2026-09-19
- **Décideurs** : drangoht

## Contexte

GitHub produit une image par dépôt (`opengraph.githubassets.com/1/compte/dépôt`). Le site s'en
servait comme illustration de vignette, avec, en dessous, un repli dessiné qui **imitait** cette
carte — même fond très clair, même titre « compte/dépôt » en haut à gauche — pour qu'une image
qui n'arrive pas ne laisse pas de trou dans la grille.

Ce montage coûtait cher en subtilité. La carte incruste l'avatar du compte en haut à droite et
aligne des compteurs en bas : affichée telle quelle sur quarante-cinq vignettes, elle placarde
le même visage partout ; recadrée au centre, elle tranche la ligne des compteurs. D'où un
recadrage à 139 % calé en haut à gauche, dont les proportions venaient de mesures faites sur les
cartes réelles. La vitrine, elle, la refusait déjà : sa bande est trop large pour ce recadrage.

La direction visuelle retenue (ADR-0008) rend le problème insoluble en l'état : sur un fond de
console, une tuile claire est ce qu'on voit en premier, et quarante-cinq en font un mur.

## Options envisagées

### A — Garder la carte, repeindre le repli aux couleurs de la console

Le repli suit la nouvelle peau, la carte reste l'illustration principale.

**Avantages** — aucun changement de comportement, aucun test à revoir.
**Inconvénients** — les deux couches se contredisent : le repli dit « console », la carte dit
« GitHub ». Et comme la carte arrive presque toujours, c'est elle qu'on voit : la peau ne tient
que sur les vignettes en échec. Le recadrage et ses mesures restent à maintenir.
**Coût** — faible, mais le résultat ne répond pas à la question posée.

### B — Dessiner la vignette, ne plus afficher la carte du tout

L'illustration est dessinée à partir de ce qui distingue un dépôt d'un autre : son langage —
d'où la teinte — et son nom — d'où l'inclinaison du dégradé. Les captures du fichier éditorial
se posent toujours par-dessus.

**Avantages** — une seule facture visuelle ; quarante-cinq requêtes vers un tiers en moins par
page d'accueil ; le recadrage, sa classe et ses mesures disparaissent ; la grille se lit d'un
coup d'œil, la teinte disant le langage avant le mot.
**Inconvénients** — la vignette ne montre plus le dépôt, elle le signale. Pour les dépôts sans
capture, il n'y a plus d'aperçu « réel ». Une table de couleurs par langage est à tenir.
**Coût** — moyen, et une bonne part est du code en moins.

### C — N'afficher que les captures du fichier éditorial

Pas de vignette dessinée : une image ou rien.

**Avantages** — le plus simple, et seules de vraies images s'affichent.
**Inconvénients** — six dépôts sur quarante-sept ont une capture. La grille serait trouée aux
cinq sixièmes.

### D — Ne rien faire

Garder la carte et la peau actuelle. Revient à renoncer à l'ADR-0008.

## Décision

Nous retenons **l'option B**.

Parce que :

1. **Deux factures visuelles ne cohabitent pas.** Une illustration tierce impose son fond, sa
   typographie et son avatar : ou bien le site adopte la sienne, ou bien il l'affiche.
2. **La carte n'apprend rien.** Nom, description et compteurs sont déjà écrits sous la vignette,
   en français ou en anglais selon la page — la carte, elle, ne sait dire que l'anglais.
3. **Le recadrage était une acrobatie.** Des pourcentages calés à l'œil sur la maquette d'un
   tiers, qui pouvaient cesser d'être vrais au premier changement de sa part, sans rien signaler.

## Conséquences

**Positives** — une page d'accueil qui ne dépend plus d'un service tiers pour s'afficher ; le
code de recadrage supprimé ; le paramètre `UsePreviewCard` et la classe `cover__image--framed`
avec lui ; la vignette reste correcte hors ligne.

**Négatives** — on perd l'aperçu réel d'un dépôt qui n'a pas de capture : la vignette signale,
elle ne montre pas. Le remède est éditorial, pas technique — ajouter une capture au fichier
`data/repositories.json` pour les dépôts qui méritent d'être vus.

Deuxième coût : la table des couleurs de `linguist` et le seuil de luminance sont désormais à
nous. Ces couleurs sont faites pour une pastille sur fond clair ; sur la vignette sombre, les
plus foncées disparaissent, et `RepositoryVisual.InkOf` les éclaircit. Un langage ajouté à la
table demande de regarder le résultat, pas seulement de lire la valeur.

**Neutres / à surveiller** — `Repository.PreviewImageUrl` n'a plus de consommateur. Elle est
retirée dans la foulée : une propriété que personne ne lit finit par être lue par erreur. Si un
jour la plupart des dépôts portent une capture, la vignette dessinée deviendra marginale et cet
ADR sera à rouvrir.

## Suivi

- [x] `UsePreviewCard`, `cover__image--framed` et le recadrage sont supprimés.
- [x] `InkOf` est couvert par des tests, teintes sombres comprises.
- [x] Un test vérifie qu'aucune page ne référence `opengraph.githubassets.com`.
- [x] `Repository.PreviewImageUrl` est retirée du modèle et de la couche anti-corruption.
- [x] `CLAUDE.md` est mis à jour : les repères qui décrivaient la carte et son recadrage.
