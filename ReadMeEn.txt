Hosts Commander v1.6.2 [2020/11/21]
(С) 2010-2020 Evgeny Vrublevsky <me@veg.by>
http://veg.by/en/projects/hostscmd/

Command line hosts file editor.

Features:
 - Adding, removing, disabling, and hiding hosts
 - Viewing list of hosts filtered by a mask
 - Batch operations on hosts by a mask
 - Creation of backup hosts files and rolling back of previous changes
 - Supports IDN (allows you to work with Unicode domain names which are stored as xn-- in the hosts file)
 - Supports IPv6 addresses
 - Supports aliases (a few domain names in a line)
 - Preserves initial formatting of your hosts file
 - A lot of aliases for all the commands
 - Integrated command interpretator for close work with hosts
 - Works on Windows XP/7+ (.NET 4), Linux and Mac OS X (Mono 2.8+)

------------------------------------------------------------------------------------------------------------------------
  How to use it?
------------------------------------------------------------------------------------------------------------------------

On Windows, copy the hosts.exe to your system directory (c:\windows\system32\), so the tool will be always available from command prompt or from the Win+R dialog. To work on Windows XP or 7, it is required to install .NET Framework 4 (it is preinstalled on Windows 8 and 10). On Windows 7+, don't forget to run hosts.exe from a Command Prompt with administrator rights if you want to see the result of execution (otherwise it appears in an external window, and then just disappears). To open a Command Prompt with administrator rights, press Win, then type "cmd", and then press Ctrl+Shift+Enter.

On Linux or MacOS, you should use Mono (2.8+). Execution of the program using mono looks like this: mono hosts.exe command param1 param2. As a convenience, you can create alias hosts="mono hosts.exe", so you will be able to run Hosts Commander as easy as on Windows. Unlike Windows, the program doesn't require superuser rights to view contents of the hosts file.

hosts [shell]
 - Runs CLI for close work with hosts
 - Enter other commands without "hosts" prefix in this mode
 - Use "exit" or "quit" to exit from this CLI

hosts list [--all] [mask]
 - Displays enabled and not hidden hostnames by default
 - With "--all" flag, it displays disabled and hidden hosts also
 - Aliases: view, select, ls, show
 - Example: list vk
 - Example: list all *.local

hosts add <host> [aliases] [ipv4] [ipv6] # [comment]
 - Adds a new hostname, [ipv4] is 127.0.0.1 by default
 - Argument [aliases] is not mandatory
 - All arguments before "#" can be in any order
 - Everything after "#" is a comment
 - Aliases: new
 - Example: hosts add myhost.dev www.myhost.dev
 - Example: hosts add another.dev 192.168.1.1 # Remote host
 - Example: hosts add домен.рф # IDN host demo
 - Example: hosts add example.com 127.0.0.1 ::1

hosts upd <host|mask> [ipv4] [ipv6] # [comment]
 - Updates IP address and comment of a hostname
 - Everything after "#" is a comment
 - Aliases: update, change
 - Example: hosts upd myhost.dev # new comment
 - Example: hosts upd another.dev 192.168.1.1

hosts set <host|mask> [ipv4] [ipv6] # [comment]
 - Adds a new hostname or updates its IP address and comment
 - A mixture of "add" and "upd"
 - Everything after "#" is a comment
 - Example: hosts set myhost.dev # new comment
 - Example: hosts set another.dev 192.168.1.1

hosts del <host|mask>
 - Deletes hostnames
 - Aliases: rem, rm, remove, delete, unset
 - Example: hosts del *.local

hosts enable <host|mask>
 - Enables hostnames
 - Aliases: on
 - Example: hosts enable localhost

hosts disable <host|mask>
 - Disables hostnames
 - Aliases: off
 - Example: hosts disable local?ost

hosts hide <host|mask>
 - Hides hostnames from "hosts view"
 - It is useful when you have a lot of hostnames most of which are not important

hosts unhide <host|mask>
 - Unhides hostnames from "hosts view"

hosts print
 - Prints raw contents of the hosts file
 - Aliases: raw, file

hosts format
 - Formats all lines in the hosts file

hosts clean
 - Removes all comments and formats all lines in the hosts file

