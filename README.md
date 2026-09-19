# Drangit

Site vitrine des dépôts publics de [github.com/drangoht](https://github.com/drangoht).

La liste vient de l'API GitHub au runtime, mise en cache, complétée par un fichier éditorial
versionné et doublée d'un instantané sur disque : le site continue de répondre quand l'API est
indisponible ou que le quota d'appels est épuisé. Bilingue FR/EN, rendu côté serveur, déployé
en conteneur.

---

## Démarrer

```bash
git clone https://github.com/drangoht/drangit.git
cd drangit

cd src/Drangit.Web
dotnet run
```

Le site écoute sur <http://localhost:5180> et oriente vers `/fr/` ou `/en/` selon la langue du
navigateur.

**Aucun secret n'est nécessaire.** Sans jeton, l'API GitHub anonyme accorde 60 requêtes par
heure et par adresse IP ; avec un cache de trente minutes, le site en consomme deux au pire
(ADR 0003). Pour lever le quota malgré tout :

```bash
cd src/Drangit.Web
dotnet user-secrets set "GitHub:Token" "<jeton>"
```

Le jeton n'a besoin d'**aucune portée** : le site ne lit que des dépôts publics. Un jeton
*fine-grained* sans permission convient — et c'est celui qu'il faut, un jeton classique avec la
portée `repo` donnerait accès à des dépôts privés dont le site n'a que faire.

---

## Construire et tester

```bash
dotnet build src/Drangit.slnx -warnaserror
dotnet test  --solution src/Drangit.slnx
dotnet format src/Drangit.slnx --verify-no-changes   # avant tout commit

# Tout sauf les tests d'intégration
dotnet test --solution src/Drangit.slnx -- --filter-not-trait "Category=Integration"
```

Les tests s'exécutent sur **Microsoft.Testing.Platform** (déclaré dans `global.json`) : les
options passent après `--`. Si `dotnet test` ne découvre rien sur votre machine, le projet de
tests est aussi un exécutable :

```bash
dotnet run --project tests/Drangit.Tests
```

---

## Configuration

| Clé | Environnement | Défaut | Rôle |
|---|---|---|---|
| `GitHub:Login` | `GitHub__Login` | `drangoht` | compte dont les dépôts publics sont exposés |
| `GitHub:Token` | `GitHub__Token` | *(vide)* | jeton facultatif ; lève le quota de 60 à 5000 appels/h |
| `GitHub:CacheDuration` | `GitHub__CacheDuration` | `00:30:00` | durée de réutilisation de la réponse |
| `GitHub:Timeout` | `GitHub__Timeout` | `00:00:10` | délai maximal d'un appel, reprises comprises |
| `GitHub:MaxPages` | `GitHub__MaxPages` | `5` | borne de la pagination (100 dépôts par page) |
| `GitHub:IncludeForks` | `GitHub__IncludeForks` | `false` | liste aussi les bifurcations |
| `Snapshot:Directory` | `Snapshot__Directory` | `/var/lib/drangit` | où vit l'instantané de repli |
| `Editorial:FilePath` | `Editorial__FilePath` | `data/repositories.json` | contenu éditorial |
| `Site:Name` | `Site__Name` | `Drangit` | nom affiché |
| `Site:GitHubUrl` | `Site__GitHubUrl` | profil GitHub | lien du pied de page |
| `Site:ItchProfileUrl` | `Site__ItchProfileUrl` | *(vide)* | lien itch.io, facultatif |

L'aperçu de partage (`og:image`) d'une page est la capture déclarée pour le dépôt, ou à défaut
`wwwroot/og-share.png`, l'image du site. La carte produite par GitHub n'y sert jamais : elle
incruste l'avatar du compte, et hors du site le recadrage qui l'écarte ne s'applique plus.

La configuration est **validée au démarrage** : un compte mal orthographié ou un cache
aberrant empêche le conteneur de démarrer, au lieu de produire une vitrine vide servie en vert.

---

## Contenu éditorial

`src/Drangit.Web/data/repositories.json` complète ce que l'API ne fournit pas. Tout y est
facultatif ; un dépôt absent s'affiche avec les seules données GitHub.

```jsonc
{
  "repositories": {
    "mon-depot": {                       // le nom du dépôt, en minuscules
      "featured": true,                  // met le dépôt en vitrine sur l'accueil
      "hidden": false,                   // le retire du site (sans rien changer sur GitHub)
      "topics": ["dotnet", "blazor"],    // s'ajoutent aux sujets déclarés sur GitHub
      "demoUrl": "https://demo.exemple/", // seulement si le dépôt n'a pas de « homepage »
      "summary":     { "en": "…", "fr": "…" },   // remplace la description courte
      "description": { "en": "…", "fr": "…" },   // texte long de la fiche
      "screenshots": [{ "url": "https://…/capture.png", "caption": { "fr": "…" } }]
    }
  }
}
```

Les slugs qui ne correspondent à aucun dépôt public sont signalés dans les journaux au premier
chargement du catalogue : une faute de frappe ne passe pas inaperçue — ce qui compte surtout
pour `hidden`, où elle laisserait affiché un dépôt qu'on croit masqué.

Le vocabulaire des sujets est volontairement restreint et partagé : un sujet n'a d'intérêt que
s'il regroupe plusieurs dépôts. Un seul dépôt du compte porte des sujets sur GitHub — sans ce
fichier, la barre de filtres n'aurait rien à proposer. Chaque critère affiche le nombre de
dépôts qu'il retient, et une bascule qui ne partage pas le catalogue en deux n'est pas proposée.

---

## Déploiement

```bash
cp .env.example .env     # puis compléter
docker compose up -d
```

La chaîne complète est décrite dans `.github/workflows/ci-cd.yml` :

1. **Build & tests** — compilation en `-warnaserror`, vérification du format, suite complète.
2. **Image Docker** — construite seulement si la suite est verte, publiée sur GHCR avec une
   étiquette immuable (SHA court) *en plus* de `latest`, puis **éprouvée** : le conteneur est
   démarré et doit servir `/health`, `/en/`, `/fr/`, et orienter `/` vers une langue (ADR 0007).
3. **Déploiement** — copie du `docker-compose.yml`, `.env` **dérivé** des secrets et variables
   du dépôt (le serveur n'a aucune configuration propre à entretenir), `docker compose up -d`,
   puis sonde locale et sonde publique.

### Secrets et variables du dépôt

| Nom | Type | Requis | Rôle |
|---|---|---|---|
| `VPS_HOST`, `VPS_USER`, `VPS_SSH_KEY` | secret | oui | accès SSH au serveur |
| `GH_API_TOKEN` | secret | non | jeton d'API GitHub (voir plus haut) |
| `SITE_URL` | variable | oui | URL publique, vérifiée après déploiement |
| `DEPLOY_PATH` | variable | non | défaut `/opt/drangit` |
| `HTTP_PORT` | variable | non | défaut `8082` |
| `GH_LOGIN`, `GH_CACHE_DURATION`, `SITE_NAME`, `SITE_GITHUB_URL`, `SITE_ITCH_URL` | variable | non | surchargent les défauts |

> Les noms côté dépôt sont en `GH_*` et non `GITHUB_*` : GitHub réserve ce préfixe et refuse
> de créer un secret ou une variable qui le porte. Seul le `.env` écrit sur le serveur emploie
> les noms attendus par le `docker-compose.yml`.

### Reverse proxy et certificat

Le conteneur n'écoute que sur le port `8082` de la machine ; nginx porte le TLS et publie le
site. `/etc/nginx/sites-available/drangit.thognard.net` :

```nginx
server {
    listen 80;
    listen [::]:80;
    server_name drangit.thognard.net;

    location / {
        proxy_pass http://127.0.0.1:8082;
        proxy_http_version 1.1;

        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/drangit.thognard.net /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
sudo certbot --nginx -d drangit.thognard.net --redirect
```

Certbot ajoute lui-même le bloc TLS, la redirection HTTP → HTTPS, et installe la tâche de
renouvellement.

**Les quatre `proxy_set_header` ne sont pas décoratifs.** L'application reconstruit son
adresse publique à partir d'eux (`UseForwardedHeaders` dans `Program.cs`) : sans `Host` et
`X-Forwarded-Proto`, l'adresse canonique, les `hreflang`, le plan du site et les aperçus de
partage porteraient `http://` et le nom du conteneur. Le site s'afficherait normalement —
seuls les moteurs de recherche verraient le problème.

### Revenir en arrière

`Actions → CI/CD → Run workflow`, en renseignant `rollback_tag` avec le SHA court d'une image
déjà publiée. Rien n'est reconstruit : l'image existante est redéployée.

---

## Architecture

```
src/Drangit.Web/
├── Repositories/            modèle + port IRepositoryCatalog + filtres
│   ├── GitHub/              couche anti-corruption (Refit) — seul endroit qui parle GitHub
│   ├── Editorial/           fusion avec data/repositories.json
│   └── Snapshots/           instantané de repli sur disque
├── Localization/            préfixe de langue du chemin (ADR 0006)
├── Seo/                     sitemap, robots.txt, JSON-LD
├── Components/              pages et composants Blazor SSR statiques
└── Resources/               traductions FR/EN
```

Un seul projet applicatif, découpé par dossiers plutôt qu'en assemblies : c'est une dérogation
assumée, documentée dans l'**ADR 0002** et tenue par des tests d'architecture.

Les décisions structurantes sont dans [`docs/adr/`](docs/adr) :

| ADR | Sujet |
|---|---|
| [0001](docs/adr/0001-utiliser-des-adr.md) | consigner les décisions |
| [0002](docs/adr/0002-solution-mono-projet.md) | un seul projet applicatif |
| [0003](docs/adr/0003-source-de-donnees-github.md) | API GitHub publique, jeton facultatif |
| [0004](docs/adr/0004-cache-et-repli-sur-instantane.md) | cache mémoire et instantané disque |
| [0005](docs/adr/0005-refit-pour-le-transport-http.md) | Refit pour le transport HTTP |
| [0006](docs/adr/0006-urls-par-langue-et-hreflang.md) | une adresse par langue, `hreflang` réciproques |
| [0007](docs/adr/0007-eprouver-l-image-avant-de-deployer.md) | éprouver l'image avant de déployer |

---

## Ajouter une page

1. Créer le composant sous `Components/Pages/`.
2. Lui donner un `<SeoHead>` — canonique et `hreflang` réciproques.
3. L'ajouter à `Seo/Sitemap.cs`, **une entrée par langue**.
4. Ajouter son chemin à `CulturePrefix.TranslatedPaths` pour que l'adresse non préfixée
   redirige.
5. Ajouter ses libellés dans **les deux** `.resx`.
6. L'ajouter à la liste des pages éprouvées dans `.github/workflows/ci-cd.yml`.
