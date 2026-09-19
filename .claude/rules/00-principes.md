# Principes — hiérarchie de décision

Quand deux principes s'opposent, trancher **dans cet ordre** :

1. **Ça marche et c'est couvert par un test.** Un design élégant non testé ne vaut rien.
2. **C'est lisible par le prochain développeur.** Le code est lu 10 fois plus qu'il n'est écrit.
3. **C'est simple.** KISS et YAGNI battent l'extensibilité spéculative.
4. **C'est SOLID** — là où la douleur du changement est réelle et constatée.
5. **Ça porte un nom de pattern** — seulement si le pattern décrit ce qui est déjà là.

> Autrement dit : **YAGNI bat SOLID bat les patterns.** L'ordre inverse produit des
> architectures « astronautes » : 6 interfaces, 4 factories, 0 fonctionnalité livrée.

## YAGNI (You Aren't Gonna Need It)

On n'écrit que ce qu'exige le besoin **d'aujourd'hui**.

Interdits explicites :
- Un point d'extension « au cas où » (interface à une seule implémentation qui n'est ni
  un port de test ni une frontière de couche).
- Un paramètre de configuration jamais changé.
- Une généricité `<T>` introduite pour un seul type.
- Un `IRepository<T>` générique, un `BaseService`, un `Helper` fourre-tout.

Exception légitime : les **frontières** (ports vers l'infrastructure, contrats publics).
Là, l'abstraction paie immédiatement en testabilité — pas plus tard, sur hypothèse.

## Règle de trois

| Occurrence | Action |
|---|---|
| 1re | Écrire le code, inline. |
| 2e | **Dupliquer.** Oui, dupliquer. On ne connaît pas encore l'axe de variation. |
| 3e | Factoriser — maintenant on voit ce qui varie et ce qui reste stable. |

Corollaire : *une mauvaise abstraction coûte plus cher que de la duplication.*
Défaire une abstraction prématurée est plus long que factoriser trois copies.

## DRY, correctement compris

DRY porte sur la **connaissance**, pas sur les caractères. Deux blocs identiques qui
répondent à deux règles métier différentes ne sont pas une duplication : ils vont
diverger. Les fusionner crée un couplage accidentel.

Question à poser : « si cette règle change, les deux endroits doivent-ils changer
**ensemble et toujours** ? » Si non → laisser dupliqué.

## KISS et loi de Déméter

- Une fonction fait une chose, à un seul niveau d'abstraction.
- `commande.Client.Adresse.Ville.Code` = train wreck. Demander, ne pas fouiller.

## Boy Scout Rule

Laisser le code un peu plus propre qu'on ne l'a trouvé — **dans un commit séparé**.
Un commit qui mélange renommage massif et correction de bug est irrelisable en revue
et impossible à revert proprement.

## Coût explicite

Toute abstraction introduite s'accompagne d'une phrase :
« ceci coûte *<indirection / fichier en plus / debug plus dur>* et le paie par
*<gain concret constaté aujourd'hui>* ». Si la seconde moitié est au futur,
l'abstraction ne passe pas.
