# Architecture — Clean Architecture appliquée à une solution .NET

## La règle de dépendance

> Les dépendances du code source pointent **uniquement vers l'intérieur**, vers les
> politiques de plus haut niveau. Rien du cercle intérieur ne connaît le cercle extérieur.

Concrètement : une classe de `Domain` ne peut pas contenir un `using Microsoft.EntityFrameworkCore`.
Ce n'est pas une préférence, c'est **vérifiable par un test**.

## Découpage de solution (à adapter, pas à copier aveuglément)

```
src/
  Projet.Domain/          entités, value objects, agrégats, domain events, règles pures
                          → 0 dépendance NuGet d'infrastructure
  Projet.Application/     cas d'usage, orchestration, ports (interfaces), DTOs internes
                          → référence Domain
  Projet.Infrastructure/  EF Core, clients HTTP, RabbitMQ, Azure, implémentation des ports
                          → référence Application
  Projet.Api/             endpoints, composition root, configuration, contrats publics
                          → référence Application + Infrastructure (câblage uniquement)
tests/
  Projet.Domain.Tests/         unitaires purs, aucun I/O, millisecondes
  Projet.Application.Tests/    cas d'usage avec ports en double de test
  Projet.IntegrationTests/     Testcontainers + WebApplicationFactory
  Projet.ArchitectureTests/    la règle de dépendance, exécutable
```

> **Ne pas créer ces 4 projets par réflexe.** Pour un service de 2 000 lignes,
> un projet + un dossier par feature suffit et se découpe plus tard sans douleur.
> Le nombre de projets suit la complexité constatée. Écrire le choix dans un ADR.

## Rendre la règle exécutable

Un commentaire dans un README ne tient pas six mois. Un test, oui.
Utiliser **NetArchTest.Rules** ou **ArchUnitNET** :

```csharp
[Fact]
public void Le_domaine_ne_depend_d_aucune_infrastructure()
{
    var result = Types.InAssembly(typeof(Order).Assembly)
        .ShouldNot()
        .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "System.Net.Http", "RabbitMQ")
        .GetResult();

    result.IsSuccessful.Should().BeTrue(
        string.Join(", ", result.FailingTypeNames ?? []));
}
```

Ajouter au minimum : Domain ne dépend de rien ; Application ne dépend pas
d'Infrastructure ; les handlers sont scellés ; les entités n'ont pas de setter public.

## Screaming Architecture

L'arborescence doit crier le **métier**, pas le framework.

✅ `Payments/`, `Refunds/`, `Kyc/` — et dans chaque dossier, la commande, le handler,
   la validation, le port.
❌ `Controllers/`, `Services/`, `Repositories/`, `Models/` — ça crie « ASP.NET MVC ».

Le découpage vertical par feature limite le couplage et rend le déplacement vers un
autre service (ou un autre repo) mécanique.

## Frontières

- **Aucune entité de domaine dans un contrat public.** Ni en réponse HTTP, ni en payload
  de message. Un DTO explicite par contrat, mappé à la main ou avec un mapper — un
  changement de domaine ne doit jamais casser un consommateur par accident.
- Les contrats de message sont **versionnés** et additifs. Un champ retiré casse les
  consommateurs déjà déployés.
- **Humble Object** : la classe qui touche l'I/O ne contient aucune décision. Elle traduit,
  délègue, retourne. Toute la logique testable en sort.
- **Un seul composition root** (`Program.cs` / `Startup`). C'est le seul endroit qui
  connaît à la fois l'abstraction et l'implémentation concrète.

## DDD tactique (si le domaine le justifie)

- **Agrégat = frontière de cohérence transactionnelle.** Une transaction modifie **un
  seul** agrégat. Entre agrégats : domain event + cohérence à terme.
- L'agrégat est modifié uniquement via sa racine, qui garantit ses invariants. Pas de
  setter public sur une entité.
- **Value object** pour tout ce qui est défini par sa valeur : `Money`, `Iban`, `Email`.
  `record` ou `readonly record struct`. La validation est dans le constructeur — un VO
  invalide ne doit pas pouvoir exister.
- Domain events levés dans l'agrégat, publiés **après** commit (ou via Outbox).
- Ne pas faire de DDD tactique sur du CRUD. Le sur-coût n'est justifié que par une réelle
  complexité de règles.

## Microservices / messagerie

- **Outbox transactionnel** dès qu'un handler écrit en base *et* publie un message.
- **Consumers idempotents** : la livraison est *at-least-once*. Clé d'idempotence
  persistée, pas un `HashSet` en mémoire.
- Retry avec back-off exponentiel + jitter, et une **dead letter queue** surveillée.
- Timeouts explicites sur tout appel réseau. Pas de valeur par défaut implicite.
- Pas de transaction distribuée : saga avec compensation, décrite dans un ADR.
- Traçabilité : `correlationId` propagé de bout en bout (OpenTelemetry).

## Décisions

Tout choix structurant ou toute dérogation aux règles ci-dessus se documente dans un ADR
(`docs/adr/NNNN-titre.md`, skill `adr`). Un ADR est immuable : on ne le modifie pas, on
en écrit un nouveau qui supersède le précédent.
