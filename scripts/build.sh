#!/bin/bash
set -e

source "$(dirname $0)/_common.sh"

title "Unity license"
install_license

title "Build binary"
/opt/Unity/Editor/Unity \
  -quit \
  -batchmode \
  -nographics \
  -logFile \
  -projectPath /src/ \
  -executeMethod "Editor.Builder.BuildLinuxHeadlessDevelopment"
