using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;


namespace Balto
{
    #region Structs
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;

        public BLENDFUNCTION(byte alpha)
        {
            this.BlendOp = 0;
            this.BlendFlags = 0;
            this.SourceConstantAlpha = alpha;
            this.AlphaFormat = 1;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public bool fErase;
        public Rectangle rcPaint;
        public bool fRestore;
        public bool fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int x;
        public int y;

        public POINT(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public long left;
        public long top;
        public long right;
        public long bottom;

        public long Height()
        {
            return bottom - top;
        }

        public long Width()
        {
            return right - left;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SIZE
    {
        public int cx;
        public int cy;

        public SIZE(int x, int y)
        {
            this.cx = x;
            this.cy = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ULW
    {
        public POINT pptDst;          //r
        public SIZE psize;            //r
        public POINT pprSrc;          //r
        public BLENDFUNCTION pblend;  //r

        public ULW(Point pptDst, Size pSize)
        {
            this.pptDst = new POINT(pptDst.X, pptDst.Y);
            this.psize  = new SIZE(pSize.Width, pSize.Height);
            this.pprSrc = new POINT(0, 0);
            this.pblend = new BLENDFUNCTION(255);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWINFO
    {
        public uint cbSize;
        public RECT rcWindow;
        public RECT rcClient;
        public uint dwStyle;
        public uint dwExStyle;
        public uint dwWindowStatus;
        public uint cxWindowBorders;
        public uint cyWindowBorders;
        public ushort atomWindowType;
        public int wCreatorVersion;
    }
    #endregion

    internal class User
    {
        #region Window State Codes:
        public const uint WS_POPUP = 0x80000000;
        public const int WS_EX_TOPMOST = 0x8;
        public const int WS_EX_TOOLWINDOW = 0x80;
        public const int WS_EX_LAYERED = 0x80000;
        public const int WS_EX_TRANSPARENT = 0x20;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_APPWINDOW = 0x00040000;
        #endregion
        #region Get Window Codes:
        public const uint GW_HWNDNEXT = 2;
        public const uint GW_HWNDPREV = 3;
        #endregion
        #region Get Ancestor Codes:
        public const int GA_PARENT = 1;
        public const int GA_ROOT = 2;
        public const int GA_ROOTOWNER = 3;
        #endregion
        #region AW Codes:
        public const uint AW_HOR_POSITIVE = 0x1;
        public const uint AW_HOR_NEGATIVE = 0x2;
        public const uint AW_VER_POSITIVE = 0x4;
        public const uint AW_VER_NEGATIVE = 0x8;
        public const uint AW_CENTER       = 0x10;
        public const uint AW_HIDE         = 0x10000;
        public const uint AW_ACTIVATE     = 0x20000;
        public const uint AW_SLIDE        = 0x40000;
        public const uint AW_BLEND        = 0x80000;
        #endregion
        #region Hook Codes:
        public const int  WH_KEYBOARD_LL  = 13; // The Low-Level Keyboard Hook
        public const int  WH_MOUSE_LL     = 14; // The Low-Level Mouse Hook
        #endregion
        #region Key State Codes:
        public const int WM_KEYDOWN    = 0x0100; // The wParam for Key-Down Action.
        public const int WM_KEYUP      = 0x0101; // The wParam for Key-Up Action.
        public const int WM_SYSKEYDOWN = 0x0104; // The wParam for System Key-Down (ALT Modified)
        public const int WM_SYSKEYUP   = 0x0105; // The wParam for System Key-Up   (ALT Modified)
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_RBUTTONDOWN = 0x0204;
        #endregion
        #region Show Window Codes:
        public const int SW_HIDE            = 0; // Hides the window and activates another window.
        public const int SW_SHOWNORMAL      = 1; // Activates and Displays in Orignal Size (never Max or Min)
        public const int SW_SHOWMINIMIZED   = 2; // Activates the window and displays it as a minimized window.
        public const int SW_SHOWMAXIMIZED   = 3; // Activates the window and displays it as a maximized window.
        public const int SW_SHOWNOACTIVATE  = 4; // Like 1, without activation
        public const int SW_SHOW            = 5; // Activates the window and displays it in its current size and position.
        public const int SW_MINIMIZE        = 6; // Minimizes, and if needed, activates another
        public const int SW_SHOWMINNOACTIVE = 7; // Like 2, without activation
        public const int SW_SHOWNA          = 8; // Like 5, without activation
        public const int SW_RESTORE         = 9; // Activates and Displays from Minimized State.
        public const int SW_SHOWDEFAULT     = 10;// Activates to its default position and size
        public const int SW_FORCEMINIMIZE   = 11;// Minimizes even if thread is not responding.
        #endregion
        #region Set Window Position Codes:
        public const int SWP_ASYNCWINDOWPOS = 0x4000;
        public const int SWP_DEFERERASE = 0x2000;
        public const int SWP_DRAWFRAME = 0x0020;
        public const int SWP_FRAMECHANGED = 0x0020;
        public const int SWP_HIDEWINDOW = 0x0080;
        public const int SWP_NOACTIVATE = 0x0010;
        public const int SWP_NOCOPYBITS = 0x0100;
        public const int SWP_NOMOVE = 0x0002;
        public const int SWP_NOOWNERZORDER = 0x0200;
        public const int SWP_NOREDRAW = 0x0008;
        public const int SWP_NOREPOSITION = 0x0200;
        public const int SWP_NOSENDCHANGING = 0x0400;
        public const int SWP_NOSIZE = 0x0001;
        public const int SWP_NOZORDER = 0x0004;
        public const int SWP_SHOWWINDOW = 0x0040;

        public static IntPtr HWND_BOTTOM = new IntPtr(1);
        public static IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public static IntPtr HWND_TOP = new IntPtr(0);
        public static IntPtr HWND_TOPMOST = new IntPtr(-1);
        #endregion
        #region Message Beep Codes:
        public const uint MB_SIMPLE       = 0xFFFFFFFF;
        public const uint MB_OK           = 0x00000000;
        public const uint MB_ICONSTOP     = 0x00000010;
        public const uint MB_ICONQUESTION = 0x00000020;
        public const uint MB_ICONWARNING  = 0x00000030;
        public const uint MB_ICONASTERISK = 0x00000040;
        #endregion
        public delegate IntPtr LowLevelKeyboardDelegate(int nCode, IntPtr wParam, IntPtr IParam);
        public delegate bool EnumWindowsDelegate(IntPtr hwnd, ref ArrayList lparam);

        [DllImport("User32.dll")]
        internal static extern bool   AllowSetForegroundWindow  (int pid);
        [DllImport("User32.dll")]
        internal static extern IntPtr BeginPaint                (IntPtr hwnd, ref PAINTSTRUCT ps);
        [DllImport("User32.dll")]
        internal static extern bool   BlockInput                (bool fBlockIt);
        [DllImport("User32.dll")]
        internal static extern bool   BringWindowToTop          (IntPtr hwnd);
        [DllImport("User32.dll")]
        internal static extern IntPtr CallNextHookEx            (IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("User32.dll")]
        internal static extern bool   CloseWindow               (IntPtr hwnd);
        [DllImport("User32.dll")]
        internal static extern bool   DestroyWindow             (IntPtr hwnd);
        [DllImport("User32.dll")]
        internal static extern bool   EnableWindow              (IntPtr hwnd, bool nAble);
        [DllImport("User32.dll")]
        internal static extern bool   EndPaint                  (IntPtr hwnd, ref PAINTSTRUCT ps);
        [DllImport("User32.dll")]
        internal static extern bool   EnumWindows               (EnumWindowsDelegate hwnd, ref ArrayList lparam);
        [DllImport("User32.dll")]
        internal static extern IntPtr GetActiveWindow           ();
        [DllImport("User32.dll")]
        internal static extern IntPtr GetAncestor               (IntPtr hwnd, uint gaFlags);
        [DllImport("User32.dll")]
        internal static extern int    GetClassName              (IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("User32.dll")]
        internal static extern IntPtr GetDC                     (IntPtr hWnd);
        [DllImport("User32.dll")]
        internal static extern IntPtr GetForegroundWindow       ();
        [DllImport("User32.dll")]
        internal static extern short  GetKeyState               (int vKey);
        [DllImport("User32.dll")]
        internal static extern IntPtr GetLastActivePopup        (IntPtr hwnd);
        [DllImport("User32.dll")]
        internal static extern UInt32 GetLastError              ();
        [DllImport("User32.dll")]
        internal static extern IntPtr GetNextWindow             (IntPtr hwnd, uint wcmd);
        [DllImport("User32.dll")]
        internal static extern IntPtr GetShellWindow            ();
        [DllImport("User32.dll")]
        internal static extern IntPtr GetTopWindow              (IntPtr hwnd);
        [DllImport("User32.dll")]
        internal static extern bool   GetWindowInfo             (IntPtr hwnd, ref WINDOWINFO wi);
        [DllImport("User32.dll")]
        internal static extern uint   GetWindowModuleFileName   (IntPtr hwnd, ref char[] buffer, uint cchFileNameMax);
        [DllImport("User32.dll")]
        internal static extern int    GetWindowText             (IntPtr hwnd, StringBuilder lpString, int nMaxCount);
        [DllImport("User32.dll")]
        internal static extern uint   GetWindowThreadProcessId  (IntPtr hwnd, ref int procIDbuffer);
        [DllImport("User32.dll")]
        internal static extern bool   IsWindow                  (IntPtr hwnd);
        [DllImport("User32.dll")]
        internal static extern bool   IsWindowEnabled           (IntPtr hwnd);
        [DllImport("User32.dll")]
        internal static extern bool   IsWindowVisible           (IntPtr hwnd);
        [DllImport("User32.dll")]
        internal static extern bool   MessageBeep               (uint uType);
        [DllImport("User32.dll")]
        internal static extern bool   OpenIcon                  (IntPtr hWnd);
        [DllImport("User32.dll")]
        internal static extern bool   RedrawWindow              (IntPtr hwnd, Rectangle uRec, IntPtr uReg, uint flags);
        [DllImport("User32.dll")]
        internal static extern int    ReleaseDC                 (IntPtr hWnd, IntPtr hDC);
        [DllImport("User32.dll")]
        internal static extern IntPtr SetActiveWindow           (IntPtr hWnd);
        [DllImport("User32.dll")]
        internal static extern bool   SetForegroundWindow       (IntPtr hWnd);
        [DllImport("User32.dll")]
        internal static extern bool   SetWindowPos              (IntPtr hwnd, IntPtr hwndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("User32.dll")]
        internal static extern IntPtr SetWindowsHookEx          (int idHook, LowLevelKeyboardDelegate lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("User32.dll")]
        internal static extern bool   ShowWindow                (IntPtr hWnd, int nCmdShow);
        [DllImport("User32.dll")]
        internal static extern void   SwitchToThisWindow        (IntPtr hWnd, bool fAltTab);
        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool   UnhookWindowsHookEx       (IntPtr hhk);
        [DllImport("User32.dll")]
        internal static extern bool   UpdateLayeredWindow       (IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pprSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
    }

    internal class Gdi
    {
        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        internal static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr DeleteObject(IntPtr hObject);
    }

    internal class Kernel
    {
        #region OpenProcess Acess Codes:
        public const uint PROCESS_VM_READ = 0x0010;
        public const uint PROCESS_QUERY_INFORMATION = 0x0400;
        #endregion
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetConsoleWindow();
        [DllImport("Kernel32.dll")]
        internal static extern int GetCurrentProcessId();
        [DllImport("Kernel32.dll")]
        internal static extern IntPtr OpenProcess(uint Aces, bool InheritHandle, uint procId);
    }

    internal class Custom
    {
        //Paint Methods with automatically saved PAINTSTRUCT.
        internal static Dictionary<IntPtr, PAINTSTRUCT> ptrstr = new Dictionary<IntPtr, PAINTSTRUCT>();
        public static IntPtr BeginPaint(IntPtr hwnd)
        {
            //TODO add an if already active clause
            PAINTSTRUCT ps = new PAINTSTRUCT();
            ptrstr.Add(hwnd, ps);
            return User.BeginPaint(hwnd, ref ps);
        }

        internal static bool EndPaint(IntPtr hwnd)
        {
            PAINTSTRUCT temp = ptrstr[hwnd];
            ptrstr.Remove(hwnd);
            return User.EndPaint(hwnd, ref temp);
        }

        /// <summary> Repeat(int, Action)
        /// Repeats the given function, the given number of times
        /// </summary>
        /// <param name="times">the number of times to repeat the function</param>
        /// <param name="func">the function to be repeated</param>
        internal static void Repeat(int times, Action func)
        {
            for (int i = 0; i < times; i++)
            {
                func();
            }
        }

        /// <summary> ConsoleLine(ConsoleColor, string)
        /// Writes the given string to it's own line
        /// of the console in the given color
        /// </summary>
        /// <param name="color">The color of the text</param>
        /// <param name="line">the string to write to the console</param>
        internal static void ConsoleLine(ConsoleColor color, string line)
        {
            ConsoleColor defColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            ConsoleLine(line);
            Console.ForegroundColor = defColor;
        }

        /// <summary> ConsoleLine(ConsoleColor, string, ConsoleColor)
        /// Writes the given string in the first given color
        /// on its own line, with the second given color as the background
        /// </summary>
        /// <param name="fColor">the color of the text</param>
        /// <param name="line">the string to write to the console</param>
        /// <param name="bgColor">the color for the background</param>
        internal static void ConsoleLine(ConsoleColor fColor, string line, ConsoleColor bgColor)
        {
            ConsoleColor defColor = Console.ForegroundColor;
            Console.ForegroundColor = fColor;
            ConsoleLine(line, bgColor);
            Console.ForegroundColor = defColor;
        }

        /// <summary> ConsoleLine(string, ConsoleColor)
        /// Writes the given string to the console
        /// on it's own line, with the given background color.
        /// </summary>
        /// <param name="line">The string to write to the console</param>
        /// <param name="color">The Background color</param>
        internal static void ConsoleLine(string line, ConsoleColor color = ConsoleColor.Black)
        {
            ConsoleColor defColor = Console.BackgroundColor;
            Console.BackgroundColor = color;
            Console.Write(line);
            Console.BackgroundColor = defColor;
            Console.Write("\n");
        }

        /// <summary> ConsoleLine(string)
        /// Writes the given string to its own line on the console
        /// </summary>
        /// <param name="line">The string to write to the console</param>
        internal static void ConsoleLine(string line)
        {
            Console.Write(line + "\n");
        }
    }

    internal static class Extensions
    {
        public static string Reverse(this string str)
        {
            char[] strArray = str.ToCharArray();
            Array.Reverse(strArray);
            return new string(strArray);
        }
    }
}
