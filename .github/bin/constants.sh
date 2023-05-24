#!/bin/bash

# https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
rids=(linux-x64 osx-x64 osx-arm64 win-x64)

# Publish a package only if the repository is upstream (planetarium/lib9c)
# and the branch is for releases (main or *-maintenance or 9c-*).
# shellcheck disable=SC2235
if [ "$GITHUB_REPOSITORY" = "planetarium/lib9c" ] && [[ \
    "$GITHUB_REF" = refs/tags/* || \
    "$GITHUB_REF" = refs/heads/main
  ]]; then
  publish_package=true
fi
