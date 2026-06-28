# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# ВАЖНО: Visual Studio при сборке смотрит на проект из папки решения (на уровень выше),
# поэтому добавляем префикс "AIAnalyzer/" к имени файла сертификата.
COPY ["AIAnalyzer/Certificates/russian_trusted_root_ca.crt", "/usr/local/share/ca-certificates/russian_trusted_root_ca.crt"]
RUN apt-get update && apt-get install -y ca-certificates && update-ca-certificates

# Только ПОСЛЕ установки сертификатов переключаемся на безопасного пользователя
USER $APP_UID

# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AIAnalyzer/AIAnalyzer.csproj", "AIAnalyzer/"]
RUN dotnet restore "./AIAnalyzer/AIAnalyzer.csproj"
COPY . .
WORKDIR "/src/AIAnalyzer"
RUN dotnet build "./AIAnalyzer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AIAnalyzer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Временно переключаемся на root, чтобы раздать права на всю папку
USER root
RUN chown -R app:app /app

# Возвращаемся на безопасного пользователя
USER app
ENTRYPOINT ["dotnet", "AIAnalyzer.dll"]