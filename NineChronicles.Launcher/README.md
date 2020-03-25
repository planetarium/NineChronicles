# Launcher

A system tray application to sync chain with network in background.

## Dependencies

- [.NETCore 3.1]

[.NETCore 3.1]: https://dotnet.microsoft.com/download/dotnet-core/3.1

## Build

```/bin/bash
# dotnet publish -r <rid> --self-contained
# rid = osx-x64 | win-x64
$ dotnet publish -r osx-x64 --self-contained  # out/osx-x64/Launcher.app
$ dotnet publish -r win-x64 --self-contained  # out/win-x64/Launcher.exe
```

### Trouble shooting

#### Unhandled exception. System.Exception: The directory doesn't exist.

```
Unhandled exception. System.Exception: The directory doesn't exist.
   at Qml.Net.Runtimes.RuntimeManager.ConfigureRuntimeDirectory(String directory)
   at Launcher.Program.Main(String[] args) in /Users/moreal/github/planetarium/nekoyume-unity/tools/launcher/Program.cs:line 21
```

*MSBuild Target*으로 전처리 과정을 이전 하면서 빌드를 할 때 제대로 qt runtime이 복사되어 들어가지 않는 문제가 있습니다.
차후 해결해야 할 문제지만 일단은 실행했던 명령어(`dotnet run`, `dotnet publish`)를 한 번 더 실행하여 정상적으로 빌드를 할 수 있습니다.

## Set up launcher

There is [*launcher.json*][launcher-json] like below.

- `storePath`: Path of store.
- `storeType`: Type of store. (`rocksdb` | `default`)
- `appProtoocolVersionToken`: `AppProtocolVersion` derived from `appProtocolVersionToken`, used in `Swarm<T>` type constructor.
- `keyStorePath`: Path of keystore. If `noMiner` is true, it is not required.
- `iceServer`: URL of ice server.
- `seed`: information of the peer having role like *seed* or *bootstrap node*.
- `noMiner`: A flag for mode to mine or not.
- `minimumDifficulty`: Minimum mining difficulty agreed in the network.
- `genesisBlockPath`: URL of the genesis block.
- `gameBinaryPath`: Path of a directory having executable game binary. If it is empty, it will be set value in default.
   Windows: %LOCALAPPDATA%\planetarium\
   Linux/macOS: $HOME/.local/share/planetarium/


[launcher-json]: ./resources/launcher.json
