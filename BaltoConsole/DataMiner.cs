using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Security.Cryptography;

namespace Balto
{
    static class DataMiner
    {
        #region Inclusions/Exclusions
        private static string[] classExclusions = 
            new string[] /*Class Names not allowed in listings*/
            {   
                "Shell_TrayWnd", 
                "DV2ControlHost",
                "MsgrIMEWindowClass",
                "SysShadow",
                "WMP9MediaBarFlyout",
                "Button"
            };
        private static string[] classInclusions = new string[0];
        private static Dictionary<string, string> QualifiedClassExclusions = new Dictionary<string,string>()
            {{"Button" , "Start"}};
        private static string[] titleExclusions = new string[1]
            {""};
        private static string[] titleInclusions = new string[0];

        /// <summary> Exclusions(WinInfo[])
        /// Returns the given list without windows in the exception lists
        /// </summary>
        /// <param name="fullArray">The Array to be Cleansed!</param>
        /// <returns></returns>
        internal static APPDAT[] Exclusions(APPDAT[] fullArray)
        {
            ArrayList tempList = new ArrayList();
            tempList.AddRange(fullArray);
            foreach (APPDAT wi in fullArray)
            {
                if (titleExclusions.Contains(wi.windowText) || //If Excluded for Title, OR
                    (classExclusions.Contains(wi.className) && //If Excluded for Class AND
                        (!QualifiedClassExclusions.ContainsKey(wi.className) || //Qualification NOT needed OR
                            QualifiedClassExclusions[wi.className] == wi.windowText)) || //Qualification found, OR
                    wi.handle == User.GetShellWindow()) tempList.Remove(wi); //If Desktop. REMOVE
            }
            return (APPDAT[])tempList.ToArray(typeof(APPDAT));
        }

        #endregion

        private static bool testMode = false;
        private static User.EnumWindowsDelegate PWDelegate = GetWindowsCallback;
        

        #region Window Collection

        /// <summary> GetWindows()
        /// Collects data on the prominent windows
        /// </summary>
        /// <returns>An array of WindowInfo for each prominent window</returns>
        internal static APPDAT[] GetWindows()
        {
            ConsoleLine("Getting Windows:", ConsoleColor.White);
            ArrayList rtrn = new ArrayList();
            User.EnumWindows(PWDelegate, ref rtrn);
            return (APPDAT[])rtrn.ToArray(typeof(APPDAT));
        }

        /// <summary> GetWindowsCallback(IntPtr, ref ArrayList)
        /// The Hook used by EnumWindows in GetWindows,
        /// Adds prominent Windows to the array
        /// which GetWindows returns
        /// </summary>
        /// <param name="hndl">The Handle being handled this instance</param>
        /// <param name="rtrn">The ArrayList that will become GetWindows return-Array</param>
        /// <returns></returns>
        internal static bool      GetWindowsCallback(IntPtr hndl, ref ArrayList rtrn)
        {
            if (IsProminentWindow(hndl))
            {
                APPDAT PromApp = new APPDAT(hndl);
                rtrn.Add(PromApp);
            }
            return true;
        }

        /// <summary> IsProminent(IntPtr)
        /// The Test used to determine if a Window is "Prominent"
        /// </summary>
        /// <param name="hndl">Handle to the Window being tested</param>
        /// <returns>true iff this is it's ancestors most recently visible child</returns>
        private static bool       IsProminentWindow(IntPtr hndl)
        {
            IntPtr root = User.GetAncestor(hndl, User.GA_ROOTOWNER);
            return GetLastVisibleActivePopUp(root) == hndl;
        }

        /// <summary> GetLastVisibleActivePopUp(IntPtr)
        /// Recursively determines the most recently Active of the Children,
        /// of the Window, of the given handle.
        /// </summary>
        /// <param name="hndl">The handle to the window who shall have their children inspected</param>
        /// <returns>The handle to the window from the given-handle's window's family:
        /// who has most recently been on the news, but isn't currently hiding in Rehab</returns>
        private static IntPtr     GetLastVisibleActivePopUp(IntPtr hndl)
        {
            IntPtr lastAPU = User.GetLastActivePopup(hndl);
            if (User.IsWindowVisible(lastAPU))
                return lastAPU;
            else if (lastAPU == hndl)
                return IntPtr.Zero;
            else
                return GetLastVisibleActivePopUp(lastAPU);
        }

        #endregion

        #region Testing/Utility

