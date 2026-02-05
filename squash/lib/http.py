from pathlib import Path
from certifi import where
from collections.abc import Callable
from typing import Dict, Union, Optional
from urllib.request import Request, urlopen
from urllib.error import HTTPError, URLError
from ssl import Purpose, create_default_context

class HttpClient:
    def __init__(self, timeout: float = 30.0):
        self.timeout = timeout
        self.ssl_context = create_default_context(Purpose.SERVER_AUTH, cafile=where())

    def request(
        self,
        method: str,
        url: str,
        *,
        headers: Optional[Dict[str, str]] = None,
        data: Optional[Union[str, bytes]] = None,
    ) -> bytes:
        if isinstance(data, str):
            data = data.encode("utf-8")

        req = Request(
            url,
            data=data,
            headers=headers or {},
            method=method.upper(),
        )

        try:
            with urlopen(req, timeout=self.timeout, context=self.ssl_context) as resp:
                return resp.read()
        except HTTPError as e:
            raise RuntimeError(f"HTTP {e.code}: {e.reason}") from e
        except URLError as e:
            raise RuntimeError(f"Request failed: {e.reason}") from e

    def download(
        self,
        url: str,
        dest: Union[str, Path],
        *,
        headers: Optional[Dict[str, str]] = None,
        chunk_size: int = 8192,
        on_progress: Optional[Callable[[int, int], None]] = None,
    ) -> None:
        dest = Path(dest)
        req = Request(url, headers=headers or {}, method='GET')

        try:
            with urlopen(req, timeout=self.timeout, context=self.ssl_context) as resp, dest.open('wb') as f:
                total = int(resp.headers.get('Content-Length', 0))
                downloaded = 0

                if on_progress:
                    on_progress(0, total)

                while True:
                    chunk = resp.read(chunk_size)
                    if not chunk:
                        break

                    f.write(chunk)
                    downloaded += len(chunk)

                    if on_progress:
                        on_progress(downloaded, total)
        except HTTPError as e:
            raise RuntimeError(f'HTTP {e.code}: {e.reason}') from e
        except URLError as e:
            raise RuntimeError(f'Download failed: {e.reason}') from e
