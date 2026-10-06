# syntax=docker/dockerfile:1
# Build context: backend/
#   docker build -t worknexa-api:1.4.0 --build-arg VERSION=1.4.0 backend

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so package downloads are cached until a project file changes.
COPY Worknexa.slnx ./
COPY src/Domain/Worknexa.Domain.csproj src/Domain/
COPY src/Application/Worknexa.Application.csproj src/Application/
COPY src/Infrastructure/Worknexa.Infrastructure.csproj src/Infrastructure/
COPY src/API/Worknexa.Api.csproj src/API/
RUN dotnet restore src/API/Worknexa.Api.csproj

COPY src/ src/
ARG VERSION=0.0.0-local
RUN dotnet publish src/API/Worknexa.Api.csproj -c Release -o /app --no-restore \
    -p:Version=${VERSION} -p:UseAppHost=false

# Migration bundle: a self-contained executable that applies pending migrations, run as a one-off job before each rollout.
# The placeholder connection string only lets the design-time factory build the model; the real one is supplied at run time.
RUN dotnet tool install --global dotnet-ef --version 10.0.12
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN ConnectionStrings__Worknexa="Host=build-only" \
    dotnet ef migrations bundle --project src/Infrastructure --startup-project src/API \
    --configuration Release --output /app/efbundle --force

# Writable mount points owned by the non-root runtime user (uid 1654 in Microsoft images).
RUN mkdir -p /data/attachments /data/keys

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS runtime
WORKDIR /app
COPY --from=build /app ./
COPY --from=build --chown=1654:1654 /data /data

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER 1654

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD ["dotnet", "/app/Worknexa.Api.dll", "--healthcheck", "/api/health"]

ENTRYPOINT ["dotnet", "/app/Worknexa.Api.dll"]
