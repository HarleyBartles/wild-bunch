FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:0eeb52c76e35a5431ca707ad2bc75e38006a05393045d8532ae44c15d9474523 AS build
WORKDIR /src
COPY deploy/database/certs/eu-north-1-bundle.pem /tmp/eu-north-1-rds-ca-bundle.pem
RUN printf '%s  %s\n' 8d8dc42958c7b9351846609d552b4b38f3b19c92c9d4816b2f814fd148fe18be /tmp/eu-north-1-rds-ca-bundle.pem | sha256sum -c -

COPY .config/dotnet-tools.json .config/
COPY src/WildBunch.Api/WildBunch.Api.csproj src/WildBunch.Api/
COPY src/WildBunch.Application/WildBunch.Application.csproj src/WildBunch.Application/
COPY src/WildBunch.Domain/WildBunch.Domain.csproj src/WildBunch.Domain/
COPY src/WildBunch.Persistence/WildBunch.Persistence.csproj src/WildBunch.Persistence/
COPY src/WildBunch.GameContent/WildBunch.GameContent.csproj src/WildBunch.GameContent/
RUN dotnet tool restore \
    && dotnet restore src/WildBunch.Api/WildBunch.Api.csproj --runtime linux-x64

COPY src/WildBunch.Api/ src/WildBunch.Api/
COPY src/WildBunch.Application/ src/WildBunch.Application/
COPY src/WildBunch.Domain/ src/WildBunch.Domain/
COPY src/WildBunch.Persistence/ src/WildBunch.Persistence/
COPY src/WildBunch.GameContent/ src/WildBunch.GameContent/
ARG WILD_BUNCH_RELEASE
RUN dotnet ef migrations bundle \
    --project src/WildBunch.Persistence/WildBunch.Persistence.csproj \
    --startup-project src/WildBunch.Api/WildBunch.Api.csproj \
    --configuration Release \
    --self-contained \
    --runtime linux-x64 \
    --output /out/efbundle \
    && printf '%s\n' "$WILD_BUNCH_RELEASE" > /out/release.txt

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:0fa044f682cb7d93a5a90401a00c626c66f7b00b86922be9441f869eae039f80 AS runtime
WORKDIR /app
COPY --chown=app:app src/WildBunch.Api/appsettings.json ./appsettings.json
COPY --from=build --chown=app:app /out/ ./
COPY --from=build --chown=app:app /tmp/eu-north-1-rds-ca-bundle.pem /app/certs/rds-ca-bundle.pem
USER $APP_UID
ENTRYPOINT ["./efbundle"]
