import subprocess
import unittest

import bbscript


class FakeRun:
    def __init__(self, returncode=0, stdout="", stderr=""):
        self.calls = []
        self.finished = subprocess.CompletedProcess([], returncode, stdout, stderr)

    def __call__(self, arguments, capture_output, text):
        self.calls.append(arguments)
        return self.finished


class ParseErrorsTests(unittest.TestCase):
    def test_compiler_line_becomes_an_error_with_its_line(self):
        errors = bbscript.parse_errors(r"D:\mods\DEV\scr_brg.py(5): Damag is not a known command." + "\n")

        self.assertEqual([bbscript.ScriptError(r"D:\mods\DEV\scr_brg.py", 5, "Damag is not a known command.")], errors)

    def test_other_lines_are_left_out(self):
        errors = bbscript.parse_errors("command_db.json was not found in X, so commands only have numbers.\n")

        self.assertEqual([], errors)


class BBScriptTests(unittest.TestCase):
    def test_check_passes_the_files_and_the_database(self):
        run = FakeRun(stdout="scr_brg.py: ok\n")
        tool = bbscript.BBScript(exe="bbscript.exe", database=r"C:\bbscript", run=run)

        result = tool.check("scr_brg.py")

        self.assertEqual([["bbscript.exe", "check", "scr_brg.py", "--db", r"C:\bbscript"]], run.calls)
        self.assertTrue(result.ok)

    def test_failed_compile_returns_the_errors(self):
        run = FakeRun(returncode=1, stderr="scr_brg.py(12): NmlAtk5X is not a state or subroutine in this file.\n")
        tool = bbscript.BBScript(exe="bbscript.exe", run=run)

        result = tool.compile("scr_brg.py", out="build")

        self.assertEqual(["bbscript.exe", "compile", "scr_brg.py", "--out", "build"], run.calls[0])
        self.assertFalse(result.ok)
        self.assertEqual(12, result.errors[0].line)

    def test_decompile_for_bbtools_adds_the_option(self):
        run = FakeRun()
        tool = bbscript.BBScript(exe="bbscript.exe", run=run)

        tool.decompile("char_brg_scr.pac", bbtools=True)

        self.assertEqual(["bbscript.exe", "decompile", "char_brg_scr.pac", "--bbtools"], run.calls[0])


if __name__ == "__main__":
    unittest.main()
