#!/bin/bash
set -evx

if [[ "$#" != "6" ]]; then
  {
    echo "error: too few arguments"
    echo "usage: $0 OUT-DIR PLATFORM GAME-DIR LAUNCHER-DIR PRIVATE-KEY TIMESTAMP"
  } > /dev/stderr
  exit 1
fi

if ! command -v realpath > /dev/null; then
  realpath() {
    python2.7 -c 'import os.path, sys; print os.path.abspath(sys.argv[1]),' "$@"
  }
fi

out_dir="$(realpath "${1%/}")"
platform="$(echo "$2" | tr '[:upper:]' '[:lower:]')"
game_binary_dir="$3"
launcher_dir="$4"
private_key="$5"
timestamp="$6"

temp_dir="$(mktemp -d)"

# debug print
{
  echo "out_dir=$out_dir"
  echo "platform=$platform"
  echo "game_binary_dir=$game_binary_dir"
  echo "launcher_dir=$luancher_dir"
  echo "temp_dir=$temp_dir"
} > /dev/stderr

mkdir -p "$out_dir/"

case "$platform" in
  macos)
    archive=tar
    archive_options=cvfz
    archive_filename=macOS.tar.gz
    ;;
  windows)
    archive=zip
    archive_options=-r9
    archive_filename=Windows.zip
    ;;
  *)
    echo "Unsupported platform: $platform" > /dev/stderr
    exit 1
esac

cp -r "$launcher_dir/"* "$temp_dir"
cp -r "$game_binary_dir/"* "$temp_dir"

latest_version=$(
  aws s3 ls s3://9c-test/v \
  | awk '{ if ($2 ~ /^v[0-9][0-9]*\/$/ ) print substr($2, 2, length($2) - 2) }' \
  | sort -nr \
  | head -n 1
)
next_version=$((latest_version + 1))

# 아직 실제로 올라가 있지 않더라도, 이쪽으로 올려야 함. 서명을 하기 위해 미리 URL을 결정해 둠.
url_base="https://download.nine-chronicles.com"
macos_url="$url_base/v$next_version/macOS.tar.gz"
windows_url="$url_base/v$next_version/Windows.zip"

passphrase="$(head -c 40 /dev/urandom | xxd -ps -c 40)"
key_id="$(planet key import --passphrase "$passphrase" "$private_key" \
         | awk '{ print $1 }')"
public_key="$(
  planet key export \
    --passphrase "$passphrase" \
    --public-key "$key_id"
)"
apv=$(
  planet apv sign \
    --passphrase "$passphrase" \
    --extra timestamp="$timestamp" \
    --extra macOSBinaryUrl="$macos_url" \
    --extra WindowsBinaryUrl="$windows_url" \
    "$key_id" \
    "$next_version"
)
planet key remove --passphrase "$passphrase" "$key_id"

if ! command -v sponge > /dev/null; then
  sponge() {
    local tmp
    tmp="$(mktemp)"
    cat > "$tmp"
    mv "$tmp" "$1"
  }
fi

clo_json="$(find "$temp_dir" -type d -name StreamingAssets | head -n 1)/clo.json"
launcher_json="$temp_dir/launcher.json"

cp NineChronicles.Launcher/resources/launcher.json "$launcher_json"
cp "$(dirname "$0")/clo.json.template" "$clo_json"

if command -v strip-json-comments > /dev/null; then
  strip-json-comments "$launcher_json" | sponge "$launcher_json"
  strip-json-comments "$clo_json" | sponge "$clo_json"
fi

# clo.json, launcher.json
(jq \
  --arg apv "$apv" \
  --arg public_key "$public_key" \
  '.appProtocolVersionToken = $apv | .trustedAppProtocolVersionSigners = [$public_key]' \
  < "$launcher_json") \
| sponge "$launcher_json"
(jq \
  --arg apv "$apv" \
  --arg public_key "$public_key" \
  '.appProtocolVersionToken = $apv | .trustedAppProtocolVersionSigners = [$public_key]' \
  < "$clo_json") \
| sponge "$clo_json"

# compress
pushd "$temp_dir/"
  $archive $archive_options "$out_dir/$archive_filename" ./*
popd

# clean up because there is space limit, 14GB.
rm -rf "${temp_dir:?}"
