# --- Build stage -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY src/Budget.Web/Budget.Web.csproj src/Budget.Web/
RUN dotnet restore src/Budget.Web/Budget.Web.csproj

COPY . .
RUN dotnet publish src/Budget.Web/Budget.Web.csproj \
    -c Release \
    --no-restore \
    -p:InvariantGlobalization=true \
    -o /app/publish

# --- Runtime stage -----------------------------------------------------------
# Alpine = smallest fully supported ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "Budget.Web.dll"]
