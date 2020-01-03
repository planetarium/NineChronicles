Scripts for 9C Data Snapshot Manager
====================================

게임 실행 전에 s3에 배포되어 있는 스냅샷으로 부터 스토어 데이터를 받아옵니다.

store-path를 읽어 Tip의 타임스탬프를 기준으로 일정시간 이상 스냅샷보다 뒤쳐져 잇으면 블록이 많이 쌓였을 것이라 가정하고 s3으로 부터 스냅샷을 불러옵니다.

또한 아래와 같은 디렉토리 구조로 배포된다는 가정하에 실행됩니다.

```text
# windows
/
  Nine Chronicles.exe
  NineChroniclesSnapshot.exe
  run.bat
  < extra files (e.g. MonoBleedingEdge, Nine Chronicles_Data...) >

# macOS
/
  Nine Chronicles.app
  NineChroniclesSnapshot
  run
```


사용법
----

윈도우에서는 `run.bat`, MacOS에서는 `run`을 더블클릭하여 사용합니다.  
만약 스냅샷을 가져오고 싶지 않다면 `Nine Chronicles`을 통해 게임을 직접 실행하고, 스냅샷 가져오기만 하고 싶다면 `NineChroniclesSnapshot`을 실행하면 됩니다.
