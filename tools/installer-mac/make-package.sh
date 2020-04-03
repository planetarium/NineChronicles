#!/bin/bash

set -e

SCRIPT_DIR="$(dirname "${BASH_SOURCE[0]}")"
cd "$SCRIPT_DIR"
SCRIPT_DIR="$(pwd)"
PAYLOAD_DIR="$SCRIPT_DIR/payload"

pkgbuild \
    --ownership preserve \
    --identifier com.ninechronicles.pkg.NineChroniclesInstaller \
    --version 0.0.1 \
    --scripts Scripts \
    --install-location /Applications \
    --root "$PAYLOAD_DIR" \
    NineChronicles.pkg
