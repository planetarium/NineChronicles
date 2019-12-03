#!/bin/bash

UNITY_PATH="/app/Nine Chronicles_Data"
PRIOR_DLLS_PATH=/data/$PRIOR_DLLS
UNITY_MANAGED_PATH=$UNITY_PATH/Managed

# now, the genesis block's name is *genesis-block*.  
# If you renamed, you must do to this, too.
GENESIS_BLOCK_PATH=/data/genesis-block
UNITY_STREAMING_ASSETS_PATH=$UNITY_PATH/StreamingAssets

# replace dlls with uploaded dlls if exists.
if [ -d "$PRIOR_DLLS_PATH" ]; then
    /bin/cp "$PRIOR_DLLS_PATH"/*.dll "$UNITY_MANAGED_PATH"
fi

# replace the genesis block if exists.
if [ -f $GENESIS_BLOCK_PATH ]; then
    /bin/cp $GENESIS_BLOCK_PATH $UNITY_STREAMING_ASSETS_PATH
else
    echo "The genesis block is required to run this image but there doesn't exist at $GENESIS_BLOCK_PATH."
fi

# entrypoint.
"/app/Nine Chronicles" "$@"
