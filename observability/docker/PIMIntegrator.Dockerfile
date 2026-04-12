FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/FactoryObservability.Shared/FactoryObservability.Shared.csproj src/FactoryObservability.Shared/
COPY src/PIMIntegrator/PIMIntegrator.csproj src/PIMIntegrator/
RUN dotnet restore src/PIMIntegrator/PIMIntegrator.csproj
COPY src/ src/
RUN dotnet publish src/PIMIntegrator/PIMIntegrator.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PIMIntegrator.dll"]
