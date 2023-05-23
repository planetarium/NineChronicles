#!/bin/bash
# Extract a release note from the given changelog file.
# Note that this script is intended to be run by GitHub Actions.
# shellcheck disable=SC2169
set -e

# shellcheck source=constants.sh
. "$(dirname "$0")/constants.sh"

if [ "$GITHUB_REPOSITORY" = "" ] | [ "$GITHUB_REF" = "" ]; then
    echo "This script is intended to be run by GitHub Actions." >/dev/stderr
    exit 1
elif [ "${GITHUB_REF:0:10}" != "refs/tags/" ]; then
    echo "This script is run for only tag push; being skipped..." >/dev/stderr
    exit 0 # If it exits with non-zero other actions become cancelled
fi

tag="${GITHUB_REF#refs/*/}"

if command -v apk; then
    apk add --no-cache ca-certificates
    update-ca-certificates
fi

# Fill the description on GitHub releases with the release note
github_user="${GITHUB_REPOSITORY%/*}"
github_repo="${GITHUB_REPOSITORY#*/}"

trial=0
while ! "$(dirname "$0")/github-release.sh" info \
    --user "$github_user" \
    --repo "$github_repo" \
    --tag "$tag"; do
    "$(dirname "$0")/github-release.sh" release \
        --user "$github_user" \
        --repo "$github_repo" \
        --tag "$tag" \
        --name "$tag" \
        --description "" || true
    trial=$((trial + 1))
    if [[ "$trial" -gt 5 ]]; then break; fi
done

for rid in "${rids[@]}"; do
    for exec_path in "./Release"/*"-$rid".*; do
        "$(dirname "$0")/github-release.sh" upload \
            --user "$github_user" \
            --repo "$github_repo" \
            --tag "$tag" \
            --name "$(basename "$exec_path")" \
            --file "$exec_path"
    done
done
