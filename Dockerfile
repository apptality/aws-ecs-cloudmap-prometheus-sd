# This is subject to change when building in CI/CD pipelines,
# as images are build for linux/amd64, and linux/arm64, and windows/amd64

# Linux (default)
ARG BUILD_IMAGE_BASE=mcr.microsoft.com/dotnet/sdk:8.0-alpine
ARG RUNTIME_IMAGE_BASE=mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine
# Windows (built out of CI/CD)
# ARG BUILD_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0-nanoserver-ltsc2022
# ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/runtime:8.0-nanoserver-ltsc2022

# Buil the app
FROM --platform=$BUILDPLATFORM ${BUILD_IMAGE_BASE} AS build
ARG BUILDPLATFORM
ARG TARGETARCH
ARG PROJECT_NAME=Apptality.CloudMapEcsPrometheusDiscovery
WORKDIR /app

# Copy everything else and build
COPY . ./
RUN dotnet restore -a $TARGETARCH ./src/$PROJECT_NAME/$PROJECT_NAME.csproj
RUN dotnet publish -a $TARGETARCH \
     --no-restore \
     --self-contained true \
     -c Release \
     -o out \
    ./src/$PROJECT_NAME/$PROJECT_NAME.csproj

# Build the runtime image
FROM ${RUNTIME_IMAGE_BASE} AS final

# Install curl for health checks
RUN apk --no-cache add curl

# create a new user and change directory ownership
RUN adduser --disabled-password \
  --home /app \
  --gecos '' dotnetuser && chown -R dotnetuser /app

USER dotnetuser
WORKDIR /app

# Only 2 files are needed to run the app: the binary and the appsettings.json
COPY --from=build /app/out/CloudMapEcsPrometheusDiscovery* .
COPY --from=build /app/out/appsettings.json .

ENTRYPOINT ["./CloudMapEcsPrometheusDiscovery"]

# Usage:
# docker build -t apptality/aws-ecs-cloudmap-prometheus-discovery:latest .
# docker run -e ASPNETCORE_URLS="http://*:9O01" -p 9001:9001 apptality/aws-ecs-cloudmap-prometheus-discovery:latest

# Mac M1 Users:
# docker buildx build --platform linux/arm64 -t apptality/aws-ecs-cloudmap-prometheus-discovery:latest .
# docker run --rm -it --platform linux/arm64 -e ASPNETCORE_URLS="http://*:9O01" -p 9001:9001 apptality/aws-ecs-cloudmap-prometheus-discovery:latest

# Debugging:
# docker run -it --rm --entrypoint /bin/sh --platform linux/arm64 apptality/aws-ecs-cloudmap-prometheus-discovery:latest
# docker run --platform linux/arm64 --rm \
#        -e ASPNETCORE_URLS="http://*:9O01" \
#        -e DiscoveryOptions__EcsClusters="<cluster>" \
#        -e AWS_REGION="us-west-2" \
#        -e AWS_ACCESS_KEY_ID="<key>" \
#        -e AWS_SECRET_ACCESS_KEY="<secret>" \
#        -e AWS_SESSION_TOKEN="<session_token>" \
#        -p 9001:9001 \
#        apptality/aws-ecs-cloudmap-prometheus-discovery:latest