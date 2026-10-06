FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:0eeb52c76e35a5431ca707ad2bc75e38006a05393045d8532ae44c15d9474523 AS build
WORKDIR /src

COPY src/WildBunch.Api/WildBunch.Api.csproj src/WildBunch.Api/
COPY src/WildBunch.Application/WildBunch.Application.csproj src/WildBunch.Application/
COPY src/WildBunch.Domain/WildBunch.Domain.csproj src/WildBunch.Domain/
COPY src/WildBunch.Persistence/WildBunch.Persistence.csproj src/WildBunch.Persistence/
COPY src/WildBunch.GameContent/WildBunch.GameContent.csproj src/WildBunch.GameContent/
RUN dotnet restore src/WildBunch.Api/WildBunch.Api.csproj

COPY src/WildBunch.Api/ src/WildBunch.Api/
COPY src/WildBunch.Application/ src/WildBunch.Application/
COPY src/WildBunch.Domain/ src/WildBunch.Domain/
COPY src/WildBunch.Persistence/ src/WildBunch.Persistence/
COPY src/WildBunch.GameContent/ src/WildBunch.GameContent/
ARG WILD_BUNCH_RELEASE
RUN dotnet publish src/WildBunch.Api/WildBunch.Api.csproj --configuration Release --no-restore --output /out /p:UseAppHost=false \
    && rm -f /out/appsettings.Development.json \
    && printf '%s\n' "$WILD_BUNCH_RELEASE" > /out/release.txt

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:0fa044f682cb7d93a5a90401a00c626c66f7b00b86922be9441f869eae039f80 AS runtime
WORKDIR /app
COPY --from=build --chown=app:app /out/ ./
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "WildBunch.Api.dll"]
