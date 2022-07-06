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

if [ "$build_target" == "macOS" ]; then
  build_target="MacOS"
fi

if [ "$build_target" == "Linux" ]; then
  build_target="Linux"
fi

# shellcheck disable=SC1090
source "$(dirname "$0")/_common.sh"

title "Unity license"
install_license

title "Build binary"
/opt/unity/Editor/Unity \
  -quit \
  -batchmode \
  -nographics \
  -logFile \
  -username "$UNITY_EMAIL" \
  -password "$UNITY_PASSWORD" \
  -serial "$UNITY_SERIAL" \
  -projectPath nekoyume \
  -executeMethod "Editor.Builder.Build""$build_target"
