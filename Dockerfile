FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/SyncDemo.Api/SyncDemo.Api.csproj", "src/SyncDemo.Api/"]
COPY ["src/SyncDemo.Shared/SyncDemo.Shared.csproj", "src/SyncDemo.Shared/"]
RUN dotnet restore "src/SyncDemo.Api/SyncDemo.Api.csproj"
COPY . .
WORKDIR "/src/src/SyncDemo.Api"
RUN dotnet build "SyncDemo.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SyncDemo.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SyncDemo.Api.dll"]
