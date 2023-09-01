#!/bin/bash
set -e

. "$(dirname "$0")/constants.sh"

for rid in "${rids[@]}"; do
    output_dir="./Release/$rid/"
    mkdir -p "$output_dir"

    dotnet publish Lib9c/Lib9c.csproj \
        -c Release \
        -r $rid \
        -o $output_dir \
        --self-contained \
        --version-suffix "$(git -C lib9c rev-parse HEAD)"

    bin_name=lib9c
    pushd "$output_dir"

    if [[ "$rid" = win-* ]]; then
        zip -r9 "../${bin_name%.exe}-$rid.zip" ./*
    else
        tar cvfJ "../$bin_name-$rid.tar.xz" ./*
    fi
    popd
    rm -rf "$output_dir"
done
