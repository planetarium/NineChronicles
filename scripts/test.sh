#!/bin/bash
set -e

source "$(dirname $0)/_common.sh"

title "Unit tests (Editor)"
mkdir -p /tmp/test-result/
/opt/Unity/Editor/Unity \
  -batchmode \
  -nographics \
  -logFile \
  -projectPath /src/ \
  -runTests \
  -testResults /tmp/test-result/nunit.xml \
  -testPlatform playmode \
  || true

cat /tmp/test-result/nunit.xml
xmllint \
  --xpath 'string(/test-run/@result)' \
  /tmp/test-result/nunit.xml \
  > /tmp/test-result/result.txt
[[ -f /tmp/test-result/result.txt ]]

wget -O /tmp/nunit3-junit.xslt \
  https://github.com/nunit/nunit-transforms/raw/master/nunit3-junit/nunit3-junit.xslt
xsltproc \
  -o /tmp/test-result/junit.xml \
  /tmp/nunit3-junit.xslt \
  /tmp/test-result/nunit.xml
cat /tmp/test-result/nunit.xml
