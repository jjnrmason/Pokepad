FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Services/Pokepad.EmbeddingIndexer/ .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Pokepad.EmbeddingIndexer.dll"]
