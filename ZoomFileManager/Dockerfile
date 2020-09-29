FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS base
WORKDIR /app
EXPOSE 8080
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["ZoomFileManager/ZoomFileManager.csproj", "ZoomFileManager/"]
RUN dotnet restore "ZoomFileManager/ZoomFileManager.csproj"
COPY . .
WORKDIR "/src/ZoomFileManager"
RUN dotnet build "ZoomFileManager.csproj" -c Release -r "linux-musl-x64" -o /app/build

FROM build AS publish
RUN dotnet publish "ZoomFileManager.csproj" -c Release -r "linux-musl-x64" -o /app/publish

FROM base AS final
ARG BUILD_ENV=prod
ENV NODE_ENV=production

WORKDIR /app
COPY --from=publish /app/publish .
RUN if [ "${BUILD_ENV}" = "prod" ]; then ln -f -s /run/secrets/zoomFileManager_settings appsettings.json; fi
RUN adduser -D hostinguser
RUN chown -R hostinguser /app
USER hostinguser
HEALTHCHECK CMD curl --fail http://localhost:8080/healthcheck || exit
ENTRYPOINT ["dotnet", "ZoomFileManager.dll"]
