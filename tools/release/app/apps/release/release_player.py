from typing import Optional

import tempfile
import os
import structlog

from app.client import SlackClient
from app.config import config
from app.tools.esigner import Esigner
from app.constants import BINARY_FILENAME_MAP, RELEASE_BASE_URL
from app.types import Network, Platforms

from .release_manager import signing_for_windows, unpack_player, upload_player
from .version import create_version_json
from app.utils.zip import compress

logger = structlog.get_logger(__name__)


def release(
    commit_hash: str,
    platform: Platforms,
    version: int,
    network: Network,
    signing: bool,
    binary_path: str,
    slack_channel: Optional[str],
):
    logger.debug(
        "Release Start",
        commit_hash=commit_hash,
        platform=platform,
        version=version,
        binary_path=binary_path,
        network=network,
        signing=signing,
        slack_channel=slack_channel,
    )

    try:
        slack = SlackClient(config.slack_token)
        esigner = Esigner()

        with tempfile.TemporaryDirectory() as tmp_path:
            extracted_path = unpack_player(tmp_path, binary_path, platform)

            logger.debug("Inject version metadata", app="player", os=platform)
            create_version_json(platform, commit_hash, version, os.path.join(extracted_path, "version.json"))

            if platform == "Windows" and signing:
                signing_for_windows(esigner, extracted_path, extracted_path, tmp_path)

            compressed_path = compress(f"{tmp_path}/result", extracted_path, os.path.join(tmp_path, BINARY_FILENAME_MAP[platform]))
            logger.debug(
                "Finish compressing artifact with version metadata",
                app="player",
                os=platform,
                compress_path=compressed_path
            )

            target_s3_dir = create_target_s3_dir(network, version)
            logger.debug("Target s3 dir", dir=target_s3_dir)

            download_url = f"{RELEASE_BASE_URL}/{target_s3_dir}/{BINARY_FILENAME_MAP[platform]}"

            upload_player(compressed_path, platform, target_s3_dir)

        if slack_channel:
            slack.send_simple_msg(
                slack_channel,
                f"[Player] Prepared '{platform}' binary - {download_url}",
            )
    except Exception:
        if slack_channel:
            slack.send_simple_msg(
                slack_channel,
                f"[Player] Failed to release",
            )
        raise


def create_target_s3_dir(network: Network, version: int):
    return f"{network}/player/{version}"
