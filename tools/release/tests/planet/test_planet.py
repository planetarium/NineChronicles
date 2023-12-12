import pytest

from toolbelt.exceptions import PlanetError
from toolbelt.tools.planet import Apv, Planet

PASSPHRASE = "test"
PRIVATE_KEY = "97548c4d920d07934c19fc012793cff0cb4a9da7c8986d971fcb4759ae31364b"
ADDRESS = "0x0B442988524d719FFb938cDe2DbbB2Ad619Bb3CA"


def test_apv_analyze_success():
    planet = Planet(ADDRESS, PASSPHRASE)

    raw_apv = "100/0B442988524d719FFb938cDe2DbbB2Ad619Bb3CA/MEQCIBWKJbA9yKZsIx8eAJ3lCUBowWXVc+spHUZKb7aah2M9AiBStP6GtIQ.Xtnlsb81rak.ARc+fo0RvZ1kkYEw9Hyc0w==/ZHU0OnRlc3R1NDp0ZXN0ZQ=="  # noqa
    except_result = Apv(
        version=100,
        signature="30440220158a25b03dc8a66c231f1e009de5094068c165d573eb291d464a6fb69a87633d022052b4fe86b4843f5ed9e5b1bf35ada93f01173e7e8d11bd9d64918130f47c9cd3",  # noqa
        signer=ADDRESS,
        extra={"test": "test"},
        raw=raw_apv,
    )

    result = planet.apv_analyze(raw_apv)

    assert result == except_result


def test_apv_analyze_failure():
    planet = Planet(ADDRESS, PASSPHRASE)

    raw_apv = "100/0B442988524d719FFb938cDeAJ3lCUBowWXVc+spHUZKb7aah2M9AiBStP6GtIQ.Xtnlsb81rak.ARc+fo0RvZ1kkYEw9Hyc0w==/ZHU0OnRlc3R1NDp0ZXN0ZQ=="  # noqa

    with pytest.raises(PlanetError):
        planet.apv_analyze(raw_apv)


def test_apv_sign():
    planet = Planet(ADDRESS, PASSPHRASE)

    raw_apv = "100/0B442988524d719FFb938cDe2DbbB2Ad619Bb3CA/MEQCIA3fH5WrxS1W3xreetU+7tfYdZeV5CHzjbrEsjVBayLpAiBuIZxgE96nw296hVrCw23FtjmwSZ8+qoDw8t0ZS32Xaw==/ZHU4OmxhdW5jaGVydTQyOjEvODY5OTNhNDE3ZDZlNjRjZTkwMmU5MTZkNjgwZGIxNGNjOTc2NTNmMXU2OnBsYXllcnU0MToxLzg4NGNjNzk2NjQ1N2Q1MDNlZjYxNTZmMTQ4Y2FkZTYzNGI3MmExYnU5OnRpbWVzdGFtcHUxMDoyMDIyLTAzLTAyZQ=="  # noqa
    expect_result = Apv(
        version=100,
        signature="304402200ddf1f95abc52d56df1ade7ad53eeed7d8759795e421f38dbac4b235416b22e902206e219c6013dea7c36f7a855ac2c36dc5b639b0499f3eaa80f0f2dd194b7d976b",  # noqa
        signer=ADDRESS,
        extra={
            "timestamp": "2022-03-02",
            "launcher": "1/86993a417d6e64ce902e916d680db14cc97653f1",
            "player": "1/884cc7966457d503ef6156f148cade634b72a1b",
        },
        raw=raw_apv,
    )

    result = planet.apv_sign(
        100,
        timestamp="2022-03-02",
        launcher="1/86993a417d6e64ce902e916d680db14cc97653f1",
        player="1/884cc7966457d503ef6156f148cade634b72a1b",
    )

    assert result == expect_result