hosts backup [name]
 - Makes a backup copy of the hosts file
 - Argument [name] is not required, it is "backup" by default

hosts restore [name]
 - Restores hosts file from a backup copy
 - Argument [name] is not required, it is "backup" by default

hosts rollback
 - Rollbacks previous operation with hosts file

hosts reset
 - Replaces current hosts file with a new one with just one default host (localhost)
 - Aliases: empty, recreate, erase

hosts open
 - Opens hosts file in default text editor for *.txt files
 - The command is available on Windows only

------------------------------------------------------------------------------------------------------------------------
  Changelog
------------------------------------------------------------------------------------------------------------------------

v1.6.x [202x/xx/xx]
 - A non-zero error code is returned if an error occurs.
 - Command "show" is renamed to "unhide" to better reflect what it does.
 - Command "list" treats "show" as an alias.
 - Command "empty" is renamed to "reset".
 - Optional argument "shell" to run the hosts interactive shell.
 - Execute as a 64-bit process on 64-bit Windows.
 - Other cosmetic changes.

v1.6.2 [2020/11/21]
 - Fixed an error which may cause a freeze on a malformed hosts file.
 - The "list" command uses "--all" as an argument instead of "all", and it can be before and after mask.
 - More understandable messages from the "add" command when you are trying to add already existing hosts.
 - Unset Read Only flag from the hosts file before changing it.
 - New command aliases: "unset" for "rem", and "erase" for "empty".

v1.6.1 [2019/07/07]
 - Program is built for .NET Framework 4.0 (it is preinstalled on Windows 8 and 10)
 - Cosmetic changes

v1.6.0 [2013/10/26]
 - Command "add" removes all previous entries with the same name as the adding host
 - Command "upd" can update existing entries only
 - Command "set" can either update existing entries or add new ones
 - Commands "add", "set", and "upd" allow add or update IPv4 and IPv6 addresses simultaneously
 - IPv6 addresses are normalized, the most compact representation is used
 - The command "recreate" is renamed to "empty"
 - The command "view" has a new alias "ls"
 - Other insignificant changes

v1.5.1 [2011/11/19]
 - Solved a problem with reading commands in the command interpreter on Mono
 - The command "rem" has a new alias "rm"

v1.5.0 [2011/11/17]
 - Support of Linux and MacOS (using Mono)
 - Write right for the hosts directory is required only for commands which change the hosts file
 - An ability to rollback the "recreate" command

v1.4.1 [2011/10/24]
 - In the command interpreter, "hosts" prefix is ignored
 - "help" command is updated

v1.4.0 [2011/10/20]
 - Built-in command interpreter which is executed by running "hosts" without arguments
 - The command "hosts open" opens hosts file in the default text editor for *.txt files
 - The command "hosts view" is simplified, it displays just enabled and not hidden hosts by default

v1.3.0 [2011/02/06]
 - Support of IDN (you can create Unicode domains)
 - Support of IPv6
 - Support of domain aliases (a few domain names in a line)
 - A new command "rollback" to rollback previous command
 - "add" and "set" are different commands (adding and updating accordingly)
 - The "add" command guesses used order of arguments, everything after "#" is a comment
 - An ability to create several backups of the hosts file (you can set a name of a backup)

v1.2.0 [2010/12/03]
 - The program requires administrator rights for running
 - Preserves original character set of hosts (useful for non-English Windows)
 - An ability to create a standard hosts file (recreate)

v1.1.0 [2010/12/01]
 - Compatibility with .NET 3.5
 - Automatic backup of the hosts on first run
 - An ability to backup and restore the hosts file
 - An ability to open the hosts file in the notepad quickly

v1.0.0 [2010/11/30]
 - First version, was written using C# for .NET 4.0
 - Adding, removing, and disabling hosts operations
 - Preserves initial formatting of your hosts file
 - An ability to hide some hosts from standard view
 - Batch operations on hosts by a mask
 - Viewing list of hosts filtered by a mask
 - A lot of aliases for all the commands

v0.1.0 [2009/07/22]
 - A prototype using C++
 - Thought out the general concept of the program
 - Wasn't completed nor released

v0.0.0 [2009/01/28]
 - The idea of the program was written into todo list and was shelved immediately

------------------------------------------------------------------------------------------------------------------------
