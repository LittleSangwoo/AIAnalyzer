# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY ["AIAnalyzer/Certificates/russian_trusted_root_ca.crt", "/usr/local/share/ca-certificates/russian_trusted_root_ca.crt"]
RUN apt-get update && apt-get install -y ca-certificates && update-ca-certificates

USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AIAnalyzer/AIAnalyzer.csproj", "AIAnalyzer/"]
RUN dotnet restore "./AIAnalyzer/AIAnalyzer.csproj"
COPY . .
WORKDIR "/src/AIAnalyzer"
RUN dotnet build "./AIAnalyzer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AIAnalyzer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root
RUN chown -R app:app /app

USER app
ENTRYPOINT ["dotnet", "AIAnalyzer.dll"]