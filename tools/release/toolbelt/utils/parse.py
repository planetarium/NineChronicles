import re
from typing import List, Tuple

from toolbelt.exceptions import TagNotFoundError


def latest_tag(tags: List[dict], rc: int, prefix: str = "") -> Tuple[str, str]:
    rc_tags = filter_tags(tags, rc, prefix)

    try:
        latest = sorted(
            rc_tags,
            key=lambda x: int(x[0].group(1)),
            reverse=True,
        )[0]
    except IndexError:
        raise TagNotFoundError(f"rc tags: {rc_tags}, prefix: {prefix}")

    return latest[1]["name"], latest[1]["commit"]["sha"]


def filter_tags(
    tags: List[dict], rc: int, prefix: str = ""
) -> List[Tuple[re.Match[str], dict]]:
    rc_number = f"v{rc}"
    deploy_number = "([0-9]+)"

    rg = rf"{prefix}{rc_number}-{deploy_number}"

    r = list(
        filter(
            lambda x: x[0] is not None,
            [
                (
                    re.fullmatch(rg, tag["name"]),
                    tag,
                )
                for tag in tags
            ],
        )
    )

    return r  # type:ignore
