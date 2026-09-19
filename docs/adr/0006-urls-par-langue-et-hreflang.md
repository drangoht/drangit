# ADR-0006 — Une adresse par langue, et des `hreflang` réciproques

- **Statut** : accepté
- **Date** : 2026-09-17

## Contexte

Le site est bilingue. Trois façons de porter la langue :

1. un cookie, posé par un sélecteur ;
2. la négociation de contenu (`Accept-Language`) sur une adresse unique ;
3. un préfixe dans le chemin : `/fr/…`, `/en/…`.

Les deux premières partagent le même défaut : **une seule adresse pour deux contenus**. Un
moteur de recherche n'en indexe qu'une version, un lien partagé ne transmet pas la langue dans
laquelle il a été lu, et la mise en cache par un proxy devient fausse.

## Décision

**Option 3.** La langue vit dans le chemin.

Le préfixe est détaché à l'entrée du pipeline et porté en `PathBase`. Conséquence : **aucune
route `@page` ne connaît la langue**, et `<base href>` la porte pour toute la page.

- La racine `/` **oriente** vers la langue négociée, par une redirection **temporaire** : c'est
  la seule adresse dont la destination dépend du visiteur, et une redirection permanente la
  figerait dans son navigateur — le francophone qui l'a visitée une fois n'atteindrait plus
  jamais l'anglais.
- Les autres adresses non préfixées redirigent **définitivement** vers l'anglais.
- Le sélecteur de langue est fait de **liens**, pas d'un formulaire : changer de langue, c'est
  suivre un lien vers la même page sous l'autre préfixe. Rien à mémoriser, rien à poster.

## Les deux conséquences à ne pas perdre de vue

**Tout lien interne doit être relatif.** `<base href>` porte le préfixe ; un `href="/repos/x"`
absolu y échappe et renvoie le visiteur en anglais. Seuls les liens du sélecteur de langue sont
absolus, à dessein. Un test d'intégration le vérifie sur chaque page.

**Une page indexable doit être ajoutée au plan du site *et* porter un `SeoHead`**, qui pose sa
canonique et ses `hreflang` réciproques. La réciprocité est la condition pour qu'un groupe de
versions soit pris en compte : une réciprocité manquante fait ignorer tout le groupe, **sans
rien signaler**.

## Conséquences

**Positives** — chaque langue est indexable et partageable ; le cache HTTP redevient correct ;
aucun état côté serveur ni cookie pour une préférence d'affichage.

**Négatives** — un intergiciel à comprendre avant de toucher au routage ; le piège du lien
absolu, qui ne se voit pas à l'œil nu ; deux fois plus d'adresses au plan du site.

**À surveiller** — une troisième langue multiplierait les `hreflang` : le composant `SeoHead`
les génère déjà depuis `SupportedCultures`, mais les traductions, elles, se rédigent à la main.
