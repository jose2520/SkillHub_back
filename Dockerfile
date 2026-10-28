# ==============================================================================
# SkillHub Multi-Stage Dockerfile (.NET 10 Web API)
# ==============================================================================

# Stage 1: Build and Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project definitions for cached layer restoration
COPY SkillHub.sln ./
COPY src/SkillHub.Domain/SkillHub.Domain.csproj src/SkillHub.Domain/
COPY src/SkillHub.Application/SkillHub.Application.csproj src/SkillHub.Application/
COPY src/SkillHub.Infrastructure/SkillHub.Infrastructure.csproj src/SkillHub.Infrastructure/
COPY src/SkillHub.API/SkillHub.API.csproj src/SkillHub.API/
COPY tests/SkillHub.Tests/SkillHub.Tests.csproj tests/SkillHub.Tests/

# Restore NuGet dependencies
RUN dotnet restore SkillHub.sln

# Copy the entire workspace and build the API
COPY . .
WORKDIR /src/src/SkillHub.API
RUN dotnet publish SkillHub.API.csproj -c Release -o /app/publish --no-restore

# Stage 2: Production Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Ensure proper globalization and culture support
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_URLS=http://+:5142
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5142

# Copy compiled artifacts from build stage
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SkillHub.API.dll"]
