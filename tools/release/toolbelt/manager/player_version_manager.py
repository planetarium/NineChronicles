import json
from datetime import datetime

import structlog

from toolbelt.types import Network

from .cf_hosted_file_manager import CFHostedFileManager
from .constants import PLAYER_VERSION_FILE_NAME

logger = structlog.get_logger(__name__)


class PlayerVersionManager(CFHostedFileManager):
    def __init__(self) -> None:
        super().__init__()

    def update_player_version(self, version: int, commit_hash: str, network: Network):
        exists_history_contents = self.get_player_version(network)
        logger.debug("Exists player version file downloaded", network=network)

        exists_history_contents[version] = {
            "version": version,
            "commit_hash": commit_hash,
            "timestamp": datetime.utcnow().strftime("%Y-%m-%d"),
        }

        file_path = self._get_file_path(network)
        self._upload_player_version(file_path, exists_history_contents)

        logger.info("New apv_history file uploaded", path=file_path)

        def check(contents: dict):
            try:
                contents[str(version)]
                return True
            except KeyError:
                return False

        self._create_invalidation_with_retry(file_path, check)

    def get_player_version(self, network: Network):
        return self._get_contents(self._get_file_path(network))

    def _upload_player_version(self, file_path: str, contents: str):
        self._upload_contents(file_path, json.dumps(contents))

    def _get_file_path(self, network: Network):
        file_path = f"{network}/{PLAYER_VERSION_FILE_NAME}"
        return file_path
