#!/bin/bash

ci-out() {
  if [[ "$GITHUB_ACTION" != "" ]]; then
    echo "::set-output name=$1::$2"
  fi
}

root="$(dirname "$0")/../.."
packages="$root/nekoyume/Assets/Packages"
submodule="$root/nekoyume/Assets/_Scripts/Lib9c/lib9c/.Libplanet"

git -C "$submodule" fetch --tags --depth=2147483647
submodule_version="$(git -C "$submodule" describe --tags --abbrev=0)"

echo >&2 "The version of Libplanet submodule vendored by Lib9c is:"
echo "$submodule_version"
ci-out submodule_version "$submodule_version"

unmatched_assemblies=""
outdated=0
for f in "$packages"/Libplanet*.dll; do
  dll_version="$(exiftool -S -ProductVersion "$f" | awk '{ print $2 }')"
  basename=$(basename "$f")
  printf "$basename: $dll_version"
  if [[ "$dll_version" = "$submodule_version" ]]; then
    printf >&2 " (matched)"
  else
    printf >&2 " (UNMATCHED)"
    unmatched_assemblies="$unmatched_assemblies,\"- *$basename*: $dll_version\""
    outdated=1
  fi
  echo
done

ci-out unmatches "[${unmatched_assemblies:1}]"
exit $outdated
