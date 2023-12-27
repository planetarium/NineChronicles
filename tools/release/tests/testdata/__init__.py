import json
import os

from tests.constants import DATA_DIR


def read_file_as_json(path: str):
    return json.loads(read_file(path))


def read_file(path: str):
    with open(os.path.join(DATA_DIR, path), mode="r") as f:
        data = f.read()

    return data
