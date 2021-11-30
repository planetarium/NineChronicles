#!/bin/bash
set -e

commit_hash="$1"
data_dir="$(dirname "$0")/../../nekoyume/Assets/PackageExtensions/Mixpanel/Resources"

echo "planetarium" > "$data_dir/MixpanelClientHost.txt"
echo "$commit_hash" > "$data_dir/MixpanelClientHash.txt"
