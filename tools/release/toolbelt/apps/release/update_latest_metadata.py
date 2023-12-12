from typing import Optional

import structlog
from toolbelt.client import SlackClient
from toolbelt.config import config

from toolbelt.manager.latest_metadata_manager import LatestMetadataManager

logger = structlog.get_logger(__name__)

def update(
        version: int,
        commit_hash: str,
        network: str,
        slack_channel: Optional[str] = None
):
    config_manager = LatestMetadataManager()
    config_manager.update_latest_version(version, commit_hash, network)

    slack = SlackClient(config.slack_token)

    if slack_channel:
        slack.send_simple_msg(
            slack_channel,
            f"[Player] Completed updating latest version metadata for '{network}' network - update will be triggered from now on",
        )
