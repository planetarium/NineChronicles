import time
from typing import Any, List

import boto3

DOWNLOAD_DISTRIBUTION_ID = "E1HPTSGY2RETN4"
RELEASE_DISTRIBUTION_ID = "E3SBBH63NSNYX"


class S3Client:
    def __init__(self, bucket: str):
        self.bucket_name = bucket

        self.s3 = boto3.client("s3")

    def read_file(self, path):
        response = self.s3.get_object(Bucket=self.bucket_name, Key=path)
        contents = response["Body"].read().decode("utf-8")
        return contents

    def upload(self, contents: Any, path: str):
        self.s3.put_object(Bucket=self.bucket_name, Key=path, Body=contents)


class CFClient:
    def __init__(self):
        self.cf = boto3.client("cloudfront")

    def create_invalidation(self, path_list: List[str], distribution_id: str) -> str:
        items = [f"/{path}" for path in path_list]
        response = self.cf.create_invalidation(
            DistributionId=distribution_id,
            InvalidationBatch={
                "Paths": {"Quantity": len(items), "Items": items},
                "CallerReference": str(time.time()).replace(".", ""),
            },
        )

        return response["Invalidation"]["Id"]
