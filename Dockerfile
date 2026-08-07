# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ConviteInterativo.sln .
COPY src/ConviteInterativo.Web/ConviteInterativo.Web.csproj src/ConviteInterativo.Web/
COPY src/ConviteInterativo.Tests/ConviteInterativo.Tests.csproj src/ConviteInterativo.Tests/
RUN dotnet restore ConviteInterativo.sln

COPY src/ src/
RUN dotnet publish src/ConviteInterativo.Web/ConviteInterativo.Web.csproj -c Release -o /app/src/ConviteInterativo.Web --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app/src/ConviteInterativo.Web

COPY --from=build /app/src/ConviteInterativo.Web ./
COPY themes/ /app/themes/
COPY assets/ /app/assets/

# data/ é montada como volume persistente em produção (ADR 0002) — o arquivo SQLite
# não pode ficar no filesystem efêmero do container.
RUN mkdir -p /app/data

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ConviteInterativo.Web.dll"]
