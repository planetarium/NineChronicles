import os
from typing import NamedTuple, Optional, get_args

from dotenv import load_dotenv

from toolbelt.types import Env

load_dotenv(".env")


class Config(NamedTuple):
    # Slack Bot API Token
    slack_token: Optional[str] = None
    # esigner path
    esigner_path: Optional[str] = None
    signing_secrets: Optional[dict] = None
    # env
    env: Env = "test"

    @classmethod
    def init(self):
        _env = os.environ["ENV"]

        env_map = {v: v for v in get_args(Env)}
        try:
            self.env = env_map[_env]
        except KeyError:
            raise ValueError(f"Env should in {get_args(Env)}")

        for v in [
            "SLACK_TOKEN",
            "ESIGNER_PATH",
        ]:
            try:
                setattr(self, v.lower(), os.environ[v])
            except KeyError:
                pass

        try:
            self.signing_secrets = {
                "credential_id": os.environ["ESIGNER_CREDENTIAL_ID"],
                "username": os.environ["ESIGNER_USERNAME"],
                "password": os.environ["ESIGNER_PASSWORD"],
                "totp_secret": os.environ["ESIGNER_TOTP_SECRET"],
            }
        except KeyError:
            pass


        return self


config = Config.init()
