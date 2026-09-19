# syntax=docker/dockerfile:1

# --- Étape de compilation ----------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /source

# Les fichiers projet d'abord : tant qu'aucune dépendance ne change, la couche de
# restauration est réutilisée telle quelle d'un build à l'autre.
COPY Directory.Build.props Directory.Build.targets global.json ./
COPY src/Drangit.slnx src/
COPY src/Drangit.Web/Drangit.Web.csproj src/Drangit.Web/
COPY tests/Drangit.Tests/Drangit.Tests.csproj tests/Drangit.Tests/
RUN dotnet restore src/Drangit.slnx

COPY . .

# La suite tourne dans l'image : une image ne se construit pas sur du code rouge.
RUN dotnet test --solution src/Drangit.slnx --configuration $BUILD_CONFIGURATION --no-restore

RUN dotnet publish src/Drangit.Web/Drangit.Web.csproj \
        --configuration $BUILD_CONFIGURATION \
        --no-restore \
        --output /app

# Le répertoire de l'instantané est préparé ici : l'image finale n'a pas de shell,
# donc plus aucun `RUN` n'y est possible.
RUN mkdir -p /snapshot-dir

# --- Image d'exécution --------------------------------------------------------
# Image « chiseled » : ni shell, ni gestionnaire de paquets, surface d'attaque réduite.
# Variante « extra », car le site est bilingue et formate dates et nombres selon la
# culture du visiteur : cela demande la globalisation ICU.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final

WORKDIR /app
COPY --from=build --chown=$APP_UID:$APP_UID /app .

# Point de montage de l'instantané de repli. Il appartient à l'utilisateur non privilégié :
# monté par Docker, il serait sinon possédé par root et l'application ne pourrait pas y écrire.
COPY --from=build --chown=$APP_UID:$APP_UID /snapshot-dir /var/lib/drangit

ENV ASPNETCORE_HTTP_PORTS=8080 \
    # GC « workstation » : sur un petit VPS partagé, le GC serveur réserve un tas par cœur
    # pour un site dont la charge ne le justifie pas.
    DOTNET_gcServer=0 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

# Pas de HEALTHCHECK ici : l'image chiseled n'embarque ni shell, ni curl, ni wget pour
# l'exécuter. La sonde /health est interrogée de l'extérieur — par la CI après déploiement,
# et par le reverse proxy.
USER $APP_UID

ENTRYPOINT ["dotnet", "Drangit.Web.dll"]
