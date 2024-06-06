#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GameManagement/JokersJunction.GameManagement.csproj", "GameManagement/"]
COPY ["Common/JokersJunction.Common.csproj", "Common/"]
COPY ["Shared/JokersJunction.Shared.csproj", "Shared/"]
COPY ["Server/JokersJunction.Server.csproj", "Server/"]
COPY ["Authentication/JokersJunction.Authentication.csproj", "Authentication/"]
COPY ["Bank/JokersJunction.Bank.csproj", "Bank/"]
COPY ["Client/JokersJunction.Client.csproj", "Client/"]
RUN dotnet restore "./GameManagement/JokersJunction.GameManagement.csproj"
COPY . .
WORKDIR "/src/GameManagement"
RUN dotnet build "./JokersJunction.GameManagement.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./JokersJunction.GameManagement.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JokersJunction.GameManagement.dll"]