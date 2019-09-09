#!/bin/bash
set -e

if [[ -f Unity_v2019.x.ulf ]]; then
  # for local debugging
  cp Unity_v2019.x.ulf /tmp/Unity_v2019.x.ulf
elif [[ "$ULF" = "" ]]; then
  echo "The ULF environment variable is missing." > /dev/stderr
  exit 1
else
  echo -n "$ULF" | base64 -d > /tmp/Unity_v2019.x.ulf
fi

title() {
  printf '=%.0s' {0..79}
  echo
  echo "$1"
  printf '=%.0s' {0..79}
  echo
}

title "Unity license"
/opt/Unity/Editor/Unity \
  -quit \
  -batchmode \
  -nographics \
  -logFile \
  -manualLicenseFile /tmp/Unity_v2019.x.ulf || true

title "Unit tests (Editor)"
mkdir -p /tmp/test/
/opt/Unity/Editor/Unity \
  -batchmode \
  -nographics \
  -logFile \
  -projectPath /src/ \
  -runTests \
  -testResults /tmp/test/nunit.xml \
  -testPlatform playmode \
  || true

cat /tmp/test/nunit.xml
xmllint \
  --xpath 'string(/test-run/@result)' \
  /tmp/test/nunit.xml \
  > /tmp/test/result.txt
[[ -f /tmp/test/result.txt ]]

wget -O /tmp/nunit3-junit.xslt \
  https://github.com/nunit/nunit-transforms/raw/master/nunit3-junit/nunit3-junit.xslt
xsltproc \
  -o /tmp/test/junit.xml \
  /tmp/nunit3-junit.xslt \
  /tmp/test/nunit.xml
cat /tmp/test/nunit.xml

title "Build binary"
/opt/Unity/Editor/Unity \
  -quit \
  -batchmode \
  -nographics \
  -logFile \
  -projectPath /src/ \
  -executeMethod "Editor.Builder.BuildLinuxHeadless"
