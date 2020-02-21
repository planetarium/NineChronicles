# Launcher

A system tray application to sync chain with network in background.

## Build

```/bin/bash
# ./scripts/publish.sh <rid>
# rid = osx-x64 | win-x64
$ ./scripts/publish.sh osx-x64
$ ./scripts/publish.sh win-x64
```

## Set up launcher

There is `launcher.json` like below.

```
{
  "storePath": "",
  "appProtocolVersion": 1,
  "keyStorePath": "",
  "passphrase": "",
  "iceServer": "turn://0ed3e48007413e7c2e638f13ddd75ad272c6c507e081bd76a75e4b7adc86c9af:0apejou+ycZFfwtREeXFKdfLj2gCclKzz5ZJ49Cmy6I=@turn.planetarium.dev:3478/",
  "noMiner": true,
  "seed": "027bd36895d68681290e570692ad3736750ceaab37be402442ffb203967f98f7b6,9c-alpha-2020-2-seed.planetarium.dev,31234",
  "genesisBlockPath": "https://9c-test.s3.ap-northeast-2.amazonaws.com/genesis-block-9c-alpha-2020-2",
  "gameBinaryPath": ""
}
```

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
