import os
import re
import subprocess
import sys
from dataclasses import dataclass

EXE_VARIABLE = "BBSCRIPT_EXE"
EXE_NAME = "bbscript.exe"
ERROR_LINE = re.compile(r"^(?P<path>.+)\((?P<line>\d+)\): (?P<message>.+)$")


@dataclass(frozen=True)
class ScriptError:
    path: str
    line: int
    message: str


@dataclass(frozen=True)
class Result:
    ok: bool
    errors: list
    output: str


def parse_errors(text):
    errors = []
    for line in text.splitlines():
        match = ERROR_LINE.match(line.strip())
        if match:
            errors.append(ScriptError(match["path"], int(match["line"]), match["message"]))
    return errors


def find_exe():
    if EXE_VARIABLE in os.environ:
        return os.environ[EXE_VARIABLE]
    beside = os.path.join(os.path.dirname(os.path.abspath(__file__)), EXE_NAME)
    return beside if os.path.isfile(beside) else EXE_NAME


class BBScript:
    def __init__(self, exe=None, database=None, run=subprocess.run):
        self.exe = exe or find_exe()
        self.database = database
        self.run = run

    def decompile(self, *inputs, out=None, bbtools=False):
        return self._call("decompile", inputs, out=out, bbtools=bbtools)

    def compile(self, *inputs, out=None):
        return self._call("compile", inputs, out=out)

    def check(self, *inputs):
        return self._call("check", inputs)

    def stubs(self, *inputs):
        return self._call("stubs", inputs)

    def import_docs(self, workbook):
        return self._call("import-docs", [workbook])

    def _call(self, verb, inputs, out=None, bbtools=False):
        arguments = [self.exe, verb, *[str(path) for path in inputs]]
        if out:
            arguments += ["--out", str(out)]
        if self.database:
            arguments += ["--db", str(self.database)]
        if bbtools:
            arguments.append("--bbtools")
        finished = self.run(arguments, capture_output=True, text=True)
        return Result(finished.returncode == 0, parse_errors(finished.stderr), finished.stdout + finished.stderr)


def main(arguments):
    if not arguments:
        print("python bbscript.py <decompile|compile|check|stubs|import-docs> <files>... [--out folder] [--db folder] [--bbtools]")
        return 2
    finished = subprocess.run([find_exe(), *arguments])
    return finished.returncode


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
