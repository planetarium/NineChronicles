# Launcher

A system tray application to sync chain with network in background.

## Build

```/bin/bash
# dotnet publish -r <rid> --self-contained
# rid = osx-x64 | win-x64
$ dotnet publish -r osx-x64 --self-contained  # out/osx-x64/Launcher.app
$ dotnet publish -r win-x64 --self-contained  # out/win-x64/Launcher.exe
```

## Set up launcher

There is [*launcher.json*][launcher-json] like below.

- `storePath`: Path of store.
- `appProtoocolVersion`: `appProtocolVersion` used in `Swarm<T>` type constructor.
- `keyStorePath`: Path of keystore. If `noMiner` is true, it is not required. 
- `passphrase`: Passphrase used to protect keystore. If `noMiner` is true, it is not required. 
- `iceServer`: URL of ice server.
- `seed`: information of the peer having role like *seed* or *bootstrap node*.
- `noMiner`: A flag for mode to mine or not.
- `genesisBlockPath`: URL of the genesis block.
- `gameBinaryPath`: Path of a directory having executable game binary. If it is empty, it will be set value in default.  
   Windows: %LOCALAPPDATA%\planetarium\  
   Linux/macOS: $HOME/.local/share/planetarium/


[launcher-json]: ./resources/launcher.json
