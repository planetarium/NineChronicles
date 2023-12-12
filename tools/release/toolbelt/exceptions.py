class ClientError(Exception):
    pass


class ResponseError(ClientError):
    pass


class CliError(Exception):
    def __init__(self, cmd: str, msg: str):
        super().__init__(msg)

        self.cmd = cmd

    def __str__(self):
        return f"Command: {self.cmd}, Message: {super().__str__()}"


class PlanetError(CliError):
    pass


class EsignerError(CliError):
    pass


class TagNotFoundError(Exception):
    pass
