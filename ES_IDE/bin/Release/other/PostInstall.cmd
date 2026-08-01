MKDIR %APPDATA%\EventScriptIDE
MKDIR %APPDATA%\EventScriptIDE\Extensions
MKDIR %APPDATA%\EventScriptIDE\Projects
MKDIR %APPDATA%\EventScriptIDE\Extensions\Actions
MKDIR %APPDATA%\EventScriptIDE\Extensions\Conditions
MKDIR %APPDATA%\EventScriptIDE\Extensions\Imports
MKDIR %APPDATA%\EventScriptIDE\Extensions\Triggers
MOVE "C:\Program Files (x86)\RO INC\ES VPE\ico.ico" "%APPDATA%\EventScriptIDE\ico.ico"
MOVE "C:\Program Files\RO INC\ES VPE\ico.ico" "%APPDATA%\EventScriptIDE\ico.ico"
DEL "C:\Program Files (x86)\RO INC\ES VPE\ico.ico"
DEL "C:\Program Files\RO INC\ES VPE\ico.ico"
