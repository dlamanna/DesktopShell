DesktopShell
============

Created in Visual Studio 2017

This program serves as a replacement of desktop shortcuts and batch files, and an extension to the Windows start menu.



This is mostly meant as a code example since many of the features of this program launch other programs I coded which are not bundled in this repository. If you want to try it anyways, add your regular expression, program executable path, and arguments in separate lines in the shortcuts.txt file.

Example:
(^txt$){1}
C:\notepad.exe
"C:\Program Files (x86)\testfile.txt"