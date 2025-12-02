FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FootballPointsApp.csproj", "."]
RUN dotnet restore "./FootballPointsApp.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "FootballPointsApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FootballPointsApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Ensure wwwroot exists
COPY --from=build /src/wwwroot ./wwwroot
ENTRYPOINT ["dotnet", "FootballPointsApp.dll"]
