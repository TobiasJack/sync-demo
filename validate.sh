#!/bin/bash

echo "========================================="
echo "SyncDemo Project Validation Script"
echo "========================================="
echo ""

# Check for .NET SDK
echo "1. Checking .NET SDK..."
if command -v dotnet &> /dev/null; then
    dotnet_version=$(dotnet --version)
    echo "✓ .NET SDK found: $dotnet_version"
else
    echo "✗ .NET SDK not found. Please install .NET 8 SDK."
    exit 1
fi

# Check for Docker
echo ""
echo "2. Checking Docker..."
if command -v docker &> /dev/null; then
    docker_version=$(docker --version)
    echo "✓ Docker found: $docker_version"
else
    echo "✗ Docker not found. Please install Docker Desktop."
    exit 1
fi

# Build solution
echo ""
echo "3. Building solution..."
cd "$(dirname "$0")"
if dotnet build SyncDemo.slnx --verbosity quiet; then
    echo "✓ Solution built successfully"
else
    echo "✗ Solution build failed"
    exit 1
fi

# Check project structure
echo ""
echo "4. Checking project structure..."
required_files=(
    "src/SyncDemo.Api/SyncDemo.Api.csproj"
    "src/SyncDemo.Shared/SyncDemo.Shared.csproj"
    "src/SyncDemo.MauiApp/SyncDemo.MauiApp.csproj"
    "docker-compose.yml"
    "Dockerfile"
    "scripts/init-oracle.sql"
)

all_files_exist=true
for file in "${required_files[@]}"; do
    if [ -f "$file" ]; then
        echo "✓ Found: $file"
    else
        echo "✗ Missing: $file"
        all_files_exist=false
    fi
done

if [ "$all_files_exist" = false ]; then
    echo "✗ Some required files are missing"
    exit 1
fi

echo ""
echo "========================================="
echo "✓ All validation checks passed!"
echo "========================================="
echo ""
echo "Next steps:"
echo "1. Start infrastructure: docker-compose up -d"
echo "2. Run API: cd src/SyncDemo.Api && dotnet run"
echo "3. Build MAUI app: cd src/SyncDemo.MauiApp && dotnet build"
echo ""
