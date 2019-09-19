#!/bin/bash
set -e

source "$(dirname $0)/_common.sh"

if [[ -f Unity_v2019.x.ulf ]]; then
  # for local debugging
  cp Unity_v2019.x.ulf /tmp/Unity_v2019.x.ulf
elif [[ "$ULF" = "" ]]; then
  echo "The ULF environment variable is missing." > /dev/stderr
  exit 1
else
  echo -n "$ULF" | base64 -d > /tmp/Unity_v2019.x.ulf
fi

title "Unity license"
/opt/Unity/Editor/Unity \
  -quit \
  -batchmode \
  -nographics \
  -logFile \
  -manualLicenseFile /tmp/Unity_v2019.x.ulf || true

title "Build binary"
/opt/Unity/Editor/Unity \
  -quit \
  -batchmode \
  -nographics \
  -logFile \
  -projectPath /src/ \
  -executeMethod "Editor.Builder.BuildLinuxHeadless"
