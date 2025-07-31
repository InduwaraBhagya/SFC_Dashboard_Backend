#!/bin/bash

echo "Setting up environment files..."

# Function to copy with confirmation
copy_with_confirmation() {
    local source="$1"
    local target="$2"
    
    if [ -f "$target" ]; then
        echo "Warning: $target already exists"
        read -p "Do you want to overwrite it? (y/n): " overwrite
        if [ "$overwrite" = "y" ] || [ "$overwrite" = "Y" ]; then
            cp "$source" "$target"
            echo "Created $target from example"
        else
            echo "Skipped $target"
        fi
    else
        cp "$source" "$target"
        echo "Created $target from example"
    fi
}

# Copy environment files
copy_with_confirmation "SFCDashboard/.env.example" "SFCDashboard/.env"
copy_with_confirmation "SFCDashboard.Api/.env.example" "SFCDashboard.Api/.env"

echo ""
echo "Environment files setup complete!"
echo ""
echo "IMPORTANT: Please edit the .env files and replace the placeholder values with your actual configuration:"
echo "  1. Azure AD credentials (Tenant ID, Client ID, Client Secret)"
echo "  2. Database connection string"
echo "  3. API URLs if different from defaults"
echo ""
echo "See ENVIRONMENT_CONFIGURATION.md for detailed instructions."
