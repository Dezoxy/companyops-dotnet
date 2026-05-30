# Multi-stage build for the API. Build context is the repo root (see infra/docker-compose.yml).
# Also serves as the one-shot migrator image: `dotnet CompanyOps.Api.dll --migrate`.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json ./
COPY src/ src/
RUN dotnet publish src/CompanyOps.Api/CompanyOps.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CompanyOps.Api.dll"]
