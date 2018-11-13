- The problem  can be reproduced on both win7 sp1 and win 10 box.
- Repro Steps
	1. Find one Win7 or win10 box which has the VS installed.
	1. Compile the attached testunc.cpp using the following command in the VS command line window: `cl /Zi testunc.cpp /link dbghelp.lib`
	1. Find the matched pdb symbol for the local c:\windows\system32\linkinfo.dll and copy it to the same folder as the testunc.exe.
	1. Run the command to set the `_NT_SYMBOL_PATH` in the command line. `set _NT_SYMBOL_PATH=srv*c:\<ptah_to_testunc.exe>*http://symweb`
	1. Run the below command to reproduce the issue in the same command line: `c:\<ptah_to_testunc.exe>\testunc.exe`
	1. You will see high cpu of testunc.exe and if u attach a debugger to the running testunc.exe, you will see the call stack like the dump