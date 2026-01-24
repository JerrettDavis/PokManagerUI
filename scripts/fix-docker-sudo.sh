#!/bin/bash
#
# Script to configure passwordless sudo for Docker commands for pokuser
# This allows POK-manager.sh to run Docker commands during restart/update without prompting for password
#
# Usage: Run this script with sudo privileges
#   sudo bash fix-docker-sudo.sh
#

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}======================================${NC}"
echo -e "${GREEN}Docker Sudo Configuration Script${NC}"
echo -e "${GREEN}======================================${NC}"
echo ""

# Check if running as root or with sudo
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}ERROR: This script must be run with sudo privileges${NC}"
    echo "Usage: sudo bash fix-docker-sudo.sh"
    exit 1
fi

# Configuration
TARGET_USER="pokuser"
SUDOERS_FILE="/etc/sudoers.d/pokuser-docker"

echo -e "${YELLOW}Step 1: Checking if user exists...${NC}"
if ! id "$TARGET_USER" &>/dev/null; then
    echo -e "${RED}ERROR: User '$TARGET_USER' does not exist${NC}"
    exit 1
fi
echo -e "${GREEN}✓ User '$TARGET_USER' exists${NC}"
echo ""

echo -e "${YELLOW}Step 2: Checking if user is in docker group...${NC}"
if groups "$TARGET_USER" | grep -q '\bdocker\b'; then
    echo -e "${GREEN}✓ User '$TARGET_USER' is in docker group${NC}"
else
    echo -e "${YELLOW}→ Adding '$TARGET_USER' to docker group...${NC}"
    usermod -aG docker "$TARGET_USER"
    echo -e "${GREEN}✓ Added '$TARGET_USER' to docker group${NC}"
fi
echo ""

echo -e "${YELLOW}Step 3: Creating sudoers file for passwordless Docker access...${NC}"

# Create the sudoers file
cat > "$SUDOERS_FILE" <<'EOF'
# Allow pokuser to run Docker commands without password
# This is required for POK-manager.sh restart/update operations
# Created by fix-docker-sudo.sh

# Docker binary
pokuser ALL=(ALL) NOPASSWD: /usr/bin/docker

# Docker Compose (both versions)
pokuser ALL=(ALL) NOPASSWD: /usr/bin/docker compose
pokuser ALL=(ALL) NOPASSWD: /usr/bin/docker-compose

# Docker system commands that might be needed
pokuser ALL=(ALL) NOPASSWD: /usr/bin/docker system prune
pokuser ALL=(ALL) NOPASSWD: /usr/bin/docker volume prune
pokuser ALL=(ALL) NOPASSWD: /usr/bin/docker network prune
pokuser ALL=(ALL) NOPASSWD: /usr/bin/docker image prune
EOF

echo -e "${GREEN}✓ Created sudoers file at $SUDOERS_FILE${NC}"
echo ""

echo -e "${YELLOW}Step 4: Setting correct permissions (0440)...${NC}"
chmod 0440 "$SUDOERS_FILE"
chown root:root "$SUDOERS_FILE"
echo -e "${GREEN}✓ Permissions set correctly${NC}"
echo ""

echo -e "${YELLOW}Step 5: Validating sudoers syntax...${NC}"
if visudo -c -f "$SUDOERS_FILE" &>/dev/null; then
    echo -e "${GREEN}✓ Sudoers file syntax is valid${NC}"
else
    echo -e "${RED}ERROR: Sudoers file has syntax errors!${NC}"
    echo "Removing invalid file for safety..."
    rm -f "$SUDOERS_FILE"
    exit 1
fi
echo ""

echo -e "${GREEN}======================================${NC}"
echo -e "${GREEN}Configuration Complete!${NC}"
echo -e "${GREEN}======================================${NC}"
echo ""
echo "Changes applied:"
echo "  ✓ User '$TARGET_USER' is in docker group"
echo "  ✓ Sudoers file created: $SUDOERS_FILE"
echo "  ✓ Docker commands can now run without password prompt"
echo ""
echo -e "${YELLOW}IMPORTANT: If '$TARGET_USER' is currently logged in, they need to log out and back in${NC}"
echo -e "${YELLOW}           for the docker group membership to take effect.${NC}"
echo ""
echo "You can verify the configuration by running:"
echo "  su - $TARGET_USER -c 'sudo docker ps'"
echo ""
echo "This should NOT prompt for a password."
echo ""
