Nine Chronicles
===============
![Nine Chronicles Banner][9c-banner]

[![CircleCI][ci-badge]][ci]
[![Discord][Discord-badge]][Discord]
[![Planetarium-Dev Discord Invite](https://img.shields.io/discord/928926944937013338?color=6278DA&label=Planetarium-dev&logo=discord&logoColor=white)](https://discord.gg/RYJDyFRYY7)
[![Discourse posts](https://img.shields.io/discourse/posts?server=https%3A%2F%2Fdevforum.nine-chronicles.com%2F&logo=discourse&label=9c-devforum&color=00D1C2
)](https://devforum.nine-chronicles.com)

[Nine Chronicles][9c] is a fully open-sourced online RPG without servers — like Bitcoin or BitTorrent,
the gamers and miners connect to each other to power a distributed game network.
Set in a vast fantasy world, it is governed by [its players][Discord], and supported by a complex economy
where supply and demand are the greatest currency.

Decentralized infrastructure has created new possibilities for online gaming, where communities
can become the actual owners of an online world. By fully open sourcing the repositories for
Nine Chronicles, players and developers alike can use any part of the game, from the beautiful
bespoke 2D assets to in-game logic and code.

To learn more about the [codebase][9c-source-code-guide] and the [GraphQL API][9c-api-guide],
visit [docs.nine-chronicles.com][9c-docs].

[ci-badge]: https://circleci.com/gh/planetarium/nekoyume-unity.svg?style=svg&circle-token=ca79d4f6281fe60cdde55d0f1c3d97d561106bda
[ci]: https://circleci.com/gh/planetarium/nekoyume-unity
[Discord-badge]: https://img.shields.io/discord/539405872346955788.svg?color=7289da&logo=discord&logoColor=white
[Discord]: https://discord.gg/planetarium
[9c]: https://nine-chronicles.com
[9c-docs]: https://docs.nine-chronicles.com
[9c-api-guide]: https://docs.nine-chronicles.com/api-guide
[9c-source-code-guide]: https://docs.nine-chronicles.com/source-code-guide
[9c-banner]: docs/9c-banner.jpeg


### Dependency
 - [Unity Hub]


### Installation

 1. Install [Unity Hub]
 1. Install Unity 2021.3.37f1 version
 1. Clone repository
    ```
    git clone https://github.com/planetarium/NineChronicles.git
    ```
 1. Navigate to the cloned directory and run the command:
    ```
    git config core.hooksPath hooks
    git submodule update --init --recursive
    ```
 1. Run Unity and build project

To launch Nine Chronicles from the Unity editor, please follow the [step-by-step guide][9c-unity-guide].

[9c-unity-guide]: https://docs.nine-chronicles.com/unity-guide

### Command Line Options

 - `--private-key`       : private key to use.
 - `--keystore-path`     : path to store private key.
 - `--host`              : host name.
 - `--port`              : port name.
 - `--no-miner`          : disable mining.
 - `--peer`              : add peer. Multiple peers can be added with `--peer peerA peerB ... `.
 - `--ice-servers`       : TURN server information used for NAT traversal. Multiple servers can be added with `--ice-servers serverA serverB`.
 - `--genesis-block-path`: path of genesis block. Supports http(s) paths and uses `Assets/StreamingAssets/genesis-block` if not provided.
 - `--storage-path`      : path to store chain data.
 - `--storage-type`      : storage type name. Currently supports `RocksDBStore` (`--storage-type rocksdb`).
 - `--rpc-client`        : starts client mode that does not store chain data.
 - `--rpc-server-host`   : rpc server host name.
 - `--rpc-server-port`   : rpc server port name.
 - `--auto-play`         : automatically generate character and enter battle stage in the background.
 - `--console-sink`      : print logs on console.
 - `--development`       : run in development mode. Shows debugging UI and log level configuration.

#### Using Command Line Options on Unity Editor

To use the above command line options on Unity Editor or on build player, `Assets/StreamingAssets/clo.json` must be created. Below is an example:

```
{
   "privateKey": "",
   "host": "127.0.0.1",
   "port": 5555,
   "noMiner": true,
   "peers": ["02ed49dbe0f2c34d9dff8335d6dd9097f7a3ef17dfb5f048382eebc7f451a50aa1,nekoyume1.koreacentral.cloudapp.azure.com,58598"]
}
```

- `Assets/StreamingAssets/clo.json` is excluded from version control.
  - `Assets/StreamingAssets/clo_nekoalpha_nominer.json` could be provided as a preset. To use this file, change the name to `clo.json`.


### Command Line Build

```
$ /UnityPath/Unity -quit -batchmode -projectPath=/path/to/nekoyume/ -executeMethod Editor.Builder.Build[All, MacOS, Windows, Linux, MacOSHeadless, WindowsHeadless, LinuxHeadless]
```

- Example

```
$ /Applications/Unity/Hub/Editor/2021.3.37f1/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath=~/planetarium/nekoyume-unity/nekoyume/ -executeMethod Editor.Builder.BuildAll
```

### Editor Build

Use the `Build` menu on the Unity Editor.

### Peer Configuration

#### Reading Order

Peer options for network communication is read in the following order:

1. Command Line parameter upon execution (`--peer`)
2. (On Windows) `peers.dat` in `%USERPROFILE%\AppData\LocalLow\Planetarium`
3. `Assets\Resources\Config\peers.txt` inside NineChronicles project.

Since the current project doesn't include option 3, the game will run in a single node if peer configuration in either option 1 or 2 are not provided.

#### Format

Peer list is stored in plain text format and each line includes a node's `publickey,host-name,port,version`.

Ex)

```
   02ed49dbe0f2c34d9dff8335d6dd9097f7a3ef17dfb5f048382eebc7f451a50aa1,nekoyume1.koreacentral.cloudapp.azure.com,58598
   02d05be62f8593721f5abfd28fb83c043ed9d9585f45b652cb67fd6eee3fd3748f,nekoyume2.koreacentral.cloudapp.azure.com,58599
```

- Host name and port must be public.
    - If `--host` is not provided upon execution, the actual host name and port could be different from the original due to the automatic relay communication via STUN/TURN.
      Therefore, nodes that are used as peers on other nodes must provide its `--host` option on execution.
- Public key is a hexadecimal string derived from the `PrivateKey` that is used to create a `Swarm` object.

[Unity Hub]: https://unity3d.com/get-unity/download


### Docker-compose Miner Test

Seed private key and node host are hardcoded for local testing purposes.

- Build `LinuxHeadless` and run the command below:

```bash
cd nekoyume/compose
docker-compose up --build
```

### Auto Play Option

`--auto-play` option can be used to generate character and automate battle stages in the background.
Currently, character's name is generated with the first 8 characters of the node's `Address` and repeats stage 1 battle at the `TxProcessInterval`.

### Console Sink Option

`--console-sink` option can send logs via `UnityDebugSink` instead of `ApplicationInsights`.

### White List

You can use `nekoyume/Assets/AddressableAssets/TableCSV/Account/ActivationSheet.csv` to manage white lists.

```
id,public_key
1,029d256bc6943cd9d18712b1fe1fdd061705d2ffa644a7705b3cf90f408d1ee278
```

If `PublicKeys` are registered in `ActivationSheet.csv`, only transactions that have been signed with the PrivateKeys of those PublicKeys can be mined.

White list feature will not be activated if there are no `PublicKeys` registered in `ActivationSheet.csv`.
