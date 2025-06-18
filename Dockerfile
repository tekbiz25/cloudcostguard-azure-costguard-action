FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# copy runner
COPY action/src/AzCostguard.Runner/ ./runner/
RUN dotnet publish runner -c Release -o /app/runner
RUN echo "=== Build stage - Contents of /app/runner ===" && ls -la /app/runner/
# build ACE
COPY action/src/third_party/ace /src/ace
RUN dotnet publish /src/ace/ace/azure-cost-estimator.csproj -c Release -o /app/ace

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install Bicep CLI
RUN apt-get update && apt-get install -y curl && \
    curl -Lo bicep https://github.com/Azure/bicep/releases/latest/download/bicep-linux-x64 && \
    chmod +x ./bicep && \
    mv ./bicep /usr/local/bin/bicep && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/runner/ ./
RUN echo "=== Final stage - Contents of /app ===" && ls -la /app/
RUN echo "=== Checking executable permissions ===" && ls -la ./AzCostguard.Runner
RUN echo "=== Testing app startup ===" && ./AzCostguard.Runner --help || echo "App test failed but continuing..."
COPY --from=build /app/ace/ ./ace/
ENV PATH="$PATH:/app/ace"
RUN echo "=== Final check before entrypoint ===" && pwd && ls -la && echo "=== Checking if /app/AzCostguard.Runner exists ===" && ls -la /app/AzCostguard.Runner
ENTRYPOINT ["/app/AzCostguard.Runner"] 