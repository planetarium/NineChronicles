#!/bin/bash
set -e

if [[ "$#" != "1" ]]; then
  {
    echo "error: too few arguments"
    echo "usage: $0 BUILD-TARGET"
  } > /dev/stderr
  exit 1
fi

build_target="$1"

# install dependencies
if command -v apt-get; then
  apt-get update || true && \
    apt-get install -y libxml2-utils xsltproc git && \
    rm -rf /var/lib/apt/lists/*
fi

source "$(dirname $0)/_common.sh"

title "Unity license"
install_license

title "Build binary"
/opt/Unity/Editor/Unity \
  -quit \
  -batchmode \
  -nographics \
  -logFile \
  -projectPath nekoyume \
  -executeMethod "Editor.Builder.Build""$build_target""Development"
