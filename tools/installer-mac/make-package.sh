#!/bin/bash

set -e

SCRIPT_DIR="$(dirname "${BASH_SOURCE[0]}")"
cd "$SCRIPT_DIR"
SCRIPT_DIR="$(pwd)"
PAYLOAD_DIR="$SCRIPT_DIR/payload"

PACKAGE_VERSION="0.0.1"
PACKAGE_IDENTIFIER="com.ninechronicles.pkg.NineChroniclesInstaller"
SCRIPTS_DIR="scripts"
INSTALL_LOCATION="/Applications/Nine Chronicles"
TEMP_9C_PACKAGE="9c.pkg"
PRODUCT_PACKAGE="NineChronicles.pkg"

rm -f "$TEMP_9C_PACKAGE"

# Make temp 9c package
pkgbuild \
    --ownership preserve \
    --identifier "$PACKAGE_IDENTIFIER" \
    --version "$PACKAGE_VERSION" \
    --scripts "$SCRIPTS_DIR" \
    --install-location "$INSTALL_LOCATION" \
    --root "$PAYLOAD_DIR" \
    "$TEMP_9C_PACKAGE"

DOTNET_RUNTIME_URL="https://download.visualstudio.microsoft.com/download/pr/b51c2705-f7e1-4a59-b6ba-2a70d9caded3/da2567cee8519d5dc4185cbee8f97498/dotnet-runtime-3.1.4-osx-x64.pkg"
DOTNET_RUNTIME_PACKAGE="dotnet-runtime-3.1.4-osx-x64.pkg"
DOTNET_RUNTIME_PACKAGE_CHECKSUM="d2bf0a1b48f82492eb7ae61f5aa1f06dd5b142357918f7b11e885031eb639c85e0a67abd7b5ebbba4059e46d5ee16a2315a362a83cab9ce1fea15027f1059e21"
# Download dotnet runtime package
if [ ! -f "$DOTNET_RUNTIME_PACKAGE" ]; then
    wget "$DOTNET_RUNTIME_URL"
fi

if [ "$DOTNET_RUNTIME_PACKAGE_CHECKSUM" \
    != "$(sha512sum "$DOTNET_RUNTIME_PACKAGE" | cut -d' ' -f1)" ]; then
    echo "$DOTNET_RUNTIME_PACKAGE checksum is different"
    exit 1
fi

TEMP_PRODUCT_DIR="/tmp/9c-installer"
rm -rf "$TEMP_PRODUCT_DIR"

# Extract dotnet runtime package
pkgutil --expand "$DOTNET_RUNTIME_PACKAGE" "$TEMP_PRODUCT_DIR"

# Extract 9c package
pkgutil --expand "$TEMP_9C_PACKAGE" "$TEMP_PRODUCT_DIR/$PACKAGE_IDENTIFIER.pkg"

# Make product package
cp Distribution "$TEMP_PRODUCT_DIR/Distribution"
pkgutil --flatten "$TEMP_PRODUCT_DIR" "$PRODUCT_PACKAGE"

rm -f "$TEMP_9C_PACKAGE"
rm -rf "$TEMP_PRODUCT_DIR"
