# Fix Docker Sudo Password Prompt

## Problem
When running `./POK-manager.sh -restart -all`, the script prompts for a sudo password during the update check phase:
```
Creating a temporary container for update...
[sudo] password for pokuser:
```

This prevents automated restarts and requires manual intervention.

## Solution
The `fix-docker-sudo.sh` script configures passwordless sudo access for Docker commands for the `pokuser` account.

## Installation

### Step 1: The script is already on the server
The script has been copied to: `/home/pokuser/fix-docker-sudo.sh`

### Step 2: Run the script with sudo
```bash
# SSH to the server
ssh pokuser@10.0.0.216

# Run the fix script (you'll need to enter your password once)
sudo bash fix-docker-sudo.sh
```

### Step 3: Log out and back in
After the script completes, log out and back in for the changes to take full effect:
```bash
exit
ssh pokuser@10.0.0.216
```

### Step 4: Verify the fix
Test that Docker commands work without password:
```bash
sudo docker ps
```

This should NOT prompt for a password.

### Step 5: Test POK-manager restart
```bash
cd ~/asa_server
./POK-manager.sh -restart -all
```

The restart should now complete without prompting for a password during the update check.

## What the Script Does

1. ✓ Verifies `pokuser` exists
2. ✓ Ensures `pokuser` is in the `docker` group
3. ✓ Creates `/etc/sudoers.d/pokuser-docker` with passwordless rules for:
   - `/usr/bin/docker`
   - `/usr/bin/docker compose`
   - `/usr/bin/docker-compose`
   - Docker system maintenance commands
4. ✓ Sets correct permissions (0440)
5. ✓ Validates sudoers syntax

## Security Notes

- Only Docker-related commands are allowed without password
- All other sudo commands still require a password
- The configuration is limited to the `pokuser` account only
- Follows Linux best practices for sudoers configuration

## Troubleshooting

### If the script fails
The script will automatically remove any invalid sudoers files for safety.

### If you still get password prompts
1. Verify the sudoers file exists:
   ```bash
   ls -l /etc/sudoers.d/pokuser-docker
   ```

2. Check its contents:
   ```bash
   sudo cat /etc/sudoers.d/pokuser-docker
   ```

3. Verify pokuser is in docker group:
   ```bash
   groups pokuser
   ```
   Should show: `pokuser sudo docker asareaders`

4. Make sure you logged out and back in after running the script

### Manual removal (if needed)
```bash
sudo rm /etc/sudoers.d/pokuser-docker
```

## Support
If you encounter issues, check the script output for specific error messages.
