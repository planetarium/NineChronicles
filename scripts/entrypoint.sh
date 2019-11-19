#!/bin/bash

PRIOR_DLLS_PATH=/data/$PRIOR_DLLS
UNITY_MANAGED_PATH=/app/nekoyume_Data/Managed

# replace dlls with uploaded dlls if exists.
if [ -d $PRIOR_DLLS_PATH ]; then
    /bin/cp $PRIOR_DLLS_PATH/*.dll $UNITY_MANAGED_PATH
fi

# entrypoint.
/app/nekoyume "$@"
