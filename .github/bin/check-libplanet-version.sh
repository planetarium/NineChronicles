#!/bin/bash

root="$(dirname "$0")/../.."
packages="$root/nekoyume/Assets/Packages"
libplanet="$root/nekoyume/Assets/_Scripts/Lib9c/lib9c/.Libplanet"

libplanet_mtime="$(git --git-dir="$libplanet/.git" \
                       log -1 --format=%ad --date=iso)"
libplanet_mtime_s="$(date -d "$libplanet_mtime" +%s)"

echo "Libplanet was updated at $libplanet_mtime." > /dev/stderr

outdated=0
for f in "$packages"/Libplanet*.dll; do
  mtime="$(git log -1 --format=%ad --date=iso -- "$f")"
  mtime_s="$(date -d "$mtime" +%s)"
  basename=$(basename "$f")
  if [[ "$mtime_s" -gt "$libplanet_mtime_s" ]]; then
    echo "$basename seems up-to-date ($mtime)." > /dev/stderr
  else
    echo "$basename seems outdated ($mtime)." > /dev/stderr
    outdated=1
  fi
done

exit $outdated
