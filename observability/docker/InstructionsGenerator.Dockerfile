FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/FactoryObservability.Shared/FactoryObservability.Shared.csproj src/FactoryObservability.Shared/
COPY src/InstructionsGenerator/InstructionsGenerator.csproj src/InstructionsGenerator/
RUN dotnet restore src/InstructionsGenerator/InstructionsGenerator.csproj
COPY src/ src/
RUN dotnet publish src/InstructionsGenerator/InstructionsGenerator.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InstructionsGenerator.dll"]
