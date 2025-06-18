FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# copy runner
COPY action/src/AzCostguard.Runner/ ./runner/
RUN dotnet publish runner -c Release -o /app/runner
# build ACE
COPY action/src/third_party/ace /src/ace
RUN dotnet publish /src/ace/ace/azure-cost-estimator.csproj -c Release -o /app/ace

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Install Bicep CLI
RUN apt-get update && apt-get install -y curl && \
    curl -Lo bicep https://github.com/Azure/bicep/releases/latest/download/bicep-linux-x64 && \
    chmod +x ./bicep && \
    mv ./bicep /usr/local/bin/bicep && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/runner/ ./
COPY --from=build /app/ace/ ./ace/
ENV PATH="$PATH:/app/ace"
ENTRYPOINT ["./AzCostguard.Runner"] 