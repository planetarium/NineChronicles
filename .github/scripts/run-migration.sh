#!/usr/bin/env bash

echo "Build Lib9c.Tools"
dotnet build .Lib9c.Tools
echo "Run Lib9c.Tools.Store"
dotnet run \
    --project .Lib9c.Tools \
    store \
    -o _benchmarks_snapshot \
    -d _benchmarks_snapshot_migrate \
    -t

