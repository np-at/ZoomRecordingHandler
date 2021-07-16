FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS base
RUN apk --no-cache add curl
WORKDIR /app
EXPOSE 8080
FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /src
COPY ["ZoomFileManager/ZoomFileManager.csproj", "ZoomFileManager/"]
RUN dotnet restore "ZoomFileManager/ZoomFileManager.csproj"
COPY . .
WORKDIR "/src/ZoomFileManager"
ARG BUILD_CONFIG=Release
RUN dotnet build "ZoomFileManager.csproj" -c $BUILD_CONFIG -r "linux-musl-x64" -o /app/build

FROM build AS publish
ARG BUILD_CONFIG=Release
RUN dotnet publish "ZoomFileManager.csproj" -c $BUILD_CONFIG -r "linux-musl-x64" -o /app/publish

FROM base AS final
ENV NODE_ENV=production

WORKDIR /app
COPY --from=publish /app/publish .
ARG BUILD_ENV=prod
RUN if [ "${BUILD_ENV}" = "prod" ]; then ln -f -s /run/secrets/zoomFileManager_settings appsettings.json; fi
RUN adduser -D hostinguser
RUN chown -R hostinguser /app
USER hostinguser
HEALTHCHECK CMD curl --fail http://localhost:8080/healthcheck || exit 1
ENTRYPOINT ["dotnet", "ZoomFileManager.dll"]
