# nekotool

## Installation

```/bin/bash
pip install -r requirements.txt
pip install .
```

## commands

```
- misc
    - generate-key
- storage
    - setup
    - upload
    - clean
```

## step guide

1. Setup to decide file_share and storage_account etc..  
   `nekotool storage setup`
2. Upload dlls to shared-dll. Also you can choose in these.
    - from nuget  
      `nekotool storage upload --version 0.6.0`  
      `nekotool storage upload --version 0.6.0-nightly.20190826`
    - from local:  
      `nekotool storage upload --file your.dll`
3. Cleanup dlls if you want.  
   `nekotool storage clean`  
   `nekotool storage clean --remove-directory` <- remove directory, too.
