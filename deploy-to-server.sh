#!/bin/bash
# Manual deployment script to run on the server
# This script should be run as a user with sudo privileges

set -e

echo "🚀 Deploying PokManager with configuration parsing support..."

# Stop services
echo "Stopping services..."
sudo systemctl stop pokmanager-web pokmanager-api

# Extract deployment package
echo "Extracting deployment package..."
cd /tmp
tar -xzf pokmanager-deploy.tar.gz

# Deploy files
echo "Copying files..."
sudo rm -rf /opt/PokManager/web/* /opt/PokManager/api/*
sudo cp -r web/* /opt/PokManager/web/
sudo cp -r api/* /opt/PokManager/api/

# Set permissions
echo "Setting permissions..."
sudo chown -R pokuser:pokuser /opt/PokManager

# Start services
echo "Starting services..."
sudo systemctl start pokmanager-web pokmanager-api

# Wait and check status
sleep 5
echo ""
echo "Service status:"
sudo systemctl status pokmanager-web --no-pager -l
echo ""
sudo systemctl status pokmanager-api --no-pager -l

echo ""
echo "✅ Deployment complete!"
echo "Check the instances page at http://10.0.0.216:5207/instances"
echo "Map names and configuration should now be visible."
