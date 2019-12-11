9C Data Snapshot Manager
========================

Prebuilt binaries
-----------------

- [macOS](https://9c-data-snapshots.s3.amazonaws.com/9c-snapshot.osx-x64.tar.gz)
- [Windows](https://9c-data-snapshots.s3.amazonaws.com/9c-snapshot.win-x64.zip)

Above binaries are automatically generated using GitHub Actions.


How to use
----------

Without any CLI options, it downloads the latest snapshot from the S3 bucket
and replace the local 9C data with it.  For more options run the program
with `--help` option.


How to upload a new snapshot
----------------------------

This program only does archiving, so you need `awscli` which is [configured][1]
to upload a snapshot archive to the S3 bucket (`9c-data-snapshots`).

On macOS:

~~~~ bash
aws s3 cp \
  --acl public-read \
  --storage-class REDUCED_REDUNDANCY \
  "$(dotnet run -r osx-x64 -- -d)" \
  s3://9c-data-snapshots/
~~~~

On Windows (PowerShell):

~~~~ pwsh
aws s3 cp `
  --acl public-read `
  --storage-class REDUCED_REDUNDANCY `
  "$(dotnet run -r win-x64 -- -d)" `
  s3://9c-data-snapshots/
~~~~

[1]: https://docs.aws.amazon.com/cli/latest/userguide/cli-chap-configure.html


How to build binaries
---------------------

Each binary for a platform should be built in the corresponding platform.
I.e., you cannot build a Windows binary from macOS or vice versa.

On macOS:

~~~~ bash
dotnet publish -r osx-x64 -c Release -o bin
~~~~

On Windows:

~~~~ pwsh
dotnet publish -r win-x64 -c Release -o bin
~~~~

Created binaries will go to the *bin/* directory.