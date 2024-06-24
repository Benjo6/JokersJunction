#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install ca-certificates
RUN apt-get update && \
    apt-get install -y ca-certificates && \
    update-ca-certificates

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Authentication/JokersJunction.Authentication.csproj", "Authentication/"]
COPY ["Common/JokersJunction.Common.csproj", "Common/"]
COPY ["Shared/JokersJunction.Shared.csproj", "Shared/"]
RUN dotnet restore "./Authentication/JokersJunction.Authentication.csproj"
COPY . .
WORKDIR "/src/Authentication"
RUN dotnet build "./JokersJunction.Authentication.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./JokersJunction.Authentication.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JokersJunction.Authentication.dll"]
