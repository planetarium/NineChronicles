#!/bin/bash
set -e

source "$(dirname $0)/_common.sh"

title "Unity license"
install_license

title "Unit tests (Editor)"
/opt/unity/Editor/Unity \
  -batchmode \
  -nographics \
  -logFile \
  -projectPath "$(dirname $0)/../nekoyume" \
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