        /// <summary> ReadList(WinInfo[])
        /// Prints the WindowText of each Window in the list to the console
        /// </summary>
        /// <param name="list">The Array to Read</param>
        internal static void ReadList(APPDAT[] list)
        {
            foreach (APPDAT wi in list)
            {
                if (!(wi.windowText == ""))
                    ConsoleLine(wi.windowText, ConsoleColor.Red);
            }
        }

        /// <summary> ConsoleLine(string, ConsoleColor):
        /// Prints the given string to the Console, with the given color as the background.
        /// Used for testing.
        /// </summary>
        /// <param name="txt">The text to be printed</param>
        /// <param name="clr">The BG color for this line</param>
        private static void ConsoleLine(string txt, ConsoleColor clr)
        {
            if (!testMode) return;
            string[] lineBreaks = new string[] { "\r\n", "\n" };
            string[] lines = txt.Split(lineBreaks, StringSplitOptions.None);
            foreach (string line in lines) Custom.ConsoleLine(line, clr);
        }

        /// <summary> ConsoleLine(string):
        /// Prints the given string to the Console.
        /// Used for testing
        /// </summary>
        /// <param name="txt">The text to be printed</param>
        private static void ConsoleLine(string txt)
        {
            ConsoleColor defColor = Console.BackgroundColor;
            ConsoleLine(txt, defColor);
        }
        #endregion

    }

    internal struct APPDAT : IEquatable<APPDAT>
    {
        private static Color[] ArCol = new Color[] { Color.Transparent, Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue, Color.Indigo, Color.Violet };
        private static Dictionary<string, ArrayList> IconMap;

        public IntPtr handle;
        public string source;
        public Icon icon;
        public Color iconBG;
        public string className;
        public string windowText;
        public WINDOWINFO detail;
        public bool valid;

        /// <summary> Constructor
        /// Creates a new APPDAT for the given handle
        /// </summary>
        /// <param name="hndl">the window handle used to create the structure</param>
        public APPDAT(IntPtr hndl)
        {
            if (IconMap == null) IconMap = new Dictionary<string, ArrayList>();

            WINDOWINFO wi = new WINDOWINFO();
            wi.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINDOWINFO));
            User.GetWindowInfo(hndl, ref wi);

            StringBuilder cnBuffer = new StringBuilder(256);
            User.GetClassName(hndl, cnBuffer, cnBuffer.Capacity);
            
            StringBuilder wtBuffer = new StringBuilder(256);
            User.GetWindowText(hndl, wtBuffer, wtBuffer.Capacity);

            int procID = 0;
            User.GetWindowThreadProcessId(hndl, ref procID);
            Process proc = Process.GetProcessById(procID);

            this.handle = hndl;
            this.source = proc.MainModule.FileName;

            if (IconMap.ContainsKey(this.source))
            {
                ArrayList tempList = IconMap[this.source];
                if (tempList.Contains(this.handle)) this.iconBG = ArCol[tempList.IndexOf(this.handle) % ArCol.Length];
                else
                {
                    this.iconBG = ArCol[tempList.Count % ArCol.Length];
                    IconMap[this.source].Add(this.handle);
                }
            }
            else
            {
                IconMap.Add(this.source, new ArrayList(new IntPtr[]{ this.handle }));
                iconBG = ArCol[0];
            }

            this.icon =  Icon.ExtractAssociatedIcon(this.source);

            this.className = cnBuffer.ToString();
            this.windowText = wtBuffer.ToString();
            this.detail = wi;
            this.valid = true;
        }

        /// <summary> ValidOnly(APPDAT[])
        /// Static Method Used to sort the invalids out of an array
        /// </summary>
        /// <param name="full">The list to be purged</param>
        /// <returns></returns>
        public static APPDAT[] ValidOnly(APPDAT[] full)
        {
            ArrayList rtrn = new ArrayList();
            foreach (APPDAT app in full) if (app.valid) rtrn.Add(app);
            return (APPDAT[])rtrn.ToArray(typeof(APPDAT));
        }

        /// <summary> validate(bool)
        /// Sets the valid field to the given value.
        /// </summary>
        /// <param name="that">The given validity</param>
        public void validate(bool isValid)
        {
            this.valid = isValid;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(APPDAT))
            {
                return this.Equals((APPDAT)obj);
            }
            return false;
        }

        public bool Equals(APPDAT that)
        {
            return this.handle == that.handle && this.className == that.className;
        }

        public override int GetHashCode()
        {
            return (this.handle.ToString() + this.className + this.handle.ToString()).GetHashCode();
        }
    }
}

