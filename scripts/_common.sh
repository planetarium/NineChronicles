#!/bin/bash

title() {
  printf '=%.0s' {0..79}
  echo
  echo "$1"
  printf '=%.0s' {0..79}
  echo
}

install_license() {
    if [[ -f Unity_v2020.x.ulf ]]; then
        # for local debugging
        cp Unity_v2020.x.ulf /tmp/Unity_v2020.x.ulf
    elif [[ "$ULF" = "" ]]; then
        echo "The ULF environment variable is missing." > /dev/stderr
        exit 1
    else
        echo -n "$ULF" | base64 -d > /tmp/Unity_v2020.x.ulf
    fi

    /opt/unity/Editor/Unity \
        -quit \
        -batchmode \
        -nographics \
        -logFile \
        -manualLicenseFile /tmp/Unity_v2019.x.ulf || true
}
