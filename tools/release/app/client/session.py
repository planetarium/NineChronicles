from urllib.parse import urljoin

from requests import Session


class BaseUrlSession(Session):
    def __init__(self, base_url: str):
        self.base_url = base_url
        super(BaseUrlSession, self).__init__()

    def request(self, method, url, *args, **kwargs):
        url = urljoin(self.base_url, url)
        return super(BaseUrlSession, self).request(method, url, *args, **kwargs)

    def prepare_request(self, request, *args, **kwargs):
        request.url = urljoin(self.base_url, request.url)
        return super(BaseUrlSession, self).prepare_request(request, *args, **kwargs)
