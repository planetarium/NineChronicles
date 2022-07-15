#!/bin/bash

title() {
  printf '=%.0s' {0..79}
  echo
  echo "$1"
  printf '=%.0s' {0..79}
  echo
}

install_license() {
    temp_filename="$(mktemp --suffix .ulf)"
    if [[ -f Unity_v2021.x.ulf ]]; then
        # for local debugging
        cp Unity_v2021.x.ulf "$temp_filename"
    elif [[ "$UNITY_LICENSE" = "" ]]; then
        echo "The ULF environment variable is missing." > /dev/stderr
        exit 1
    elif [[ "$UNITY_LICENSE" = \<* ]]; then
        # Bare XML
        echo -n "$UNITY_LICENSE" > "$temp_filename"
    else
        # Base64-encoded XML
        echo -n "$UNITY_LICENSE" | base64 -d > "$temp_filename"
    fi

    /opt/unity/Editor/Unity \
        -quit \
        -batchmode \
        -nographics \
        -logFile \
        -username "$UNITY_EMAIL" \
        -password "$UNITY_PASSWORD" \
        -serial "$UNITY_SERIAL" \
        -manualLicenseFile "$temp_filename" || true
}
