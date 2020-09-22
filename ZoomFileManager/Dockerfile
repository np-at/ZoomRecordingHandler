#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS base
WORKDIR /app
EXPOSE 80
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
WORKDIR /app
COPY --from=publish /app/publish .
RUN ln -f -s /run/secrets/zoomFileManager_settings appsettings.json
ENTRYPOINT ["dotnet", "ZoomFileManager.dll"]
