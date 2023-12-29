#!/usr/bin/env bash

PLATFORM=$1

mkdir -p "./tmp"

if [ "$PLATFORM" == "windows" ]; then
  DOWNLOAD_URL="https://www.ssl.com/download/codesigntool-for-windows/"
else
  DOWNLOAD_URL="https://www.ssl.com/download/codesigntool-for-linux-and-macos/"
fi

curl -L "$DOWNLOAD_URL" -o "./tmp/CodeSignTool.zip"
if [ ! -f "./tmp/CodeSignTool.zip" ]; then
  echo "Failed to download CodeSignTool.zip"
  exit 1
fi

unzip -q "./tmp/CodeSignTool.zip" -d "./tmp/codesign"
rm "./tmp/CodeSignTool.zip"

if [ ! -d "./tmp/codesign" ]; then
  echo "Failed to extract CodeSignTool"
  exit 1
fi

if [ "$PLATFORM" != "windows" ]; then
  chmod +x "./tmp/codesign/CodeSignTool.sh"
fi

echo "CodeSignTool installation completed"
