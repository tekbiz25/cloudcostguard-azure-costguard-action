# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY action/src/AzCostguard.Runner/ ./AzCostguard.Runner/
RUN dotnet publish AzCostguard.Runner/AzCostguard.Runner.csproj -c Release -o /app/publish

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# Ensure we're in the correct directory regardless of external working directory overrides
ENTRYPOINT ["sh", "-c", "cd /app && dotnet AzCostguard.Runner.dll \"$@\"", "--"] 