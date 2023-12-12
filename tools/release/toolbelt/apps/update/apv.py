from datetime import datetime

import structlog

from toolbelt.config import config
from toolbelt.manager import APVHistoryManager
from toolbelt.tools.planet import Apv, Planet
from toolbelt.types import Network

logger = structlog.get_logger(__name__)


def append_apv(number: int, network: Network):
    planet = Planet(config.key_address, config.key_passphrase)
    remote_config_manager = APVHistoryManager()

    apv = generate_apv(planet, number)
    logger.info("APV Created", version=apv.version, signer=apv.signer)

    remote_config_manager.append_apv(apv, network)


def remove_apv(number: int, network: Network):
    remote_config_manager = APVHistoryManager()
    remote_config_manager.remove_apv(number, network)


def generate_apv(planet: Planet, number: int) -> Apv:
    timestamp = datetime.utcnow().strftime("%Y-%m-%d")
    extra = {}
    extra["timestamp"] = timestamp

    apv = planet.apv_sign(
        number,
        **extra,
    )

    return apv
