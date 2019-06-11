# Nekoyume

### 의존성
 - [Unity Hub]


### 설치 방법

 1. [Unity Hub] 설치
 1. Unity 2019.1.0f2 버전 설치
 1. 저장소 클론
    ```
    $ git clone git@github.com:planetarium/nekoyume-unity.git
    ```
 1. 유니티 실행후 프로젝트 빌드


### 커맨드라인 옵션

 - `--private-key` : 사용할 프라이빗 키를 지정합니다.
 - `--host`        : 사용할 Host 이름을 지정합니다.
 - `--port`        : 사용할 Port를 지정합니다.
 - `--no-miner`    : 마이닝을 사용하지 않습니다.
 - `--peer`        : Peer 를 추가합니다. 추가하려는 Peer가 여럿일 경우 --peer peerA peerB ... 와 같이 추가할 수 있습니다.


### 커맨드라인 빌드

```
$ /UnityPath/Unity -quit -batchmode -projectPath=/path/to/nekoyume/ -executeMethod Editor.Builder.Build[All, MacOS, Windows, Linux, MacOSHeadless, WindowsHeadless, LinuxHeadless]
```

- Example

```
$ /Applications/Unity/Hub/Editor/2019.1.0f2/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath=~/planetarium/nekoyume-unity/nekoyume/ -executeMethod Editor.Builder.BuildAll
```

[Unity Hub]: https://unity3d.com/get-unity/download


### Docker-compose 마이너 테스트

로컬에서 테스트 하는 것을 전제로 Seed 의 개인키와 노드들의 Host가 하드코딩 되어 있습니다.

- `LinuxHeadless` 빌드 후 아래 명령 실행

```bash
cd nekoyume/compose
docker-compose up --build
```
