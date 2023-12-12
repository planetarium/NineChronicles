from typing import Optional

import typer

from toolbelt.utils.typer import network_arg, platforms_arg

from .release_player import release as release_player
from .update_latest_metadata import update as update_latest_metadata

release_app = typer.Typer()


def convert_version(version):
    x, y, z = version.split(".")
    return int(x.zfill(4) + y.zfill(4) + z.zfill(4))


@release_app.command()
def player(
    commit_hash: str,
    version: str,
    network: str = network_arg,
    platform: str = platforms_arg,
    signing: bool = False,
    slack_channel: Optional[str] = None,
    run_id: Optional[str] = None,
):
    converted_version = convert_version(version)

    release_player(
        commit_hash,
        platform,  # type:ignore
        converted_version,
        network,  # type:ignore
        signing,
        slack_channel,
        run_id,
    )


@release_app.command()
def update_latest(
    commit_hash: str,
    version: str,
    network: str,
    slack_channel: Optional[str] = None,
):
    converted_version = convert_version(version)

    update_latest_metadata(
        converted_version,
        commit_hash,
        network,
        slack_channel,
    )
