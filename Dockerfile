# Backend VeggieLife (ASP.NET Core 8) — multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY VeggieLife.sln ./
COPY src/VeggieLife.API/VeggieLife.API.csproj src/VeggieLife.API/
COPY src/VeggieLife.BLL/VeggieLife.BLL.csproj src/VeggieLife.BLL/
COPY src/VeggieLife.DAL/VeggieLife.DAL.csproj src/VeggieLife.DAL/
COPY tests/VeggieLife.Tests/VeggieLife.Tests.csproj tests/VeggieLife.Tests/
RUN dotnet restore VeggieLife.sln
COPY src ./src
RUN dotnet publish src/VeggieLife.API/VeggieLife.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app .
RUN apt-get update && apt-get install -y --no-install-recommends curl libfontconfig1 libfreetype6 && rm -rf /var/lib/apt/lists/* \
    && useradd --create-home appuser && chown -R appuser /app
USER appuser
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --retries=10 CMD curl -fs http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "VeggieLife.API.dll"]
