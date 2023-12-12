import json

import structlog

from toolbelt.tools.planet import Apv
from toolbelt.types import Network

from .cf_hosted_file_manager import CFHostedFileManager
from .constants import APV_HISTORY_FILE_NAME

logger = structlog.get_logger(__name__)


class APVHistoryManager(CFHostedFileManager):
    def __init__(self) -> None:
        super().__init__()

    def append_apv(self, apv: Apv, network: Network):
        exists_history_contents = self.get_apv_history(network)
        logger.debug("Exists apv_history file downloaded", network=network)

        exists_history_contents[apv.version] = {
            "number": apv.version,
            "signer": apv.signer,
            "raw": apv.raw,
            "timestamp": apv.extra["timestamp"],
        }

        file_path = self._get_apv_history_path(network)
        self.upload_apv_history(file_path, exists_history_contents)

        logger.info("New apv_history file uploaded", path=file_path)

        def check(contents: dict):
            try:
                contents[str(apv.version)]
                return True
            except KeyError:
                return False

        self._create_invalidation_with_retry(file_path, check)

    def remove_apv(self, number: int, network: Network):
        exists_history_contents = self.get_apv_history(network)
        logger.debug("Exists apv_history file downloaded", network=network)

        exists_history_contents.pop(str(number))

        file_path = self._get_apv_history_path(network)
        self.upload_apv_history(file_path, exists_history_contents)

        logger.info("Apv removed", path=file_path)

        def check(contents: dict):
            try:
                contents[str(number)]
                return False
            except KeyError:
                return True

        self._create_invalidation_with_retry(file_path, check)

    def get_apv_history(self, network: Network):
        exists_history_contents = self._get_contents(self._get_apv_history_path(network))
        return exists_history_contents

    def upload_apv_history(self, file_path: str, contents: str):
        self._upload_contents(file_path, json.dumps(contents))

    def _get_apv_history_path(self, network: Network):
        file_path = f"{network}/{APV_HISTORY_FILE_NAME}"
        return file_path
