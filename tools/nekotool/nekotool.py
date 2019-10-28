import os
import json
import click
import requests
import tempfile
from azure.common.credentials import ServicePrincipalCredentials
from azure.mgmt.storage import StorageManagementClient

TEMPDIR = tempfile.gettempdir()

def download(url) -> str:
    filename = list(filter(lambda x: x!='', url.split('/')))[-1]
    download_path = f"{TEMPDIR}/{filename}"
    with open(download_path, 'wb') as f:
        f.write(requests.get(url).content)
    return download_path

@click.group(help="Useful tools for operation.")
def cli():
    ...

@cli.group()
def storage():
    ...

@cli.group()
def misc():
    ...

@misc.command(help="Generate secp256k1 keypair.")
@click.option(
    '-o', '--output',
    type=click.File('w'),
    default='key.json',
    help="Path of file to write keys generated.")
def generate_key(output: click.File):
    import json
    from ecdsa import SigningKey, SECP256k1
    sk = SigningKey.generate(curve=SECP256k1)
    public_key = sk.verifying_key.to_string().hex()
    private_key = sk.to_string().hex()
    click.echo(f"private_key: {private_key}")
    click.echo(f"public_key: {public_key}")
    json.dump({
        'publicKey': public_key,
        'privateKey': private_key,
    }, output, indent=4, sort_keys=True)

@storage.command(help="Upload file to azure storage.")
@click.option(
    '-f', '--file',
    type=click.Path(exists=True, resolve_path=True),
    default=None,
    help="DLL to update."
)
@click.option(
    '-v', '--version',
    type=str,
    default=None,
    help="Libplanet version you wanna upload to storage."
)
@click.option(
    '--shared-directory',
    type=str,
    default='shared_dlls',
    help="Path where dll will be uploaded."
)
@click.option(
    '-c', '--config-path',
    type=str,
    default='~/.nekotool.config.json',
    help="Path of config file."
)
def upload(file, version, shared_directory, config_path):
    config_path = os.path.expanduser(config_path)
    with open(config_path) as f:
        config = json.load(f)

    def upload(path: str):
        from azure.storage.file import FileService
        service = FileService(
            account_name=config['account_name'],
            account_key=config['account_key'])
        if shared_directory not in service.list_directories_and_files(config['share_name']):
            service.create_directory(config['share_name'], shared_directory)
        service.create_file_from_path(config['share_name'], shared_directory, path.split('/')[-1], path)
    if file is not None:
        upload(file)
    elif version is not None:
        from zipfile import ZipFile
        url = 'https://api.nuget.org/v3-flatcontainer/libplanet/{0}/libplanet.{0}.nupkg'.format(version)
        downloaded_path = download(url) # download
        zipfile = ZipFile(downloaded_path)
        extracted_directory = f"{TEMPDIR}/libplanet-{version}"
        zipfile.extractall(extracted_directory) # extract
        upload(extracted_directory+'/lib/netstandard2.0/Libplanet.dll')
        upload(extracted_directory+'/lib/netstandard2.0/Libplanet.Stun.dll')
    else:
        click.echo("At least one of the --file or --version options must be provided.")

@storage.command(help="Cleanup files and directory in shared directory.")
@click.option(
    '--shared-directory',
    type=str,
    default='shared_dlls',
    help="Path where dll will be uploaded."
)
@click.option(
    '-c', '--config-path',
    type=str,
    default='~/.nekotool.config.json',
    help="Path of config file."
)
@click.option(
    '-R', '--remove-directory',
    is_flag=True,
    help="Remove directory too."
)
def clean(shared_directory, config_path, remove_directory):
    config_path = os.path.expanduser(config_path)
    with open(config_path) as f:
        config = json.load(f)

    from azure.storage.file import FileService
    service = FileService(
        account_name=config['account_name'],
        account_key=config['account_key'])
    if service.exists(config['share_name'], shared_directory):
        for file in service.list_directories_and_files(config['share_name'], shared_directory):
            service.delete_file(config['share_name'], shared_directory, file.name)
        if remove_directory:
            service.delete_directory(config['share_name'], shared_directory)

@storage.command(
    help="Setup azure storage configuration."
)
@click.option(
    '-o', '--output',
    default="~/.nekotool.config.json",
    help="Path to export config as json."
)
def setup(output):
    from typing import Dict
    from azure.common.client_factory import get_client_from_cli_profile
    from azure.mgmt.storage import StorageManagementClient
    from azure.mgmt.storage.v2019_04_01.models._models_py3 import StorageAccount

    client: StorageManagementClient = get_client_from_cli_profile(StorageManagementClient)

    # select storage-account
    def storage_account_name_with_prefix(index: int, account: StorageAccount):
        account_id = account.id
        resource_group_name = account_id.split('/')[4]
        account_name = account.name
        return f"{index}: account_name={account_name} / resource_group_name={resource_group_name}"
    
    def make_storage_account_select_message(accounts):
        # help_message = "# You should write-and-quit this file after change prefix of storage account which you wanna to use, to 'y' from 'n'. ^-^\n"\
        #             + "# Also you shouldn't change format plz. If you don't, see https://bit.ly/31ACJIJ \n\n"
        content = "\n".join(map(lambda x: storage_account_name_with_prefix(*x), enumerate(accounts)))
        return content

    accounts = sorted(client.storage_accounts.list(), key=lambda x: x.name)
    print(make_storage_account_select_message(accounts))
    print()
    print("Choose number of account.")
    print(': ', end='')
    result = int(input())
    choosed_account = accounts[result]

    config = {
        'resource_group_name': choosed_account.id.split('/')[4],
        'account_name': choosed_account.name,
    }

    account_key = client.storage_accounts.list_keys(**config).keys[0].value
    config['account_key'] = account_key

    file_shares = list(client.file_shares.list(
        resource_group_name=config['resource_group_name'],
        account_name=config['account_name']))

    print(*map(lambda x: f"{x[0]}: {x[1].name} [modified at: {x[1].last_modified_time}]", enumerate(file_shares)), sep='\n')
    print()
    print("Choose number of account.")
    print(': ', end='')
    result = int(input())
    choosed_file_share = file_shares[result]

    config['share_name'] = choosed_file_share.name
    
    output_path = os.path.expanduser(output)
    with open(output_path, 'w') as f:
        json.dump(config, f, indent=4)

if __name__ == '__main__':
    cli()
