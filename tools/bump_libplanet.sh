#!/bin/bash

set -e

VERSION=$1

if [ "$1" == "" ]; then
    echo "Usage: $0 <Libplanet Version>"
    exit 1
fi

SCRIPT_DIR=$(dirname "${BASH_SOURCE[0]}")
cd "$SCRIPT_DIR"
SCRIPT_DIR=$(pwd)

PROJECTS=("Libplanet" "Libplanet.RocksDBStore")


for project in "${PROJECTS[@]}"; do
    echo "Bump $project to $VERSION"
    echo "Downloading $project package..."

    wget "https://www.nuget.org/api/v2/package/$project/${VERSION}"

    TMPDIR="$VERSION-tmp"
    mkdir -p "$TMPDIR"

    mv "$VERSION" "$TMPDIR/"

    cd "$TMPDIR"
    unzip -o "$VERSION"

    LIB_DIR="lib/netstandard2.0"
    LIBPLANET_FILES=(*.{dll,xml})
    cd "$LIB_DIR"

    for f in $LIBPLANET_FILES; do
        chmod 644 "$f"
    done

    PACKAGE_DIR="$SCRIPT_DIR/../nekoyume/Assets/Packages/"

    for f in $LIBPLANET_FILES; do
        cp "$f" "$PACKAGE_DIR"
    done


    cd "$SCRIPT_DIR"
    rm -rf "$TMPDIR"
done
