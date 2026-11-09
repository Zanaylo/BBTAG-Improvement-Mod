#define ENTRY_JUMP_SIZE 5
#define JUMP_NEAR 0xE9
#define LOADER_SPIN_FIRST 0xEB
#define LOADER_SPIN_SECOND 0xFE
#define PE_HEADER_FIELD 0x3C
#define ENTRY_POINT_FIELD 0x28
#define PAGE_EXECUTE_READWRITE 0x40
#define WAIT_FOREVER 0xFFFFFFFFu

typedef unsigned long Dword;
typedef unsigned char Byte;
typedef Dword(__stdcall* ThreadStart)(void*);

__declspec(dllimport) void* __stdcall GetModuleHandleW(const unsigned short* name);
__declspec(dllimport) void* __stdcall GetCurrentProcess(void);
__declspec(dllimport) void* __stdcall GetProcAddress(void* module, const char* name);
__declspec(dllimport) int __stdcall VirtualProtect(void* address, unsigned int size, Dword protection, Dword* previous);
__declspec(dllimport) int __stdcall FlushInstructionCache(void* process, const void* address, unsigned int size);
__declspec(dllimport) void* __stdcall CreateThread(void* security, unsigned int stack, ThreadStart start, void* argument, Dword flags, Dword* id);
__declspec(dllimport) Dword __stdcall WaitForSingleObject(void* handle, Dword milliseconds);
__declspec(dllimport) int __stdcall CloseHandle(void* handle);

extern Byte __ImageBase;

static Byte* g_gameEntry;
static Byte g_gameEntryBytes[ENTRY_JUMP_SIZE];

static void WriteGameEntry(const Byte* bytes)
{
    Dword previous;
    int index;

    if (!VirtualProtect(g_gameEntry, ENTRY_JUMP_SIZE, PAGE_EXECUTE_READWRITE, &previous))
        return;

    for (index = 0; index < ENTRY_JUMP_SIZE; index++)
        g_gameEntry[index] = bytes[index];

    VirtualProtect(g_gameEntry, ENTRY_JUMP_SIZE, previous, &previous);
    FlushInstructionCache(GetCurrentProcess(), g_gameEntry, ENTRY_JUMP_SIZE);
}

static void __cdecl StartModAndReleaseEntry(void)
{
    ThreadStart start;
    void* thread;

    WriteGameEntry(g_gameEntryBytes);
    start = (ThreadStart)GetProcAddress(&__ImageBase, "BBTAG_IM_StandaloneStart");

    if (start == 0)
        return;

    thread = CreateThread(0, 0, start, 0, 0, 0);

    if (thread == 0)
        return;

    WaitForSingleObject(thread, WAIT_FOREVER);
    CloseHandle(thread);
}

static __declspec(naked) void EntryGate(void)
{
    __asm {
        pushad
        call StartModAndReleaseEntry
        popad
        jmp dword ptr [g_gameEntry]
    }
}

static void __cdecl ArmEntryGate(void)
{
    Byte* game = (Byte*)GetModuleHandleW(0);
    Dword peHeader = *(Dword*)(game + PE_HEADER_FIELD);
    Byte jump[ENTRY_JUMP_SIZE];
    int index;

    g_gameEntry = game + *(Dword*)(game + peHeader + ENTRY_POINT_FIELD);

    if (g_gameEntry[0] == LOADER_SPIN_FIRST && g_gameEntry[1] == LOADER_SPIN_SECOND)
        return;

    for (index = 0; index < ENTRY_JUMP_SIZE; index++)
        g_gameEntryBytes[index] = g_gameEntry[index];

    jump[0] = JUMP_NEAR;
    *(Dword*)(jump + 1) = (Dword)((Byte*)EntryGate - (g_gameEntry + ENTRY_JUMP_SIZE));
    WriteGameEntry(jump);
}

#pragma section(".CRT$XCU", long, read)
__declspec(allocate(".CRT$XCU")) void(__cdecl* g_armEntryGate)(void) = ArmEntryGate;
