$9cRoot = git rev-parse --show-toplevel
$LibplanetRoot = Join-Path -Path $9cRoot -ChildPath "nekoyume/Assets/_Scripts/Lib9c/lib9c/.Libplanet"
$LibplanetRocksDBStorePath = Join-Path -Path $LibplanetRoot -ChildPath "Libplanet.RocksDBStore/"
$LibplanetRocksDBStoreProj = Join-Path -Path $LibplanetRocksDBStorePath -ChildPath "Libplanet.RocksDBStore.csproj"
$tempDir = [System.IO.Path]::GetTempPath()
$TempOutputPath = Join-Path -Path $tempDir -ChildPath $(New-Guid)
$PackagePath = Join-Path -Path $9cRoot -ChildPath "nekoyume/Assets/Packages/"

git submodule update --recursive
dotnet clean $LibplanetRocksDBStoreProj
dotnet build $LibplanetRocksDBStoreProj -c Release -o $TempOutputPath

Copy-Item -Path $(Join-Path -Path $TempOutputPath -ChildPath "*") -Include "*.dll", "*.xml" -Destination $PackagePath
Remove-Item $TempOutputPath -Recurse
