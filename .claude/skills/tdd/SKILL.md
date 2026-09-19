---
name: tdd
description: Boucle TDD stricte red-green-refactor pour C#/.NET. À utiliser dès qu'un comportement de production doit être implémenté ou modifié. Interdit d'écrire l'implémentation avant un test qui échoue.
argument-hint: [comportement à implémenter]
---

# Boucle TDD

## Contrat

Aucune ligne de code de production n'est écrite tant qu'un test **exécuté et rouge** ne
l'exige. Si l'utilisateur demande d'écrire l'implémentation d'abord, le signaler une fois,
puis suivre sa décision — mais ne jamais prétendre avoir fait du TDD.

## Cycle — une itération par comportement

### 1. RED

- Écrire **un seul** test, nommé `Methode_Contexte_ResultatAttendu`.
- Il doit décrire un comportement observable, pas une implémentation.
- **Lancer les tests.** Montrer le message d'échec.
- Vérifier que l'échec est le bon : une assertion qui échoue, pas un `NullReferenceException`
  imprévu ni une erreur de compilation qu'on n'attendait pas.

```bash
dotnet test --filter "FullyQualifiedName~<NomDuTest>"
```

### 2. GREEN

- Écrire **le minimum** pour passer. Le code laid est autorisé ici.
- Ne pas implémenter le cas suivant « puisqu'on y est ».
- Relancer : vert.

### 3. REFACTOR

**Étape obligatoire, pas facultative.** Avec la suite verte :

- Renommer ce qui est mal nommé (production **et** test).
- Extraire une méthode dès qu'un commentaire semble nécessaire.
- Supprimer la duplication apparue **entre les trois derniers tests** aussi bien que dans
  la production.
- Relancer après **chaque** modification. Rouge = on annule, on ne poursuit pas.

### 4. Boucler

Cas suivant : cas limite, erreur, valeur nulle/vide, borne. Faire les cas d'erreur tôt,
ils dictent souvent la signature.

## Le test d'abord dicte le design

Si le test est pénible à écrire, **c'est le design qui est en cause**, pas le test :

| Douleur | Diagnostic |
|---|---|
| Il faut mocker 6 choses | Trop de dépendances → SRP violé |
| Impossible d'instancier sans une base | Logique métier collée à l'infra → Humble Object |
| Le résultat varie selon l'heure | `DateTime.Now` en dur → `TimeProvider` |
| Le test doit vérifier un état interne | Le comportement n'est pas exposé → mauvaise frontière |
| Le test casse à chaque refactoring | Il teste l'implémentation → remonter d'un niveau |

Dans ces cas : **corriger le design**, ne pas contourner avec un mock supplémentaire.

## Outillage attendu

xUnit + FluentAssertions (ou `Assert` natif, choisir une fois) + NSubstitute/Moq pour les
ports uniquement. Testcontainers pour l'intégration. Voir `.claude/rules/50-tests.md`.

## Rapport de fin

```
Comportement : <ce qui a été implémenté>
Cycles       : <n> (red/green/refactor)
Tests ajoutés: <liste>
Refactorings : <ce qui a été nettoyé pendant la phase refactor>
Non couvert  : <cas limites laissés de côté, et pourquoi>
```
