from enum import IntEnum
from typing import Union, TextIO

EOL = '\n'
Input = Union[str, int, Exception]
CSI = '\x1b['

def esc(code: str) -> str:
    return f'{CSI}{code}'

class Styles:
    reset = esc('0m')
    bold = esc('1m')
    dim = esc('2m')
    italic = esc('3m')
    underline = esc('4m')
    inverse = esc('7m')
    hidden = esc('8m')
    strikethrough = esc('9m')

    @staticmethod
    def color_rgb(r: int, g: int, b: int) -> str:
        return esc(f'38;2;{r};{g};{b}m')

    color_close = esc('39m')

def erase_line() -> str:
    return esc('2K')

def erase_lines(n: int) -> str:
    return ''.join(f'{esc('2K')}{esc('1A')}' for _ in range(n))

def erase_end_line() -> str:
    return esc('K')

def link(text: str, url: str) -> str:
    return f'\x1b]8;;{url}\x1b\\{text}\x1b]8;;\x1b\\'

class MessageSeverity(IntEnum):
    INFO = 0
    WARNING = 1
    ERROR = 2
    SUCCESS = 3

class MessageName(IntEnum):
    PADDING = 0x0
    ARGUMENT_ERROR = 0x1
    INPUT_FILE_TOO_SMALL = 0x2
    LOW_BITRATE_WARNING = 0x3
    PROGRESS = 0x4
    ITERATION = 0x5
    ITERATION_RESULT = 0x6
    VENDOR_BINARY_NOT_FOUND = 0x7
    GENERIC_ERROR = 0x50
    RESULTS = 0x99

PREFIX = 'SQ'

class Tui:
    def __init__(self, stdout_: TextIO, stderr_: TextIO):
        self.stdout = stdout_
        self.stderr = stderr_

        self.brand_info = Styles.color_rgb(0x00, 0x92, 0xB8)
        self.brand_warning = Styles.color_rgb(0xF0, 0xB1, 0x00)
        self.brand_error = Styles.color_rgb(0xE7, 0x00, 0x0B)
        self.brand_success = Styles.color_rgb(0x7C, 0xCF, 0x00)

        self.arrows = {
            MessageSeverity.INFO: f'{self.brand_info}➤{Styles.color_close}',
            MessageSeverity.WARNING: f'{self.brand_warning}➤{Styles.color_close}',
            MessageSeverity.ERROR: f'{self.brand_error}➤{Styles.color_close}',
            MessageSeverity.SUCCESS: f'{self.brand_success}➤{Styles.color_close}',
        }

    def writeln(
        self,
        message: Input,
        *,
        type_: MessageName = MessageName.PADDING,
        severity: MessageSeverity = MessageSeverity.INFO,
    ):
        output = self.format(message, type_, severity) + EOL
        stream = (
            self.stderr
            if severity is MessageSeverity.ERROR
            else self.stdout
        )

        return stream.write(output)

    def write(
        self,
        message: Input,
        *,
        type_: MessageName = MessageName.PADDING,
        severity: MessageSeverity = MessageSeverity.INFO,
    ):
        output = self.format(message, type_, severity)
        return self.stdout.write(f'\r{output}{erase_end_line()}')

    def erase_line(self):
        self.stdout.write(erase_line())

    def erase_lines(self, count: int):
        self.stdout.write(erase_lines(count))

    def reset(self, text: Input) -> str:
        return f'{Styles.reset}{text}{Styles.reset}'

    def bold(self, text: Input) -> str:
        return f'{Styles.bold}{text}{Styles.reset}'

    def dim(self, text: Input) -> str:
        return f'{Styles.dim}{text}{Styles.reset}'

    def italic(self, text: Input) -> str:
        return f'{Styles.italic}{text}{Styles.reset}'

    def underline(self, text: Input) -> str:
        return f'{Styles.underline}{text}{Styles.reset}'

    def inverse(self, text: Input) -> str:
        return f'{Styles.inverse}{text}{Styles.reset}'

    def hidden(self, text: Input) -> str:
        return f'{Styles.hidden}{text}{Styles.reset}'

    def strikethrough(self, text: Input) -> str:
        return f'{Styles.strikethrough}{text}{Styles.reset}'

    def link(self, text: Input, url: str) -> str:
        return link(str(text), url)

    def color_info(self, text: Input) -> str:
        return f'{self.brand_info}{text}{Styles.color_close}'

    def color_warning(self, text: Input) -> str:
        return f'{self.brand_warning}{text}{Styles.color_close}'

    def color_error(self, text: Input) -> str:
        return f'{self.brand_error}{text}{Styles.color_close}'

    def color_success(self, text: Input) -> str:
        return f'{self.brand_success}{text}{Styles.color_close}'

    def format(
        self,
        message: Input,
        type_: MessageName,
        severity: MessageSeverity,
    ) -> str:
        prefix = self.make_prefix(type_)
        arrow = self.arrows[severity]
        return f'{arrow} {prefix}: {self.reset(message)}'

    def make_prefix(self, type_: MessageName) -> str:
        stringified = self.stringify_message_name(type_)

        if type_ is MessageName.PADDING:
            return self.dim(stringified)

        return link(stringified, f'https://google.com/#{int(type_)}')

    def stringify_message_name(self, name: MessageName) -> str:
        return f'{PREFIX}{int(name):04d}'
