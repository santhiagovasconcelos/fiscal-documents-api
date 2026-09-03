FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["FiscalDocuments.Worker/FiscalDocuments.Worker.csproj", "FiscalDocuments.Worker/"]
COPY ["FiscalDocuments.Api/FiscalDocuments.Api.csproj", "FiscalDocuments.Api/"]

RUN dotnet restore "FiscalDocuments.Worker/FiscalDocuments.Worker.csproj"

COPY . .

WORKDIR "/src/FiscalDocuments.Worker"

RUN dotnet build "FiscalDocuments.Worker.csproj" \
    -c Release \
    -o /app/build

FROM build AS publish

RUN dotnet publish "FiscalDocuments.Worker.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "FiscalDocuments.Worker.dll"]