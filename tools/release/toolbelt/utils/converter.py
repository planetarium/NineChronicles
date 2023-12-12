from typing import Dict

from toolbelt.github.constants import (
    DP_REPO,
    HEADLESS_REPO,
    MARKET_SERVICE_REPO,
    SEED_REPO,
    WORLD_BOSS_REPO,
)
from toolbelt.types import Network


def dockerhub2github_repo(dockerhub_repo: str):
    dockerhub2github_repo_map = {
        "ninechronicles-headless": HEADLESS_REPO,
        "lib9c-stateservice": HEADLESS_REPO,
        "ninechronicles-dataprovider": DP_REPO,
        "libplanet-seed": SEED_REPO,
        # "nine-chronicles-bridge-observer": BRIDGE_OBSERVER_REPO,
        "world-boss-service": WORLD_BOSS_REPO,
        "market-service": MARKET_SERVICE_REPO,
    }

    try:
        return dockerhub2github_repo_map[dockerhub_repo]
    except KeyError:
        raise ValueError(f"Not found {dockerhub_repo} matched repo")


def infra_dir2network(dir: str) -> Network:
    infra_dir2network_map: Dict[str, Network] = {
        "9c-main": "main",
        "9c-internal": "internal",
    }

    try:
        return infra_dir2network_map[dir]
    except KeyError:
        raise ValueError(f"Not found {dir} matched network")
