#!/bin/bash
set -e

commit_hash="$1"
target_network="$2"
data_dir="$(dirname "$0")/../../nekoyume/Assets/Resources"

echo "planetarium" > "$data_dir/ClientHost.txt"
echo "$commit_hash" > "$data_dir/ClientHash.txt"
echo "$target_network" > "$data_dir/TargetNetwork.txt"
