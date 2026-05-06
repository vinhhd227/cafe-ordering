FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Api.Web/Api.Web.csproj src/Api.Web/
COPY src/Api.Core/Api.Core.csproj src/Api.Core/
COPY src/Api.UseCases/Api.UseCases.csproj src/Api.UseCases/
COPY src/Api.Infrastructure/Api.Infrastructure.csproj src/Api.Infrastructure/
RUN dotnet restore src/Api.Web/Api.Web.csproj

COPY . .
RUN dotnet publish src/Api.Web/Api.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.Web.dll"]
