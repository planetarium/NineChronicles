import shutil
import subprocess
from datetime import datetime
from typing import List, Tuple

from toolbelt.exceptions import PlanetError

from .apv import Apv

PLANET_CLI_PATH = shutil.which("planet")
assert PLANET_CLI_PATH is not None


class Planet:
    def __init__(self, address: str, passphrase: str) -> None:
        self.address = address
        self.passphrase = passphrase

    @staticmethod
    def apv_analyze(apv: str) -> Apv:
        """
        It runs the `planet apv analyze` command and parse the `Apv`

        :param apv: raw apv string
        :type apv: str
        :return: parsed Apv
        """

        raw_command = f"planet apv analyze {apv}"
        result = subprocess.run(
            raw_command,
            capture_output=True,
            text=True,
            shell=True,
        )
        output = result.stdout

        if not output:
            raise PlanetError(raw_command, result.stderr)

        split = [x for x in output.split()]
        properties = dict((split[i], split[i + 1]) for i in range(0, len(split), 2))
        extra = dict(
            (key[6:], value)
            for key, value in properties.items()
            if key.startswith("extra.")
        )

        return Apv(
            int(properties["version"]),
            properties["signature"],
            properties["signer"],
            extra,
            apv,
        )

    def apv_sign(self, version: int, **kwargs) -> Apv:
        """
        It create new apv with `planet apv sign` command and returns an APV object

        :param version: The version of the APV you're signing
        :type version: int
        :return: The apv_analyze function is being called on the output of the apv_sign function.
        """

        key_id = self.key(self.address)
        raw_command = (
            f"planet apv sign --passphrase {self.passphrase} {key_id} {version} "
        )

        for k, v in kwargs.items():
            raw_command += f"-e {k}={v} "

        out = subprocess.run(raw_command, capture_output=True, text=True, shell=True)
        if not out.stdout:
            raise PlanetError("planet apv sign {{key_id}} {version}", out.stderr)

        return self.apv_analyze(out.stdout.strip())

    def keys(self) -> List[Tuple[str, str]]:
        """
        It runs the `planet key` command and parses the output

        :return: [(key_id, address), ...]
        """

        raw_command = "planet key"
        output = subprocess.run(raw_command, capture_output=True, text=True, shell=True)

        if not output.stdout:
            raise PlanetError(raw_command, output.stderr)

        values = output.stdout.split()
        results: List[Tuple[str, str]] = []

        for i in range(0, len(values), 2):
            key_id = values[i]
            address = values[i + 1]

            results.append((key_id, address))

        return results

    def key(self, address: str) -> str:
        """
        It returns the key_id for the given target_address.

        :param address: The address of the APV key you want to import
        :type address: str
        :return: The key_id of the key that matches the target_address
        """

        keys = self.keys()

        for k, a in keys:
            if a == address:
                return k

        raise ValueError(f"Failed to import APV key for {address}")


def generate_extra(
    commit_map: dict,
    reset_required: bool,
    prev_extra: dict,  # can optional...
):
    """
    If the reset flag is not set, then we increment the version number of each project by one if the
    commit hash has changed

    :param commit_map: A dictionary of project names to commit hashes
    :type commit_map: dict
    :param reset_required: If the versioning should be reset
    :type reset_required: bool
    :param prev_extra: This is the extra data from the previous build
    :type prev_extra: Optional[dict]
    :return: A dictionary of the project name and the version number and commit hash.
    """

    if not reset_required:
        assert prev_extra is not None

    project_data = {}
    for key in commit_map.keys():
        commit = commit_map[key]

        project_version = 1
        if not reset_required:
            try:
                prev_version, prev_commit = prev_extra[key].split("/")
                prev_version = int(prev_version)

                if commit != prev_commit:
                    project_version = prev_version + 1
                else:
                    project_version = prev_version
            except (ValueError, IndexError, KeyError, TypeError):
                # Different Schema
                pass

        project_data[key] = f"{project_version}/{commit}"

    timestamp = datetime.utcnow().strftime("%Y-%m-%d")
    project_data["timestamp"] = timestamp

    return project_data
