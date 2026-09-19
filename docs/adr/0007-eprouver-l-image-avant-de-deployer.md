# ADR-0007 — Éprouver l'image sur ses pages réelles avant de déployer

- **Statut** : accepté
- **Date** : 2026-09-17

## Contexte

La suite de tests est verte, l'image se construit : cela ne dit pas qu'elle *sert* le site. Un
fichier de contenu oublié dans le publish, une option de configuration absente du modèle, un
répertoire d'instantané inaccessible à l'utilisateur non privilégié — aucun de ces défauts
n'apparaît avant le premier démarrage du conteneur.

La sonde `/health` ne les rattrape pas : elle est **volontairement indépendante de GitHub**,
pour ne pas faire redémarrer le conteneur en boucle quand une API tierce tombe. Elle répond
donc 200 alors que toute page listant des dépôts échouerait.

## Décision

Entre la construction et le déploiement, la CI **démarre l'image** et lui demande ses pages.

- `/health`, `/en/` et `/fr/` doivent répondre 200. **Les deux langues** : l'une peut casser
  sans l'autre — une clé de traduction absente d'un seul `.resx`, par exemple.
- `/` doit répondre 302 **vers un préfixe de langue** (ADR-0006). Une image qui la servirait
  en 200 serait cassée sans que rien d'autre ne le voie.
- **Aucun secret n'entre dans cette étape**, et le compte interrogé est volontairement
  inexistant : GitHub répond 404, aucun instantané n'existe, et l'application doit alors servir
  une vitrine vide sans faillir. C'est le pire cas ; s'il tient, le cas nominal tient aussi.

Le déploiement lui-même répète cette vérification, deux fois : une **sonde locale** depuis le
serveur, puis une **sonde publique** sur l'URL du site. C'est ce qui sépare « l'application ne
répond pas » de « la route publique ne mène pas jusqu'à elle » — sans quoi une panne applicative
et un reverse proxy mal configuré échouent de la même façon, sans rien dire de la cause.

## Conséquences

**Positives** — une image cassée ne part jamais en production ; un échec de déploiement indique
où chercher.

**Négatives** — une à deux minutes de plus par exécution ; une étape qui dépend de Docker sur le
runner.

**À surveiller** — la liste des pages éprouvées est écrite à la main dans le workflow : une page
publiée sans y être ajoutée n'est pas couverte.
