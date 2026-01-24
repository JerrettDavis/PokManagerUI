#!/bin/bash
# Simple deployment script for PokManager
# Usage: ./deploy.sh

set -e

echo "🚀 Starting PokManager deployment..."
echo ""

# Configuration
PUBLISH_DIR="/var/www/pokmanager"
WEB_PUBLISH="${PUBLISH_DIR}/web"
API_PUBLISH="${PUBLISH_DIR}/api"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

step() {
    echo -e "${YELLOW}▶${NC} $1"
}

success() {
    echo -e "${GREEN}✓${NC} $1"
}

# Step 1: Build
step "Building application..."
dotnet build --configuration Release
success "Build completed"

# Step 2: Run tests
step "Running tests..."
dotnet test --configuration Release --no-build --verbosity minimal
success "Tests passed"

# Step 3: Stop services
step "Stopping services..."
if systemctl is-active --quiet pokmanager-web; then
    sudo systemctl stop pokmanager-web
    echo "  - Stopped pokmanager-web"
fi

if systemctl is-active --quiet pokmanager-api; then
    sudo systemctl stop pokmanager-api
    echo "  - Stopped pokmanager-api"
fi
success "Services stopped"

# Step 4: Publish applications
step "Publishing applications..."
mkdir -p "${WEB_PUBLISH}" "${API_PUBLISH}"

dotnet publish src/Presentation/PokManager.Web/PokManager.Web.csproj \
    -c Release \
    -o "${WEB_PUBLISH}" \
    --no-build

dotnet publish src/Presentation/PokManager.ApiService/PokManager.ApiService.csproj \
    -c Release \
    -o "${API_PUBLISH}" \
    --no-build

success "Applications published"

# Step 5: Set permissions
step "Setting permissions..."
sudo chown -R www-data:www-data "${PUBLISH_DIR}"
sudo chmod -R 755 "${PUBLISH_DIR}"
success "Permissions set"

# Step 6: Start services
step "Starting services..."
sudo systemctl start pokmanager-web
sudo systemctl start pokmanager-api
success "Services started"

# Step 7: Wait for startup
step "Waiting for services to start..."
sleep 10

# Step 8: Verify deployment
step "Verifying deployment..."
if [ -f "scripts/verify-deployment.sh" ]; then
    bash scripts/verify-deployment.sh localhost 5207 5374
else
    echo "⚠️  Verification script not found, skipping automated checks"
    echo "  - Please run manual verification"
fi

echo ""
echo "🎉 Deployment complete!"
echo ""
echo "Check status with:"
echo "  sudo systemctl status pokmanager-web"
echo "  sudo systemctl status pokmanager-api"
