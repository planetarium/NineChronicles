#!/bin/bash

if [ $# -ne 1 ]; then
    echo "$0 <runtime>"
    echo "- runtime: runtime identifier (e.g., linux-x64, osx-x64, win-x64)"
    exit
fi

rid="$1"

# Download Qt Runtime.
if [ ! -d "qt-runtimes/$rid" ]; then
    echo "Install Qt Runtime for $rid ..."
    runtime_tar_file="qt-5.12.2-ad0689c-$rid-runtime.tar.gz"
    wget "https://github.com/qmlnet/qt-runtimes/releases/download/releases/$runtime_tar_file" -O "/tmp/$runtime_tar_file"
    mkdir -p "qt-runtimes/$rid"
    pushd "qt-runtimes/$rid"
        echo "/tmp/$runtime_tar_file"
        tar -xvzf "/tmp/$runtime_tar_file"
    popd
    echo "It might be finished."
fi

# Place Qt Runtime.
cp -r "qt-runtimes/$rid/" "../out/$rid/qt-runtime"
