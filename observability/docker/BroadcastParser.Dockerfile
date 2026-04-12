FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/FactoryObservability.Shared/FactoryObservability.Shared.csproj src/FactoryObservability.Shared/
COPY src/BroadcastParser/BroadcastParser.csproj src/BroadcastParser/
RUN dotnet restore src/BroadcastParser/BroadcastParser.csproj
COPY src/ src/
RUN dotnet publish src/BroadcastParser/BroadcastParser.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BroadcastParser.dll"]
