#!/bin/bash

SHARED_DLL_PATH=/data/shared_dlls
UNITY_MANAGED_PATH=/app/nekoyume_Data/Managed

# replace dlls with uploaded dlls if exists.
if [ -d $SHARED_DLL_PATH ]; then
    /bin/cp $SHARED_DLL_PATH/*.dll $UNITY_MANAGED_PATH
fi

# entrypoint.
/app/nekoyume $@
