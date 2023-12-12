from random import random

import pytest

from toolbelt.exceptions import TagNotFoundError
from toolbelt.utils.parse import latest_tag


def create_tag(name: str):
    return {
        "name": name,
        "commit": {
            "sha": str(random()),
        },
    }


v100290_1 = create_tag("v100290-1")
v100290_5 = create_tag("v100290-5")
v100290_10 = create_tag("v100290-10")
v100290_15 = create_tag("v100290-15")
v100302_1 = create_tag("v100302-1")
v100302_2 = create_tag("v100302-2")
v100402_5 = create_tag("v100402-5")
v100402_9 = create_tag("v100402-9")
v100402_14 = create_tag("v100402-14")

bad_tag = create_tag("bad-tag")

internal_v100290_1 = create_tag("internal-v100290-1")
internal_v100290_5 = create_tag("internal-v100290-5")


@pytest.mark.parametrize(
    "tags,rc,expect_result",
    [
        (
            # check if not sort to use string
            [v100290_1, v100290_5, v100290_10, v100290_15],
            100290,
            (v100290_15["name"], v100290_15["commit"]["sha"]),
        ),
        (
            # check normal case
            [v100302_1, v100302_2],
            100302,
            (v100302_2["name"], v100302_2["commit"]["sha"]),
        ),
        (
            # check shuffled case
            [v100290_10, v100402_5, v100402_9, v100302_1, bad_tag],
            100402,
            (v100402_9["name"], v100402_9["commit"]["sha"]),
        ),
    ],
)
def test_latest_tag_normal(tags: list, rc: int, expect_result):
    r = latest_tag(tags, rc, prefix="")
    assert r == expect_result


@pytest.mark.parametrize(
    "tags,rc,err",
    [
        (
            # rc not found
            [v100290_10, v100402_5, v100402_9],
            100002,
            TagNotFoundError,
        ),
        (
            # empty list
            [],
            100302,
            TagNotFoundError,
        ),
    ],
)
def test_latest_tag_failure(tags: list, rc: int, err):
    with pytest.raises(err):
        latest_tag(tags, rc, prefix="")


@pytest.mark.parametrize(
    "tags,rc,expect_result",
    [
        (
            # check if not sort to use string
            [internal_v100290_1, internal_v100290_5],
            100290,
            (
                internal_v100290_5["name"],
                internal_v100290_5["commit"]["sha"],
            ),
        ),
    ],
)
def test_latest_tag_prefix(tags: list, rc: int, expect_result):
    r = latest_tag(tags, rc, prefix="internal-")
    assert r == expect_result
