#!/bin/bash

# Script to build and push Azure Cost Guard Docker image

echo "Building Azure Cost Guard Docker image..."

# Set your Docker Hub username
DOCKER_USERNAME="tekbiz25"  # Change this to your Docker Hub username
IMAGE_NAME="azure-cost-guard"
TAG="latest"
FULL_IMAGE_NAME="$DOCKER_USERNAME/$IMAGE_NAME:$TAG"

# Clone the source repository if not already present
if [ ! -d "AzureCostGuard" ]; then
  echo "Cloning Azure Cost Guard repository..."
  git clone https://github.com/your-org/AzureCostGuard.git
  cd AzureCostGuard
  git submodule update --init --recursive
else
  echo "Using existing AzureCostGuard directory..."
  cd AzureCostGuard
fi

# Build the Docker image
echo "Building Docker image: $FULL_IMAGE_NAME"
docker build -t $FULL_IMAGE_NAME .

# Login to Docker Hub
echo "Logging in to Docker Hub..."
echo "Please enter your Docker Hub password:"
docker login -u $DOCKER_USERNAME

# Push the image
echo "Pushing image to Docker Hub..."
docker push $FULL_IMAGE_NAME

echo "✅ Successfully built and pushed: $FULL_IMAGE_NAME"
echo ""
echo "You can now use this image in your pipeline:"
echo "docker run --rm $FULL_IMAGE_NAME" 