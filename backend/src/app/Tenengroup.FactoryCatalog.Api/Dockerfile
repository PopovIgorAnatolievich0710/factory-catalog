FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

ENV ASPNETCORE_ENVIRONMENT=Production

COPY ["NuGet.config", "."]
COPY ["infrastructure/Tenengroup.Infrastructure.Common/Tenengroup.Infrastructure.Common.csproj", "infrastructure/Tenengroup.Infrastructure.Common/"]
COPY ["app/Tenengroup.FactoryCatalog.Api/Tenengroup.FactoryCatalog.Api.csproj", "app/Tenengroup.FactoryCatalog.Api/"]
COPY ["app/Tenengroup.FactoryCatalog.Business.Abstractions/Tenengroup.FactoryCatalog.Business.Abstractions.csproj", "app/Tenengroup.FactoryCatalog.Business.Abstractions/"]
COPY ["app/Tenengroup.FactoryCatalog.Business/Tenengroup.FactoryCatalog.Business.csproj", "app/Tenengroup.FactoryCatalog.Business/"]
COPY ["app/Tenengroup.FactoryCatalog.Domains/Tenengroup.FactoryCatalog.Domains.csproj", "app/Tenengroup.FactoryCatalog.Domains/"]
COPY ["app/Tenengroup.FactoryCatalog.DataAccess.Abstractions/Tenengroup.FactoryCatalog.DataAccess.Abstractions.csproj", "app/Tenengroup.FactoryCatalog.DataAccess.Abstractions/"]
COPY ["app/Tenengroup.FactoryCatalog.DataAccess/Tenengroup.FactoryCatalog.DataAccess.csproj", "app/Tenengroup.FactoryCatalog.DataAccess/"]
COPY ["app/Tenengroup.FactoryCatalog.CompositionRoot/Tenengroup.FactoryCatalog.CompositionRoot.csproj", "app/Tenengroup.FactoryCatalog.CompositionRoot/"]
COPY ["tests/unit-tests/Tenengroup.FactoryCatalog.Business.UnitTests/Tenengroup.FactoryCatalog.Business.UnitTests.csproj", "tests/unit-tests/Tenengroup.FactoryCatalog.Business.UnitTests/"]

RUN dotnet restore "tests/unit-tests/Tenengroup.FactoryCatalog.Business.UnitTests/Tenengroup.FactoryCatalog.Business.UnitTests.csproj"
RUN dotnet restore "app/Tenengroup.FactoryCatalog.Api/Tenengroup.FactoryCatalog.Api.csproj"

COPY . .

RUN dotnet test "tests/unit-tests/Tenengroup.FactoryCatalog.Business.UnitTests/Tenengroup.FactoryCatalog.Business.UnitTests.csproj"

WORKDIR "/src/app/Tenengroup.FactoryCatalog.Api"
RUN dotnet build "Tenengroup.FactoryCatalog.Api.csproj" -c Release -o /app/build


FROM build AS publish

RUN apt-get update && apt-get install -y curl tar && rm -rf /var/lib/apt/lists/*

RUN TRACER_VERSION=$(curl -s https://api.github.com/repos/DataDog/dd-trace-dotnet/releases/latest | grep tag_name | cut -d '"' -f 4 | cut -c2-) \
    && echo "Installing Datadog Tracer v$TRACER_VERSION" \
    && curl -Lo /tmp/datadog-dotnet-apm.tar.gz "https://github.com/DataDog/dd-trace-dotnet/releases/download/v${TRACER_VERSION}/datadog-dotnet-apm-${TRACER_VERSION}.tar.gz" \
    && mkdir -p /opt/datadog \
    && mkdir -p /var/log/datadog/dotnet \
    && tar -C /opt/datadog -xzf /tmp/datadog-dotnet-apm.tar.gz \
    && rm /tmp/datadog-dotnet-apm.tar.gz

RUN dotnet publish "Tenengroup.FactoryCatalog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

RUN apt-get update && \
    apt-get install -y fontconfig fonts-liberation ca-certificates && \
    fc-cache -f && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=publish /app/publish .
COPY --from=publish /opt/datadog /opt/datadog

RUN groupadd appgroup && \
    useradd -m -g appgroup appuser

ENV Kestrel__Endpoints__Http__Url=http://0.0.0.0:5010

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV DD_DOTNET_TRACER_HOME=/opt/datadog
ENV LD_PRELOAD=/opt/datadog/continuousprofiler/Datadog.Linux.ApiWrapper.x64.so
ENV DD_INTEGRATIONS=/opt/datadog/integrations.json
ENV DD_PROFILING_ENABLED=true
ENV DD_KUBELET_TLS_VERIFY=false
ENV DD_RUNTIME_METRICS_ENABLED=true
ENV DD_LOGS_INJECTION=true
ENV DD_PROFILING_MIN_CORES_THRESHOLD=0.5
ENV DD_LOGS_ENABLED=true
ENV DD_LOGS_DIRECT_SUBMISSION_INTEGRATIONS=Serilog
ENV DD_TRACE_128_BIT_TRACEID_GENERATION_ENABLED=false
ENV DD_SERVICE=factory-catalog
ENV DD_VERSION=1.0.0

LABEL com.datadoghq.tags.service=factory-catalog
LABEL com.datadoghq.tags.version=1.0.0

EXPOSE 5010

USER appuser

ENTRYPOINT []
CMD ["dotnet", "Tenengroup.FactoryCatalog.Api.dll"]
