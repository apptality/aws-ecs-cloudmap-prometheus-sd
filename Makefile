# Project Setup/Tools
.PHONY: bootstrap
bootstrap:
	dotnet tool restore
	chmod +x .scripts/order-directory-packages.sh

# Build
.PHONY: run
run: build
	@dotnet run \
		--urls "http://localhost:9001/prometheus-targets" \
		--no-build \
		--no-launch-profile \
		--project src/Apptality.CloudMapEcsPrometheusDiscovery/Apptality.CloudMapEcsPrometheusDiscovery.csproj

.PHONY: build
build:
	dotnet build /tl /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary

.PHONY: clean
clean: clean-dotnet-folders

.PHONY: clean-dotnet-folders
clean-dotnet-folders:
	find . -iname "bin" | xargs rm -rf
	find . -iname "obj" | xargs rm -rf

.PHONY: fix-packages
fix-packages:
	@.scripts/order-directory-packages.sh