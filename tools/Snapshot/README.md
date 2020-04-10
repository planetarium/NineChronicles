# Snapshot

로컬에 있는 스토어를 압축하여 제네시스와 팁 블록의 해시에 따라 이름을 정해줍니다. *{genesisHash}-snapshot-{tipHash}.zip*

```
$ dotnet run -- --help
Usage: Snapshot [--output-directory <String>] [--store-path <String>] [--help] [--version]

Snapshot

Options:
  -o, --output-directory <String>     (Default: )
  --store-path <String>               (Default: )
  -h, --help                         Show help message
  --version                          Show version
```
