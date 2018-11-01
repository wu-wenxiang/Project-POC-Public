#include <windows.h>
#include <dbghelp.h>
//#include <dbgeng.h>

typedef char    TW_STR32[34],     FAR *pTW_STR32;
typedef unsigned short TW_UINT16, FAR *pTW_UINT16;
typedef unsigned long  TW_UINT32, FAR *pTW_UINT32;
typedef struct {
   TW_UINT16  MajorNum;  /* Major revision number of the software. */
   TW_UINT16  MinorNum;  /* Incremental revision number of the software. */
   TW_UINT16  Language;  /* e.g. TWLG_SWISSFRENCH */
   TW_UINT16  Country;   /* e.g. TWCY_SWITZERLAND */
   TW_STR32   Info;      /* e.g. "1.0b3 Beta release" */
} TW_VERSION, FAR * pTW_VERSION;
typedef LPVOID         TW_MEMREF;
typedef struct {
   TW_UINT32  Id;              /* Unique number.  In Windows, app hWnd      */
   TW_VERSION Version;         /* Identifies the piece of code              */
   TW_UINT16  ProtocolMajor;   /* App and DS must set to TWON_PROTOCOLMAJOR */
   TW_UINT16  ProtocolMinor;   /* App and DS must set to TWON_PROTOCOLMINOR */
   TW_UINT32  SupportedGroups; /* Bit field OR combination of DG_ constants */
   TW_STR32   Manufacturer;    /* Manufacturer name, e.g. "Hewlett-Packard" */
   TW_STR32   ProductFamily;   /* Product family name, e.g. "ScanJet"       */
   TW_STR32   ProductName;     /* Product name, e.g. "ScanJet Plus"         */
} TW_IDENTITY, FAR * pTW_IDENTITY;

    TW_UINT16 FAR PASCAL DSM_Entry( pTW_IDENTITY pOrigin,
                                pTW_IDENTITY pDest,
                                TW_UINT32    DG,
                                TW_UINT16    DAT,
                                TW_UINT16    MSG,
                                TW_MEMREF    pData);

    typedef TW_UINT16 (FAR PASCAL *DSMENTRYPROC)(pTW_IDENTITY, pTW_IDENTITY,
                                                 TW_UINT32,    TW_UINT16,
                                                 TW_UINT16,    TW_MEMREF);
     
typedef BOOLEAN (__fastcall *LOGMSGPROC) (LPCSTR inMessageTypeStr,LPCSTR inMessageStr); 
        
DSMENTRYPROC m_DSMEntry;
LOGMSGPROC m_LogMsg;

TW_IDENTITY idsrc,iddst;


extern "C" __declspec( dllexport ) int loadtwain()
{

m_DSMEntry = (DSMENTRYPROC)GetProcAddress(LoadLibrary(TEXT("TWAIN_32.DLL")), "DSM_Entry");
HANDLE hp=GetCurrentProcess();
SymInitialize(hp,NULL,true);
SymSetOptions(SYMOPT_EXACT_SYMBOLS);
ULONG64 buffer[(sizeof(SYMBOL_INFO) + MAX_SYM_NAME * sizeof(TCHAR) + sizeof(ULONG64) - 1)/sizeof(ULONG64)];
PSYMBOL_INFO pSymbol = (PSYMBOL_INFO)buffer;
pSymbol->SizeOfStruct = sizeof(SYMBOL_INFO);
SymFromName(hp,TEXT("twain_32!LogWriteHandler"),pSymbol);
m_LogMsg=(LOGMSGPROC)pSymbol->Address;

/*
DebugCreate( __uuidof(IDebugClient), (void**)&g_Client );
g_Client->AttachProcess(0,GetCurrentProcessId(),DEBUG_ATTACH_NONINVASIVE|DEBUG_ATTACH_NONINVASIVE_ALLOW_PARTIAL);
g_Client->QueryInterface( __uuidof(IDebugSymbols), (void**)&g_Symbols );
g_Symbols->SetSymbolPath(TEXT("SRV*"));
//g_Symbols->Reload(TEXT("twain_32"));
g_Symbols->GetOffsetByName(TEXT("twain_32!LogWriteHandler"),(PULONG64)m_LogMsg);
*/
return 1;
}


extern "C" __declspec( dllexport ) int call_dsm_entry()
{

__try
{
(*m_DSMEntry)(NULL, NULL, 1, 2, 0x601, NULL);
}__except(GetExceptionCode()==0xc0000005?EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH)
{
return 0;
}
return 1;

}


extern "C" __declspec( dllexport ) int call_logmsg()
{
(*m_LogMsg)(TEXT("test1"),TEXT("test2"));
return 1;
}