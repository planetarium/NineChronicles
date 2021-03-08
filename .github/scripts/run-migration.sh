#!/usr/bin/env bash

dotnet build Lib9c.Tools
dotnet run \
    --project Lib9c.Tools \
    -s _benchmarks_snapshot
    -d _benchmarks_snapshot_migrate
    -t

