FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 31300
# Create directories with proper permissions BEFORE switching user
RUN mkdir -p /data && \
    chown -R $APP_UID:$APP_UID /data && \
    chmod -R 755 /data
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["JobAppRazorWeb.csproj", "./"]
RUN dotnet restore "JobAppRazorWeb.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "JobAppRazorWeb.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "JobAppRazorWeb.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
VOLUME ["/data"]
ENTRYPOINT ["dotnet", "JobAppRazorWeb.dll"]
