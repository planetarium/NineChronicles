#!/bin/bash

UNITY_PATH="/app/Nine Chronicles_Data"
PRIOR_DLLS_PATH=/data/$PRIOR_DLLS
UNITY_MANAGED_PATH="$UNITY_PATH/Managed"

# now, the genesis block's name is *genesis-block*.  
# If you renamed, you must do to this, too.
GENESIS_BLOCK_PATH=/data/genesis-block
S3_GENESIS_BLOCK_URL="https://9c-test.s3.ap-northeast-2.amazonaws.com/genesis-block"
UNITY_STREAMING_ASSETS_PATH="$UNITY_PATH/StreamingAssets"

# replace dlls with uploaded dlls if exists.
if [ -d "$PRIOR_DLLS_PATH" ]; then
    /bin/cp "$PRIOR_DLLS_PATH"/*.dll "$UNITY_MANAGED_PATH"
fi

# download genesis block from s3 storage.
wget "$S3_GENESIS_BLOCK_URL" -O "$UNITY_STREAMING_ASSETS_PATH/genesis-block"

# entrypoint.
"/app/Nine Chronicles" "$@"
