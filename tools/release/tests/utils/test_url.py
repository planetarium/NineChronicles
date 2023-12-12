import pytest

from toolbelt.utils.url import build_download_url


@pytest.mark.parametrize(
    "test_args,url",
    [
        (
            [
                "https://test.com",
                "internal",
                100,
                "player",
                "1asefiei13ifd",
                "Windows.zip",
            ],
            "https://test.com/internal/v100/player/1asefiei13ifd/Windows.zip",
        ),
        (
            [
                "https://test.com",
                "internal",
                1100,
                "launcher",
                "1asefiei13ifd",
                "Linux.tar.gz",
            ],
            "https://test.com/internal/v1100/launcher/1asefiei13ifd/Linux.tar.gz",
        ),
        (
            [
                "https://test.com",
                "main",
                1020,
                "player",
                "1asefiei13ifd",
                "Windows.zip",
            ],
            "https://test.com/main/v1020/player/1asefiei13ifd/Windows.zip",
        ),
    ],
)
def test_build_download_url(test_args, url):
    result = build_download_url(*test_args)

    assert result == url
