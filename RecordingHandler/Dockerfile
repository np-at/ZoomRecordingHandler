FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS base
RUN apk --no-cache add curl
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /src
COPY ["RecordingHandler/RecordingHandler.csproj", "RecordingHandler/"]
RUN dotnet restore "RecordingHandler/RecordingHandler.csproj"
COPY . .
WORKDIR "/src/RecordingHandler"
ARG BUILD_CONFIG=Release
RUN dotnet build "RecordingHandler.csproj" -c $BUILD_CONFIG -r "linux-musl-x64" -o /app/build

FROM build AS publish
ARG BUILD_CONFIG=Release
RUN dotnet publish "RecordingHandler.csproj" -c $BUILD_CONFIG -r "linux-musl-x64" -o /app/publish

FROM base AS final
ENV NODE_ENV=production
ENV ASPNETCORE_URLS="http://+:8080"
ARG BUILD_ENV=prod
WORKDIR /app
COPY --from=publish /app/publish .

RUN adduser -D hostinguser
RUN chown -R hostinguser /app
USER hostinguser
HEALTHCHECK CMD curl --fail http://localhost:8080/healthcheck || exit 1
ENTRYPOINT ["dotnet", "RecordingHandler.dll"]
