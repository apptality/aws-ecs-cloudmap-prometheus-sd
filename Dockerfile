# Use the official Microsoft .NET Core SDK image to build the application
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env
ARG BUILDPLATFORM
ARG TARGETARCH
ARG PROJECT_NAME=Apptality.CloudMapEcsPrometheusDiscovery
WORKDIR /app

# Copy everything else and build
COPY . ./
RUN dotnet restore -a $TARGETARCH ./src/$PROJECT_NAME/$PROJECT_NAME.csproj
RUN dotnet publish -a $TARGETARCH \
     --self-contained true \
     -c Release \
     -o out \
    ./src/$PROJECT_NAME/$PROJECT_NAME.csproj

# Build runtime image using Alpine
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine
ARG TARGETARCH
ARG BUILDPLATFORM

ENV DOTNET_CLI_TELEMETRY_OPTOUT=0 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    NUGET_XMLDOC_MODE=skip

WORKDIR /app
COPY --from=build-env /app/out/CloudMapEcsPrometheusDiscovery .
COPY --from=build-env /app/out/appsettings.json .
EXPOSE 9001
ENTRYPOINT ["./CloudMapEcsPrometheusDiscovery"]

# Usage:
# docker build -t apptality/cloud-map-ecs-prometheus-discovery:latest .
# docker run -e ASPNETCORE_URLS="http://*:9O01" -p 9001:9001 apptality/cloud-map-ecs-prometheus-discovery:latest

# Mac M1 Users:
# docker buildx build --platform linux/arm64 -t apptality/cloud-map-ecs-prometheus-discovery:latest .
# docker run --rm -it --platform linux/arm64 -e ASPNETCORE_URLS="http://*:9O01" -p 9001:9001 apptality/cloud-map-ecs-prometheus-discovery:latest

# Debugging:
# docker run -it --entrypoint /bin/sh --platform linux/arm64 apptality/cloud-map-ecs-prometheus-discovery:latest
# docker run --platform linux/arm64 --rm \
#        -e ASPNETCORE_URLS="http://*:9O01" \
#        -e DiscoveryOptions__EcsClusters="<cluster>" \
#        -e AWS_REGION="us-west-2" \
#        -e AWS_ACCESS_KEY_ID="<key>" \
#        -e AWS_SECRET_ACCESS_KEY="<secret>" \
#        -e AWS_SESSION_TOKEN="<session_token>" \
#        -p 9001:9001 \
#        apptality/cloud-map-ecs-prometheus-discovery:latest