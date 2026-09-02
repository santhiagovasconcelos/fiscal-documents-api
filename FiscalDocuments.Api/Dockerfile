# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["FiscalDocuments.Api/FiscalDocuments.Api.csproj", "FiscalDocuments.Api/"]
RUN dotnet restore "FiscalDocuments.Api/FiscalDocuments.Api.csproj"

COPY . .

WORKDIR "/src/FiscalDocuments.Api"

RUN dotnet publish "FiscalDocuments.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FiscalDocuments.Api.dll"]