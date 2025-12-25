## DesktopShell
###### <em>Created in Visual Studio 2017</em>
============

This program serves as a replacement of desktop shortcuts and batch files, and an extension to the Windows start menu.

____

This is mostly meant as a code example since many of the features of this program launch other programs I coded which are not bundled in this repository. If you want to try it anyways, add your [regular expression](https://regexr.com/), program executable path, and commandline arguments in separate lines in the shortcuts.txt file. Add one line spacing between statements, and if no commandline arguments are required, put a dash on the third line. To use your new regex either re-open the DesktopShell.exe program or type the command "rescan".

***

### Example:

<p>
(^txt$){1}<br>
C:\notepad.exe<br>
"C:\Program Files (x86)\testfile.txt"<br>
C:\notepad.exe<br>
"C:\Program Files (x86)\testfile2.txt"<br>
<br>
(^img$){1}<br>
C:\paint.exe<br>
-</p>
