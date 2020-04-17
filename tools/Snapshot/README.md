# Snapshot

로컬에 있는 스토어를 압축하여 제네시스와 팁 블록의 해시에 따라 이름을 정해줍니다. *{genesisHash}-snapshot-{tipHash}.zip*

생성한 스냅샷을 론처 업데이터에서 사용하게 하고 싶다면 *https://9c-test.s3.ap-northeast-2.amazonaws.com/latest/{genesisHash}-snapshot.zip*
경로에 업로드 해야 합니다.

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
