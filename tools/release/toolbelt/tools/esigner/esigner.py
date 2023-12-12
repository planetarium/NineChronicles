import shlex
import subprocess

from toolbelt.config import config
from toolbelt.exceptions import EsignerError


class Esigner:
    def sign(
        self,
        *,
        credential_id: str,
        username: str,
        password: str,
        input_dir_path: str,
        output_dir_path: str,
        totp_secret: str,
    ):
        raw_command = (
            f"sh CodeSignTool.sh batch_sign "
            f"-credential_id={credential_id} "
            f"-username={username} "
            f"-password={password} "
            f"-input_dir_path={input_dir_path} "
            f"-output_dir_path={output_dir_path} "
            f"-totp_secret={totp_secret}"
        )

        result = subprocess.run(
            shlex.split(raw_command),
            capture_output=True,
            text=True,
            cwd=config.esigner_path,
        )

        if not result.stdout:
            raise EsignerError("sh CodeSignTool.sh batch_sign", result.stderr)
        elif "Error" in result.stdout:
            raise EsignerError("sh CodeSignTool.sh batch_sign", result.stdout)

        return result
