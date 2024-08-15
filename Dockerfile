# Use the official Microsoft .NET Core SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy everything else and build
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out --self-contained true --runtime linux-musl-x64 /p:PublishTrimmed=true

# Build runtime image using Alpine
FROM alpine:latest
WORKDIR /app
COPY --from=build-env /app/out .
EXPOSE 9001
ENTRYPOINT ["./CloudMapEcsPrometheusDiscovery"]

# Usage:
# docker build -t cloud-map-ecs-prometheus-discovery:latest .
# docker run -p 9001:9001 cloud-map-ecs-prometheus-discovery:latest

# Mac M1 Users:
# docker build --platform linux/x86_64 -t cloud-map-ecs-prometheus-discovery:latest .
# docker run --platform linux/x86_64 -p 9001:9001 cloud-map-ecs-prometheus-discovery:latest