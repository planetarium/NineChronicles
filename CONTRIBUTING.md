Contributor guide
=================

Note: This document at present is for only code contributors.
We should expand it so that it covers reporting bugs, filing issues,
and writing docs.


Questions & online chat  [![Discord](https://img.shields.io/discord/928926944937013338.svg?color=7289da&logo=discord&logoColor=white)][Discord server]
-----------------------

We have a [Discord server] to discuss Nine Chronicles.  There are some channels
for purposes in the *Nine Chronicles* category:

 -  *#general*: A space for general discussions related to Nine Chronicles or community interactions.
 -  *#9c-unity*: Chat with maintainers and contributors of Nine Chronicles.
    Ask questions to *hack*  Nine Chronicles and to make a patch for it.  People here
    usually speak in Korean, but feel free to speak in English.

[Discord server]: https://planetarium.dev/discord


Prerequisites
-------------
Before contributing to the NineChronicles repository, ensure you have the following:

Unity Version: You need to have [Unity 2021.3.37f1] installed. This is the required version for developing and running NineChronicles.

[Unity 2021.3.37f1]: https://unity.com/kr/releases/editor/whats-new/2022.3.37#notes


Installation
-------------
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

[9c-unity-guide]: https://nine-chronicles.dev/forum-trunk/playing-the-nine-chronicles-local-network-with-the-unity-editor


How to play in the editor
-------------
If you want to run on the editor, please press the run button on 'IntroScene', or press 'Donguri button' at the top to go to that scene and start the game.


Lib9c Submodule
-------------
This repository uses **lib9c** as a submodule, which contains core logic and blockchain elements essential to NineChronicles. To initialize and update the submodule
```bash
git submodule init
git submodule update
```

It is located under `Assets/_Scripts/Lib9c` in the repository.

For contributions related to **lib9c**, refer to the [lib9c repository](https://github.com/planetarium/lib9c) and ensure changes are reflected when updating the submodule.


Command Line Options
-------------

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

Coding Style
-----------------------

### Indentation
- Follow the default settings for indentation.
- Disable alignment of multi-line assignments based on operators.

Example:
```csharp
// Disabled alignment
{
    x = 1;
    yyy = 2;
    zzzz = 3;
}
```

### Naming
- Use default naming conventions.

### Syntax Style

#### Braces
- Always enforce braces for `if`, `for`, `foreach`, `while`, `do-while`, `using`, `lock`, and `fixed` statements.
- Set notifications to **Suggestion** level.

#### Code Body
- Prefer block body over expression body for functions.  
- Recommend expression body for properties, indexers, and events.

```csharp
// Block body
private int Add(int a, int b)
{
    return a + b;
}
```

#### Attributes
- Separate each attribute with its own square brackets.  

### Braces Layout
- Use BSD style (default setting).

### Blank Lines
- Follow the default settings.

### Line Breaks and Wrapping
- Stick to the default settings unless otherwise needed.
- Wrap limit: 300 characters.

#### Arrangement of Attributes
- Place field or property/indexer/event attribute on the same line

### Spaces
- Use default settings except for **Between attribute sections**, which is set to `false`.