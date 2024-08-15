# Use the official Microsoft .NET Core SDK image to build the application
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
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
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build-env /app/out/CloudMapEcsPrometheusDiscovery .
COPY --from=build-env /app/out/appsettings.json .
EXPOSE 9001
ENTRYPOINT ["./CloudMapEcsPrometheusDiscovery"]

# Usage:
# docker build -t cloud-map-ecs-prometheus-discovery:latest .
# docker run -e ASPNETCORE_URLS="http://*:9O01" -p 9001:9001 cloud-map-ecs-prometheus-discovery:latest

# Mac M1 Users:
# docker buildx build --platform linux/amd64 -t cloud-map-ecs-prometheus-discovery:latest .
# docker run --platform linux/amd64 -e ASPNETCORE_URLS="http://*:9O01" -p 9001:9001 cloud-map-ecs-prometheus-discovery:latest