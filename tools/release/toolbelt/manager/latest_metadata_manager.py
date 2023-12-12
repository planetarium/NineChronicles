import json
import structlog

from toolbelt.types import Network

from .cf_hosted_file_manager import CFHostedFileManager
from .constants import LATEST_METADATA_FILE_NAME
from ..apps.release.version import generate_latest

logger = structlog.get_logger(__name__)


class LatestMetadataManager(CFHostedFileManager):
    def __init__(self) -> None:
        super().__init__()

    def update_latest_version(self, version: int, commit_hash: str, network: Network):
        exists_history_contents = generate_latest(version, commit_hash)

        file_path = self._get_file_path(network)
        self._upload_latest_version(file_path, exists_history_contents)

        logger.info("New latest metadata file uploaded", path=file_path)

        def check(contents: dict):
            # try:
            #     contents[str(version)]
            #     return True
            # except KeyError:
            #     return False

            try:
                if contents["version"] == version:
                    return True
                return False
            except KeyError:
                return False

        self._create_invalidation_with_retry(file_path, check)

    def get_latest_version(self, network: Network):
        return self._get_contents(self._get_file_path(network))

    def _upload_latest_version(self, file_path: str, contents: dict):
        self._upload_contents(file_path, json.dumps(contents, indent=4))

    def _get_file_path(self, network: Network):
        file_path = f"{network}/player/{LATEST_METADATA_FILE_NAME}"
        return file_path
