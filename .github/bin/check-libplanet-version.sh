#!/bin/bash

ci-out() {
  if [[ "$GITHUB_ACTION" != "" ]]; then
    echo "::set-output name=$1::$2"
  fi
}

root="$(dirname "$0")/../.."
packages="$root/nekoyume/Assets/Packages"
lib9c_props_path="$root/nekoyume/Assets/_Scripts/Lib9c/lib9c/Directory.Build.props"
version_pattern="<LibplanetVersion>(.*)<\/LibplanetVersion>"

if grep -qE "$version_pattern" "$lib9c_props_path"; then
  sed_pattern="s/.*$version_pattern.*/\\1/p"
  submodule_version="$(cat $lib9c_props_path | sed -nE $sed_pattern)"
else
  echo >&2 "The version of Libplanet is not specified in $lib9c_props_path."
  exit 1
fi

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
