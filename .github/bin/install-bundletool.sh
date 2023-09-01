#!/bin/bash

REPO="google/bundletool"
API_URL="https://api.github.com/repos/$REPO/releases/latest"

OUTPUT_DIR="${1:-.}"

DOWNLOAD_URL=$(curl -s $API_URL \
    | jq -r '.assets[] | select(.name | contains("bundletool-all")) | .browser_download_url')

FILENAME=$(basename $DOWNLOAD_URL)

if [ ! -z "$DOWNLOAD_URL" ]; then
    echo "Downloading $DOWNLOAD_URL..."
    curl -L $DOWNLOAD_URL -o "$OUTPUT_DIR/$FILENAME"
    echo "Download completed to $OUTPUT_DIR/$FILENAME!"
else
    echo "Error: Could not find a suitable asset to download."
fi
