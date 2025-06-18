FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# copy runner
COPY action/src/AzCostguard.Runner/ ./runner/
RUN dotnet publish runner -c Release -o /app/runner
# build ACE
COPY third_party/ace /src/ace
RUN dotnet publish /src/ace/src/ArmEstimator.Cli -c Release -o /app/ace

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/runner ./
COPY --from=build /app/ace ./ace/
ENV PATH="$PATH:/app/ace"
ENTRYPOINT ["dotnet", "AzCostguard.Runner.dll"] 