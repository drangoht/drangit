---
name: revue-clean-code
description: Revue de code C#/.NET selon Clean Code, SOLID et les règles du projet. À utiliser après avoir écrit ou modifié du code, ou pour relire un diff/une PR. Produit un rapport priorisé, sans modifier le code.
argument-hint: [fichier, dossier ou "diff"]
---

# Revue de code

**Ne rien modifier.** Cette skill produit un rapport. Les corrections se font ensuite,
explicitement demandées.

## Périmètre

Par défaut : `git diff` (non commité) puis `git diff main...HEAD`. Si un chemin est donné,
s'y limiter. Ne jamais commenter du code non touché sauf s'il explique le défaut.

## Grille

Parcourir dans cet ordre — s'arrêter au premier niveau qui a des occurrences suffit
rarement, mais l'ordre structure le rapport.

**1. Correction & sécurité**
- Cas limite non couvert, `null` non géré, exception avalée (`catch {}`, `catch (Exception)` muet)
- `throw ex;` au lieu de `throw;`
- Secret, PAN/IBAN/token journalisé ou en dur
- Injection (SQL concaténé, chemin de fichier depuis l'entrée utilisateur)
- Concurrence : état partagé mutable, `async void`, `.Result`, `CancellationToken` non propagé

**2. Architecture**
- Règle de dépendance violée (`using` d'infra dans `Domain`)
- Entité de domaine exposée dans un contrat public
- Interface définie du mauvais côté de la frontière (DIP)
- Service Locator, dépendance statique cachée (`DateTime.Now`, `Guid.NewGuid`)
- Écriture en base + publication de message sans Outbox

**3. SOLID**
- SRP : classe multi-acteurs, > 5-6 dépendances injectées
- OCP : `switch` sur un type **dupliqué** à plusieurs endroits
- LSP : `NotSupportedException` dans un `override`, `is <SousType>` chez l'appelant
- ISP : interface large, implémentations à `NotImplementedException`
- DIP : `new` d'un type volatil dans du code métier

**4. Clean Code**
- Nom qui ment, `Manager`/`Helper`/`Utils`/`Data`
- Fonction à plusieurs niveaux d'abstraction, > 3 paramètres, paramètre booléen
- CQS violé (méthode qui modifie **et** retourne)
- Commentaire qui paraphrase, code commenté, `#region`
- Train wreck (`a.B.C.D`), `null` retourné pour une collection

**5. Tests**
- Comportement ajouté sans test
- Test qui vérifie l'implémentation ou l'ordre des appels d'un mock
- `Thread.Sleep`, dépendance à l'ordre d'exécution
- Mock d'un type non possédé (`DbContext`, `HttpClient`)
- Assertion absente ou triviale

**6. YAGNI**
- Abstraction à une seule implémentation, généricité pour un type, option jamais utilisée
- Pattern appliqué sans deux cas réels

## Format du rapport

Regrouper par sévérité, jamais par fichier.

```
## Bloquant   — à corriger avant merge
- `src/…/PaymentHandler.cs:42` — le CancellationToken reçu n'est pas passé à
  SaveChangesAsync : l'annulation ne fait rien.
  → Correction : `await _db.SaveChangesAsync(ct);`

## Majeur     — dette qui va coûter
## Mineur     — confort de lecture
## À noter    — remarque, pas une demande de changement
```

Chaque point : **fichier:ligne**, ce qui ne va pas, **pourquoi ça coûte**, correction
concrète. Pas de « pensez à respecter SRP ».

Terminer par une ligne de synthèse et, si le code est propre, le dire — un rapport qui
trouve toujours quelque chose perd sa valeur de signal.
