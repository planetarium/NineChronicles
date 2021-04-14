#!/bin/bash
set -ex

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
  apt-get update || true
  if ! command -v dotnet; then
    apt-get install -y apt-transport-https software-properties-common
    wget -O /tmp/ms.deb \
      https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb
    dpkg -i /tmp/ms.deb
    add-apt-repository universe
    apt-get update
    apt-get install -y dotnet-sdk-3.1
  fi

  apt-get install -y libxml2-utils xsltproc git
  rm -rf /var/lib/apt/lists/*
fi

# shellcheck disable=SC1090
source "$(dirname "$0")/_common.sh"

title "Unity license"
install_license

title "Build binary"
DOTNET_PATH="$(command -v dotnet)" \
/opt/unity/Editor/Unity \
  -quit \
  -batchmode \
  -nographics \
  -logFile \
  -projectPath nekoyume \
  -executeMethod "Editor.Builder.Build""$build_target""Development"
