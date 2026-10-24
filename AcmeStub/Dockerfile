FROM mcr.microsoft.com/dotnet/sdk:10.0.402-noble AS build
WORKDIR /source
COPY AcmeStub.csproj .
RUN dotnet restore AcmeStub.csproj
COPY . .
RUN dotnet publish AcmeStub.csproj --no-restore --configuration Release --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0.13-noble-chiseled
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "AcmeStub.dll"]
