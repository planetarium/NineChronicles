from typing import Dict, NamedTuple


class Apv(NamedTuple):
    # Apv version number
    version: int
    # Signer's signature
    signature: str
    # Signer Address
    signer: str
    # Apv extra data (i.e. {player: "1/asef2rassfaefaesf"})
    extra: Dict[str, str]
    # Raw apv string
    raw: str
