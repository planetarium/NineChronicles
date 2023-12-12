from typing import get_args

import typer

from toolbelt.types import Network, Platforms


def version_validation(ctx: typer.Context, p: typer.CallbackParam, v: str):
    if ctx.resilient_parsing:
        return
    if len(v) != 7 or not v.startswith("v"):
        raise typer.BadParameter("APV version (e.g. v100086)")
    return v


def network_validation(ctx: typer.Context, p: typer.CallbackParam, v: str):
    if ctx.resilient_parsing:
        return

    network_map = {v: v for v in get_args(Network)}
    try:
        network_map[v]
    except KeyError:
        raise ValueError(f"Network should in {get_args(Network)}")
    return v


def platforms_validation(ctx: typer.Context, p: typer.CallbackParam, v: str):
    if ctx.resilient_parsing:
        return

    platforms_map = {v: v for v in get_args(Platforms)}
    try:
        platforms_map[v]
    except KeyError:
        raise ValueError(f"Platforms should in {get_args(Platforms)}")
    return v


version_arg = typer.Argument(
    ..., help="RC version e.g. v100260", callback=version_validation
)
network_arg = typer.Argument(
    ...,
    help="Network type [internal, main, preview]",
    callback=network_validation,
)
platforms_arg = typer.Argument(
    ...,
    help="Platform type [Windows, macOS, Linux]",
    callback=platforms_validation,
)
apv_arg = typer.Argument(..., help="APV version ie. 1085/123...")
