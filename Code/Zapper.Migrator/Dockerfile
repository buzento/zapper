FROM mcr.microsoft.com/dotnet/sdk:10.0.402-noble AS build
WORKDIR /source/Code
COPY --from=docs AcceptedVulnerabilities.md /source/Docs/
COPY . .
RUN dotnet restore Zapper.Migrator/Zapper.Migrator.csproj --locked-mode
RUN dotnet publish Zapper.Migrator/Zapper.Migrator.csproj --no-restore --configuration Release --output /app

FROM mcr.microsoft.com/dotnet/runtime:10.0.13-noble-chiseled
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Zapper.Migrator.dll"]
