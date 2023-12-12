import json
import time
from typing import Callable

import requests
import structlog
from botocore.exceptions import ClientError

from toolbelt.client.new_aws import (
    DOWNLOAD_DISTRIBUTION_ID,
    RELEASE_DISTRIBUTION_ID,
    CFClient,
    S3Client,
)
from toolbelt.constants import RELEASE_BASE_URL, RELEASE_BUCKET

logger = structlog.get_logger(__name__)


class CFHostedFileManager:
    def __init__(self) -> None:
        self.s3_client = S3Client(RELEASE_BUCKET)
        self.cf_client = CFClient()

    def _get_contents(self, file_path: str):
        try:
            exists_history_contents = json.loads(self.s3_client.read_file(file_path))
        except ClientError as e:
            if e.response["Error"]["Code"] == "NoSuchKey":
                exists_history_contents = {}
            else:
                raise

        return exists_history_contents

    def _upload_contents(self, file_path: str, contents: str):
        self.s3_client.upload(contents, file_path)

    def _create_invalidation_with_retry(
        self, file_path: str, check: Callable[[dict], bool]
    ):
        for _ in range(10):
            self.cf_client.create_invalidation([file_path], RELEASE_DISTRIBUTION_ID)
            self.cf_client.create_invalidation([file_path], DOWNLOAD_DISTRIBUTION_ID)

            r = requests.get(f"{RELEASE_BASE_URL}/{file_path}")
            apv_history_contents = r.json()

            if check(apv_history_contents):
                logger.info("Invalidation created", path=file_path)
                break
            else:
                logger.info("Not applied, retry", path=file_path, count=_)
                time.sleep(60)
