# Conventions C# / .NET

## Configuration du projet — non négociable

`Directory.Build.props` à la racine de `src/` :

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

Analyzers : `Microsoft.CodeAnalysis.NetAnalyzers`, `SonarAnalyzer.CSharp`, et
`Roslynator.Analyzers` si l'équipe le souhaite. Un `.editorconfig` versionné fait foi ;
`dotnet format --verify-no-changes` en CI.

**Ne jamais supprimer un warning avec `#pragma warning disable` sans commentaire
justificatif ni ticket.**

## Nullabilité

- `Nullable` activé partout. Un warning de nullabilité est une erreur.
- `!` (null-forgiving) interdit sauf commentaire qui explique l'invariant garanti.
- Validation des entrées publiques : `ArgumentNullException.ThrowIfNull(x);`
- Ne jamais retourner `null` pour une collection → `[]` / `Array.Empty<T>()`.

## Types

- `record` pour les DTOs et les value objects (égalité structurelle, `with`).
- `readonly record struct` pour les petits VO à forte volumétrie (`Money`, `Quantity`).
- `sealed` par défaut sur les classes. L'héritage est une décision, pas un défaut.
- `required` (C# 11) pour les propriétés obligatoires plutôt qu'un constructeur à 9 paramètres.
- **Immutabilité par défaut** : `init` plutôt que `set`, `IReadOnlyList<T>` en type de retour.
- Primary constructors : acceptables pour l'injection dans une classe scellée ; **pas** sur
  les entités de domaine (ils encouragent l'exposition de l'état).

## Style

- `var` quand le type est évident à droite de `=`, type explicite sinon.
- Pattern matching (`switch` expression, `is`) plutôt qu'une cascade de `if`.
- Expressions-bodied members pour les one-liners triviaux seulement.
- Pas de `#region`. Un fichier qui a besoin de régions a besoin d'être coupé.
- Un type public par fichier, nom de fichier = nom du type.
- Champs privés `_camelCase`, tout le reste `PascalCase`, interfaces préfixées `I`.
- Suffixe `Async` **uniquement** sur les méthodes qui retournent `Task`/`ValueTask`.

## Déterminisme

- `TimeProvider` (.NET 8+) au lieu de `DateTime.Now` / `UtcNow`. Injecté, jamais statique.
- `DateTimeOffset` plutôt que `DateTime` pour tout instant persisté ou échangé. UTC en base.
- Décimaux monétaires : `decimal`, jamais `double`. Idéalement un VO `Money` portant la devise —
  additionner deux devises différentes doit être impossible à la compilation ou lever.
- `Guid.CreateVersion7()` (.NET 9) pour les identifiants persistés (meilleure localité d'index).

## Collections & LINQ

- Type de retour : la plus faible abstraction utile (`IEnumerable<T>` pour un flux,
  `IReadOnlyList<T>` pour une collection matérialisée).
- Ne pas énumérer deux fois un `IEnumerable<T>` (bug classique : requête rejouée).
- LINQ lisible > LINQ malin. Une chaîne de 8 opérateurs se coupe en variables nommées.
- Pas de `.Result` ni de logique asynchrone à l'intérieur d'un `Select`.

## Logging

- Logging **structuré** : `logger.LogInformation("Paiement {PaymentId} rejeté : {Reason}", id, reason);`
  Jamais d'interpolation `$"…"` — elle casse l'indexation et coûte même quand le niveau est désactivé.
- Ne jamais journaliser de données sensibles (PAN, IBAN complet, token, données personnelles).
- Une exception se journalise **une fois**, au niveau qui la traite. Pas à chaque étage.
- `LoggerMessage` source-generated sur les chemins chauds.

## Configuration

- Options fortement typées (`IOptions<T>`) validées au démarrage
  (`.ValidateDataAnnotations().ValidateOnStart()`). Une configuration invalide fait échouer
  le démarrage, pas la première requête en production.
- Aucun secret dans le code ni dans `appsettings.json`.
