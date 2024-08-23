#!/bin/bash

# Check if the correct number of arguments are provided
if [ "$#" -lt 1 ] || [ "$#" -gt 2 ]; then
    echo "Usage: $0 <target_ecr_repository_url> [source_dockerhub_image_url] [docker_image_platform: 'linux/amd64' (Default) | 'linux/arm64'] [target_ecr_repository_tag (Default: same as source)]"
    exit 1
fi

# Positional parameters
TARGET_ECR_REPO=$1
SOURCE_DOCKERHUB_IMAGE=${2:-'apptality/aws-ecs-cloudmap-prometheus-discovery:latest'}

# Extract the region from the ECR URL
export AWS_REGION=$(echo $TARGET_ECR_REPO | cut -d'.' -f4)
# Extract tag from the source DockerHub image
DOCKERHUB_TAG=$(echo $SOURCE_DOCKERHUB_IMAGE | cut -d':' -f2)
# Unless tag is overridden, use the same tag for ECR
TARGET_ECR_REPO_TAG=${3:-$DOCKERHUB_TAG}

# By default, use linux/amd64 platform
DOCKER_IMAGE_PLATFORM=${4:-'linux/amd64'}

# Log in to AWS ECR
echo "Authenticating with AWS ECR..."
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $(echo $TARGET_ECR_REPO | cut -d'/' -f1)

echo "** Pushing image for platform: $DOCKER_IMAGE_PLATFORM"
docker pull --platform $DOCKER_IMAGE_PLATFORM $SOURCE_DOCKERHUB_IMAGE
docker tag $SOURCE_DOCKERHUB_IMAGE $TARGET_ECR_REPO:$DOCKERHUB_TAG
docker push $TARGET_ECR_REPO:$DOCKERHUB_TAG

echo "Image successfully pushed to $TARGET_ECR_REPO:$DOCKERHUB_TAG"
