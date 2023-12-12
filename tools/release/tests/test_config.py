from toolbelt.config import config


def test_config():
    assert config.env is not None
    assert config.slack_token is not None
    assert config.github_token is not None
    assert config.key_passphrase is not None
    assert config.key_address is not None
