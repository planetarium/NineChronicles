import typer

from app.apps import *

import structlog
import logging
from app.config import config

if config.env == "production":
    structlog.configure(
        wrapper_class=structlog.make_filtering_bound_logger(logging.INFO),
    )

app = typer.Typer()
app.add_typer(release_app, name="release")

if __name__ == "__main__":
    app()
