# Launcher

A system tray application to sync chain with network in background.

## Dependencies

- [.NETCore 3.1]

[.NETCore 3.1]: https://dotnet.microsoft.com/download/dotnet-core/3.1

## Build

```/bin/bash
# dotnet publish -r <rid> -p:PublishSingleFile=true
# rid = osx-x64 | win-x64
$ dotnet publish -r osx-x64 -p:PublishSingleFile=true  # out/osx-x64/Nine Chronicles.app
$ dotnet publish -r win-x64 -p:PublishSingleFile=true  # out/win-x64/Nine Chronicles.exe
```


## Set up launcher

There is [*launcher.json*][launcher-json] like below.

- `storePath`: Path of store.
- `storeType`: Type of store. (`rocksdb` | `default`)
- `appProtoocolVersionToken`: `AppProtocolVersion` derived from `appProtocolVersionToken`, used in `Swarm<T>` type constructor.
- `keyStorePath`: Path of keystore. If `noMiner` is true, it is not required.
- `iceServer`: URL of ice server.
- `seed`: information of the peer having role like *seed* or *bootstrap node*.
- `noMiner`: A flag for mode to mine or not.
- `noTrustedStateValidators`: A flag for whether to trust the status of specified peers.
- `genesisBlockPath`: URL of the genesis block.
- `gameBinaryPath`: Path of a directory having executable game binary. If it is empty, it will be set value in default.
   Windows: %LOCALAPPDATA%\planetarium\
   Linux/macOS: $HOME/.local/share/planetarium/


[launcher-json]: ./resources/launcher.json


## 배포 디렉토리 구조

### MacOS

```text
# 배포 시
/
├── Launcher.Updater
└── launcher.json

# Launcher.Updater가 구성하는 디렉터리 구조
/
├── Launcher.Updater
└── launcher.json
├── Launcher.app/
└── Nine Chronicles.app/
```


### Windows

```text
# 배포 시
/
├── Launcher.Updater.exe
└── launcher.json

# Launcher.Updater가 구성하는 디렉터리 구조
/
├── Launcher.Updater.exe
└── launcher.json
├── Launcher.exe
├── Nine Chronicles.exe
├── Nine Chronicles_Data/
├──── ...
├── MonoBleedingEdge/
├──── ...
└── ...
```
