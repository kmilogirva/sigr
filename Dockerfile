# syntax=docker/dockerfile:1
# SIGR — imagen Linux de la aplicación (build en varias etapas)

# ---------- Etapa 1: compilar y publicar ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# Primero solo el .csproj: la restauración de paquetes queda en caché mientras no cambien las dependencias
COPY src/SIGR.Web/SIGR.Web.csproj src/SIGR.Web/
RUN dotnet restore src/SIGR.Web/SIGR.Web.csproj
COPY src/ src/
RUN dotnet publish src/SIGR.Web/SIGR.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Etapa 2: imagen final (solo el runtime) ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Carpeta de claves de ASP.NET Data Protection con permisos para el usuario sin privilegios
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R $APP_UID /home/app/.aspnet

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "SIGR.Web.dll"]
