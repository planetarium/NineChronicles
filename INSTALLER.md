# 의존성

- [Unity Hub]
- [.NETCore 3.1]
- [Inno Setup]

[Unity Hub]: https://unity3d.com/get-unity/download
[.NETCore 3.1]: https://dotnet.microsoft.com/download/dotnet-core/3.1
[Inno Setup]: https://jrsoftware.org/isinfo.php


# 만드는 법

1. [게임 빌드 방법](./README.md)을 참고하여 윈도우 빌드를 만듭니다.
2. [론처 빌드 방법](./NineChronicles.Launcher/README.md)을 참고하여 론처 빌드를 만듭니다.
3. 다음 커맨드를 실행합니다.
    ```pwsh
    > iscc .\tools\installer\installer.iss
    ```
