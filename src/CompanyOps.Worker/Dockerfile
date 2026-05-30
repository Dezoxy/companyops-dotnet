# Multi-stage build for the Worker (no web surface → runtime image). Context = repo root.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json ./
COPY src/ src/
RUN dotnet publish src/CompanyOps.Worker/CompanyOps.Worker.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "CompanyOps.Worker.dll"]
