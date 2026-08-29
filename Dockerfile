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

# tzdata — TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo") (HU-07a item 1,
# usado no cálculo do prazo de confirmação) lança exceção sem isso: a imagem
# runtime, ao contrário da sdk, não vem com o banco de fusos IANA instalado.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/src/ConviteInterativo.Web ./
COPY themes/ /app/themes/
COPY assets/ /app/assets/

# data/ é montada como volume persistente em produção (ADR 0002) — o arquivo SQLite
# não pode ficar no filesystem efêmero do container.
RUN mkdir -p /app/data

# ADR 0015 decisão 3: roda como o usuário non-root já embutido na imagem
# mcr.microsoft.com/dotnet/aspnet (uid 1654, "app"), em vez de root. O chown
# garante que esse usuário consiga criar/escrever db/convite.db em runtime
# (Directory.CreateDirectory + Migrate no Program.cs) tanto no path relativo
# padrão (db/) quanto num volume montado depois em /app/data.
RUN chown -R app:app /app
USER app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ConviteInterativo.Web.dll"]
