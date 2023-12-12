from typing import List, Literal, Optional, Tuple

Env = Literal["test", "production"]
Network = Literal["main", "internal", "preview", "test"]

# repo title, tag, commit sha
RepoInfos = List[Tuple[str, Optional[str], str]]

Platforms = Literal["Windows", "macOS", "Linux"]
