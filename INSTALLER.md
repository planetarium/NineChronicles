# 의존성

- [Unity Hub]
- [.NETCore 3.1]
- [Inno Setup]

[Unity Hub]: https://unity3d.com/get-unity/download
[.NETCore 3.1]: https://dotnet.microsoft.com/download/dotnet-core/3.1
[Inno Setup]: https://jrsoftware.org/isinfo.php


# 만드는 법

## Windows

1. [게임 빌드 방법](./README.md)을 참고하여 윈도우 빌드를 만듭니다.
2. [업데이터 빌드 방법](./tools/Updater/Updater/README.md)을 참고하여 Launcher.Updater 프로젝트를 빌드합니다.
3. 다음 커맨드를 실행합니다.
    ```pwsh
    > iscc .\tools\installer\installer.iss
    ```

## macOS

1. [업데이터 빌드 방법](./tools/Updater/Updater/README.md)을 참고하여 Launcher.Updater 를 만듭니다.
2. Launcher.Updater 와 `NineChronicles.Launcher/resources/launcher.json` 를
   `tools/installer-mac/payload` 에 복사합니다.
3. `tools/installer-mac/make-package.sh`
