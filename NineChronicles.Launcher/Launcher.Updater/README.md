# Launcher.Updater

## 업데이터 동작 순서

### 1. 업데이터 업데이트

S3에 업로드 되어있는 업데이터의 metadata를 읽어 자신의 [MD5] checksum과 비교하여 상이하다면 해당 바이너리를 받아 교체한 후 재실행합니다.

S3의 올릴때는 아래과 같이 AWS CLI를 사용하여 올리거나 Web 콘솔을 통해 올리는 경우 메타데이터로 [MD5] checksum을 필히 넣어주어야 합니다.

```/bin/bash
# macOS
aws s3 cp \
    'Nine Chronicles Updater' 's3://9c-test/latest/NineChroniclesUpdater' \
    --acl public-read \
    --metadata md5-checksum="$(md5 -q 'Nine Chronicles Updater')"

# Windows (Powershell)
aws s3 cp \
    'Nine Chronicles Updater.exe' 's3://9c-test/latest/NineChroniclesUpdater.exe' \
    --acl public-read \
    --metadata md5-checksum=(Get-FileHash /Users/moreal/.bashrc -Algorithm MD5).Hash)
```

[MD5]: https://en.wikipedia.org/wiki/MD5


### 2-1. 인스톨러 모드 - 인자로 아무것도 주지 않았을 때

정의된 URL로부터 게임 바이너리를 받아 같은 디렉토리에 설치합니다.

- macOS: https://download.nine-chronicles.com/latest/macOS.tar.gz
- Windows: https://download.nine-chronicles.com/latest/Windows.zip

### 2-2. 인스톨러 모드 - 인자가 주어졌을 때

인자로 주어진 URI로 부터 게임 바이너리를 받아 교체합니다.

### 3. 게임 실행

같은 디렉토리에 있는 게임 바이너리를 실행합니다.
