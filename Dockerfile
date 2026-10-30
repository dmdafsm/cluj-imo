# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached across code changes.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ClujImo.slnx ./
COPY src/ClujImo.Domain/ClujImo.Domain.csproj src/ClujImo.Domain/
COPY src/ClujImo.Infrastructure/ClujImo.Infrastructure.csproj src/ClujImo.Infrastructure/
COPY src/ClujImo.Web/ClujImo.Web.csproj src/ClujImo.Web/
COPY tests/ClujImo.Domain.Tests/ClujImo.Domain.Tests.csproj tests/ClujImo.Domain.Tests/
COPY tests/ClujImo.Infrastructure.Tests/ClujImo.Infrastructure.Tests.csproj tests/ClujImo.Infrastructure.Tests/
RUN dotnet restore ClujImo.slnx

COPY . .
RUN dotnet build ClujImo.slnx -c Release --no-restore \
 && dotnet test ClujImo.slnx -c Release --no-build \
 && dotnet publish src/ClujImo.Web -c Release --no-build -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__Default="Data Source=/data/cluj-imo.db"
RUN mkdir /data && chown "$APP_UID" /data
COPY --from=build /app .
USER $APP_UID
VOLUME /data
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s \
  CMD ["bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET /health HTTP/1.0\\r\\n\\r\\n' >&3 && grep -q Healthy <&3"]
ENTRYPOINT ["dotnet", "ClujImo.Web.dll"]
