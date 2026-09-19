---
name: choix-pattern
description: À utiliser avant d'introduire une interface, une abstraction, une hiérarchie ou un design pattern en C#. Filtre YAGNI, cherche l'idiome .NET natif, et n'autorise le pattern GoF qu'en dernier recours. Utiliser proactivement dès qu'une abstraction est envisagée.
---

# Choisir (ou refuser) une abstraction

Procédure obligatoire. **Ne pas sauter d'étape, ne pas écrire de code avant l'étape 5.**

## 1. Décrire le problème sans nommer de pattern

Interdit dans cette étape : « Factory », « Strategy », « Repository »…
Formuler : *ce qui varie*, *ce qui reste stable*, *qui demande le changement*.

> « Le calcul des frais diffère selon le moyen de paiement, et cette sélection apparaît
>   à 3 endroits. Le reste du flux de règlement est identique. »

## 2. Filtre YAGNI

Répondre par écrit :

- Combien d'implémentations **existent réellement aujourd'hui** ? (≥ 2 requis)
- La 3e occurrence est-elle atteinte, ou anticipe-t-on ? (cf. règle de trois)
- Que se passe-t-il si on **ne fait rien** ? Le coût est-il concret ou hypothétique ?

Si une réponse est au futur → **s'arrêter ici**, écrire la version directe, et signaler
à l'utilisateur qu'on a délibérément écarté l'abstraction (avec le déclencheur qui la
justifierait plus tard).

## 3. Chercher l'idiome .NET natif

Consulter le tableau de `.claude/rules/30-design-patterns.md`. La majorité des patterns
GoF sont déjà dans le langage ou le framework : `Func<>`, `IEnumerable`/`yield`, `record`
+ `with`, DI keyed services, middleware, `event`/`IObservable`, Scrutor `.Decorate`.

Un delegate suffit souvent là où on allait écrire une interface à une méthode.

## 4. Vérifier la direction de la dépendance

Si une interface est créée :
- Est-elle définie dans le projet du **consommateur** (DIP) ?
- Chaque consommateur utilise-t-il **tous** ses membres (ISP) ?
- Isole-t-elle quelque chose de **volatil** (I/O, temps, réseau, tiers) ? Sinon, pourquoi ?

## 5. Proposer — avec le coût

Format de sortie attendu :

```
Problème      : <ce qui varie / ce qui est dupliqué, avec les fichiers concernés>
Options       : A) <direct, sans abstraction>  B) <idiome .NET>  C) <pattern GoF>
Retenu        : <option> — <pourquoi>
Coût          : <indirection, fichiers, difficulté de debug>
Bénéfice      : <gain constaté AUJOURD'HUI, pas au futur>
Écarté        : <ce qu'on n'a pas fait, et le déclencheur qui le justifierait>
```

Si l'option retenue est A, c'est un **bon résultat**, pas un échec.

## Refus automatiques

Refuser et expliquer, même si l'utilisateur le demande explicitement — en proposant
l'alternative :

- `IRepository<T>` générique au-dessus d'EF Core
- Singleton statique (état global)
- `IServiceProvider` injecté hors composition root (Service Locator)
- `BaseService` / `BaseController` / `*Helper` statique fourre-tout
- Interface à une seule implémentation qui n'est ni un port d'infra ni une frontière
- Généricité `<T>` pour un seul type
