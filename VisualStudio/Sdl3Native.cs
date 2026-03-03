using System.Runtime.InteropServices;
using System.Reflection;

namespace BtxDecoder;

internal static class Sdl3Native
{
    private static bool _resolverInitialized;

    public const uint SDL_INIT_VIDEO = 0x00000020;
    public const uint SDL_WINDOW_RESIZABLE = 0x00000020;

    public const uint SDL_QUIT = 0x100;
    public const uint SDL_KEYDOWN = 0x300;
    public const uint SDL_TEXTINPUT = 0x303;
    public const uint SDL_MOUSEMOTION = 0x400;
    public const uint SDL_MOUSEBUTTONDOWN = 0x401;
    public const uint SDL_MOUSEBUTTONUP = 0x402;

    public const uint SDL_PIXELFORMAT_ARGB8888 = 372645892;
    public const int SDL_TEXTUREACCESS_STATIC = 0;
    public const int SDL_LOGICAL_PRESENTATION_LETTERBOX = 2;

    public const int SDLK_RETURN = 13;
	public const int SDLK_KP_ENTER = 1073741912;
    public const int SDLK_BACKSPACE = 8;
    public const int SDLK_HOME = 1073741898;
    public const int SDLK_LEFT = 1073741904;
    public const int SDLK_RIGHT = 1073741903;
    public const int SDLK_UP = 1073741906;
    public const int SDLK_DOWN = 1073741905;
    public const int SDLK_F1 = 1073741882;
    public const int SDLK_F2 = 1073741883;
    public const int SDLK_F3 = 1073741884;
    public const int SDLK_F4 = 1073741885;
    public const int SDLK_F5 = 1073741886;
    public const int SDLK_F12 = 1073741893;

    public const ushort KMOD_NONE = 0x0000;
    public const ushort KMOD_SHIFT = 0x0003;
    public const ushort KMOD_ALT = 0x0300;

    public static void InitNativeResolver()
    {
        if (_resolverInitialized)
        {
            return;
        }

        NativeLibrary.SetDllImportResolver(
            Assembly.GetExecutingAssembly(),
            static (libraryName, assembly, searchPath) =>
            {
                if (!string.Equals(libraryName, "SDL3", StringComparison.Ordinal))
                {
                    return IntPtr.Zero;
                }

                string baseDir = AppContext.BaseDirectory;
                string[] candidates =
                {
                    Path.Combine(baseDir, "native", "win-x64", "SDL3.dll"),
                    Path.Combine(baseDir, "SDL3.dll"),
                    Path.Combine(baseDir, "native", "libSDL3.so"),
                    Path.Combine(baseDir, "native", "libSDL3.so.0"),
                    Path.Combine(baseDir, "libSDL3.so"),
                    Path.Combine(baseDir, "libSDL3.so.0"),
                };

                foreach (string candidate in candidates)
                {
                    if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out IntPtr handle))
                    {
                        return handle;
                    }
                }

                return IntPtr.Zero;
            });

        _resolverInitialized = true;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SDL_KeyboardEvent
    {
        public uint type;
        public uint reserved;
        public ulong timestamp;
        public uint windowID;
        public uint which;
        public uint scancode;
        public int key;
        public ushort mod;
        public ushort raw;
        [MarshalAs(UnmanagedType.I1)]
        public bool down;
        [MarshalAs(UnmanagedType.I1)]
        public bool repeat;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SDL_TextInputEvent
    {
        public uint type;
        public uint reserved;
        public ulong timestamp;
        public uint windowID;
        public IntPtr text;
    }

    [StructLayout(LayoutKind.Explicit, Size = 128)]
    internal struct SDL_Event
    {
        [FieldOffset(0)]
        public uint type;

        [FieldOffset(0)]
        public SDL_KeyboardEvent key;

        [FieldOffset(0)]
        public SDL_TextInputEvent text;
    }

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SDL_Init(uint flags);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_CreateWindow(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string title,
        int w,
        int h,
        uint flags);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_CreateRenderer(IntPtr window, [MarshalAs(UnmanagedType.LPUTF8Str)] string? name);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_CreateTexture(IntPtr renderer, uint format, int access, int w, int h);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_UpdateTexture(IntPtr texture, IntPtr rect, IntPtr pixels, int pitch);

    [DllImport("SDL3", EntryPoint = "SDL_UpdateTexture", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_UpdateTexture(IntPtr texture, IntPtr rect, [In] uint[] pixels, int pitch);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_RenderClear(IntPtr renderer);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SDL_SetRenderLogicalPresentation(IntPtr renderer, int w, int h, int mode);

    [DllImport("SDL3", EntryPoint = "SDL_RenderTexture", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SDL_RenderCopy(IntPtr renderer, IntPtr texture, IntPtr srcrect, IntPtr dstrect);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_RenderPresent(IntPtr renderer);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SDL_WaitEventTimeout(out SDL_Event sdlEvent, int timeout);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SDL_StartTextInput(IntPtr window);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SDL_StopTextInput(IntPtr window);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_GetKeyName(int key);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_DestroyTexture(IntPtr texture);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_DestroyRenderer(IntPtr renderer);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_DestroyWindow(IntPtr window);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_Quit();

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_GetError();

    public static string GetErrorString()
    {
        IntPtr ptr = SDL_GetError();
        return ptr == IntPtr.Zero ? "unknown SDL error" : Marshal.PtrToStringUTF8(ptr) ?? "unknown SDL error";
    }
}
