from typing import Never

class BaseAssertableException(BaseException):
    @classmethod
    def raise_if(cls, condition: bool, message: str) -> None | Never:
        if condition:
            raise cls(message)
