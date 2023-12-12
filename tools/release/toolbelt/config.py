import os
from typing import NamedTuple, Optional, get_args

from dotenv import load_dotenv

from toolbelt.types import Env

load_dotenv(".env")


class Config(NamedTuple):
    # Github token (commit, read)
    github_token: str
    # Runtime API Url
    runtime_url: str
    # Runtime API Token
    runtime_token: str
    # signer key passphrase
    key_passphrase: Optional[str] = None
    # signer key address
    key_address: Optional[str] = None
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
        self.runtime_url = os.environ["ACTIONS_RUNTIME_URL"]
        self.runtime_token = os.environ["ACTIONS_RUNTIME_TOKEN"]

        env_map = {v: v for v in get_args(Env)}
        try:
            self.env = env_map[_env]
        except KeyError:
            raise ValueError(f"Env should in {get_args(Env)}")

        github_token = os.environ["GITHUB_TOKEN"]

        if not github_token:
            raise ValueError(f"github_token is required")

        self.github_token = github_token

        for v in [
            "SLACK_TOKEN",
            "KEY_PASSPHRASE",
            "KEY_ADDRESS",
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
