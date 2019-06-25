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

### 피어 설정

#### 읽기 순서

통신을 하기 위한 피어 목록은 다음과 같은 순서로 로드합니다.  

1. 실행 시 커맨드라인 인자 (`--peer`)
2. (Windows 기준) `%USERPROFILE%\AppData\LocalLow\Planetarium` 에 있는 `peers.dat`
3. 9C 프로젝트의 `Assets\Resources\Config\peers.txt`

현재 프로젝트에선 기본 피어 설정(3번)이 비어있으므로, 1,2번에 별도의 피어 설정이 되어있지 않다면 싱글 노드로 동작합니다.

#### 형식

피어 목록은 평문(Plain Text) 형식으로 저장되며 한 줄마다 한 노드의 정보를 `공개키,호스트명,포트,버전` 형태로 적습니다.

예시) 

```
   02ed49dbe0f2c34d9dff8335d6dd9097f7a3ef17dfb5f048382eebc7f451a50aa1,nekoyume1.koreacentral.cloudapp.azure.com,58598
   02d05be62f8593721f5abfd28fb83c043ed9d9585f45b652cb67fd6eee3fd3748f,nekoyume2.koreacentral.cloudapp.azure.com,58599
```

- 호스트명과 포트는 외부에서 접속 가능한 것이어야 합니다.
    - 실행시 인자(`—host`)를 지정하지 않은 경우에는 자동으로 STUN/TURN에 의해 릴레이 되므로 실제 호스트명과 포트가 달라질 수 있습니다. 
      즉, 피어 목록에 기술되는 노드는 반드시 `--host`를 통해 호스트명을 지정하여 실행되어야 합니다.
- 공개키는 `Swarm` 객체 생성시 사용한 개인키(`PrivateKey`)로부터 유도된 것을 16진수로 부호화한 것입니다.

[Unity Hub]: https://unity3d.com/get-unity/download


### Docker-compose 마이너 테스트

로컬에서 테스트 하는 것을 전제로 Seed 의 개인키와 노드들의 Host가 하드코딩 되어 있습니다.

- `LinuxHeadless` 빌드 후 아래 명령 실행

```bash
cd nekoyume/compose
docker-compose up --build
```
