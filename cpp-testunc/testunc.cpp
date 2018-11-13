#define UNICODE
#define DBGHELP_TRANSLATE_TCHAR
#include <windows.h>
#include <iostream>
#include <dbghelp.h>

using namespace std;

typedef BOOL (*PFN)(LPTSTR p1,PDWORD p2,LPTSTR p3,LPTSTR *p4);

int testFun()
{
	cout << "testFun" << endl;
	return 0;
}

int main()
{
	HINSTANCE hInst = LoadLibrary(L"linkinfo.dll");
	if (hInst)
	{
		BOOL bret;
		HANDLE hp = GetCurrentProcess();
		bret = SymInitialize(hp, NULL, true);
		if (bret)
		{
			SymSetOptions(SYMOPT_EXACT_SYMBOLS);
			ULONG64 buffer[(sizeof(SYMBOL_INFO) + MAX_SYM_NAME * sizeof(TCHAR) + sizeof(ULONG64) - 1) / sizeof(ULONG64)];
			PSYMBOL_INFO pSymbol = (PSYMBOL_INFO)buffer;
			// bret = SymFromName(hp, L"linkinfo!PathIsUNCEx", pSymbol);
			// bret = SymFromName(hp, L"testunc!testFun", pSymbol);
			bret = SymFromName(hp, L"linkinfo!GetRemotePathInfo", pSymbol);
			if (bret)
			{
				PFN fnPointer = (PFN)pSymbol->Address;
				if (fnPointer)
				{
					DWORD p2;
					(fnPointer)(L"\\\\testbox", &p2, NULL, NULL);
				}
			}
			else
			{
				DWORD ret = GetLastError();
				cout << "ret = " << ret << endl;
			}
		}
	}
	cout << "end" << endl;
	return 0;
}