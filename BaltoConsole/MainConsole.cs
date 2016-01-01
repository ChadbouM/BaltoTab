using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

///BaltoTab: A Background Alt-Tab replacement
/// that allows you to group applications &
/// manage the Desktop quickly using shortcuts
///Version: 1.0.0
///By: Michael Chadbourne
namespace Balto
{
    /// <summary> MainConsole
    /// The rather static Console, which acts as the Functional
    /// Core of the program and containing the Main() method. 
    /// </summary>
    public class MainConsole
    {
        #region Attributes
        private static bool        visible;    //True if the console is currently displayed
        private static bool        safteyOn;   //Saftey which acts to stop Selecting from the open-key-press.
        private static IntPtr      mHandle;    //Handle to the Main Module
        private static int         pHandle;    //Handle to Process ID (unused)
        private static IntPtr      cHandle;    //Handle to the Console Window
        private static IntPtr      hHandle;    //Handle to the Hook Procedure
        private static IntPtr      mhHandle;   //Handle to the Mouse Hook

        private static FourPoint   swapSave;   //position of app being swapped.
        private static FourPoint   position;   //position of GUI cursor

        private static Deck[] deckApps;
        //private static APPDAT[][]  deckApps;   //An Array of Decks, which are each an Array of APPDAT.
        private static APPDAT[]    freeApps;   //An Array of APPDATs, representing the free-apps
        private static BaltoGUI    myWindow;   //The GUI window.

        private static BaltoState  state;      //enum representation of state.
        #region States:
        private enum BaltoState
        {
            Starting,
            Displayed,
            Hidden,
            Swapping,
            Locked,
            Closing,
            Test
        }
        #endregion
        private static Binds       bind;       //enum representation of the KeyBindings
        #region Binds:
        private enum Binds
        {
            ControlFreak,
            SideWinder,
            OddBod
        }
        private enum Commands
        {
            OpenGUI,
            CloseGUI,
            Left,
            Right,
            Up,
            Down,
            Select,
            Swap,
            Report,
            OpenCnsl,
            CloseCnsl
        }

        private static Dictionary<Binds, Dictionary<Commands, Keys[]>> KeyConfigs = new Dictionary<Binds,Dictionary<Commands,Keys[]>>();

        //Binding Dictionaries, Each represents a possible key binding set.
        private static Dictionary<Commands, Keys[]> CFBindings                  = new Dictionary<Commands,Keys[]>();
        private static Dictionary<Commands, Keys[]> SWBindings                  = new Dictionary<Commands,Keys[]>();
        private static Dictionary<Commands, Keys[]> OBBindings                  = new Dictionary<Commands,Keys[]>();

        /// <summary>
        /// Assigns the bindings to Dictionaries so they can be called like this:
        /// KeyBindings[ControlFreak][Select] returns the list of bindings for Select in Control Freak
        /// </summary>
        private static void CreateBindingMaps()
        {
            //Control-Freak Bindings
            CFBindings.Add(Commands.OpenGUI,   new[] { Keys.Q });
            CFBindings.Add(Commands.CloseGUI,  new[] { Keys.Escape });
            CFBindings.Add(Commands.Left,      new[] { Keys.A, Keys.Left });
            CFBindings.Add(Commands.Right,     new[] { Keys.D, Keys.Right, Keys.Tab });
            CFBindings.Add(Commands.Up,        new[] { Keys.W, Keys.Up });
            CFBindings.Add(Commands.Down,      new[] { Keys.S, Keys.Down });
            CFBindings.Add(Commands.Select,    new[] { Keys.Q, Keys.Return });
            CFBindings.Add(Commands.Swap,      new[] { Keys.E });
            CFBindings.Add(Commands.Report,    new[] { Keys.R });
            CFBindings.Add(Commands.OpenCnsl,  new[] { Keys.O });
            CFBindings.Add(Commands.CloseCnsl, new[] { Keys.P });

            //Side-Winder Bindings
            SWBindings.Add(Commands.OpenGUI,   new[] { Keys.Q });
            SWBindings.Add(Commands.CloseGUI,  new[] { Keys.Escape });
            SWBindings.Add(Commands.Left,      new[] { Keys.Left });
            SWBindings.Add(Commands.Right,     new[] { Keys.D, Keys.Right });
            SWBindings.Add(Commands.Up,        new[] { Keys.Up });
            SWBindings.Add(Commands.Down,      new[] { Keys.A, Keys.Down });
            SWBindings.Add(Commands.Select,    new[] { Keys.Q, Keys.Return });
            SWBindings.Add(Commands.Swap,      new[] { Keys.W });
            SWBindings.Add(Commands.Report,    new[] { Keys.R });
            SWBindings.Add(Commands.OpenCnsl,  new[] { Keys.O });
            SWBindings.Add(Commands.CloseCnsl, new[] { Keys.P });

            //Odd-Bod Bindings
            OBBindings.Add(Commands.OpenGUI,   new[] { Keys.Q });
            OBBindings.Add(Commands.CloseGUI,  new[] { Keys.Escape });
            OBBindings.Add(Commands.Left,      new[] { Keys.Tab, Keys.Left });
            OBBindings.Add(Commands.Right,     new[] { Keys.W, Keys.Right });
            OBBindings.Add(Commands.Up,        new[] { Keys.D1, Keys.Up });
            OBBindings.Add(Commands.Down,      new[] { Keys.A, Keys.Down });
            OBBindings.Add(Commands.Select,    new[] { Keys.Q, Keys.Return });
            OBBindings.Add(Commands.Swap,      new[] { Keys.Oemtilde });
            OBBindings.Add(Commands.Report,    new[] { Keys.R });
            OBBindings.Add(Commands.OpenCnsl,  new[] { Keys.O });
            OBBindings.Add(Commands.CloseCnsl, new[] { Keys.P });
            
            KeyConfigs.Add(Binds.ControlFreak, CFBindings);
            KeyConfigs.Add(Binds.SideWinder,   SWBindings);
            KeyConfigs.Add(Binds.OddBod,       OBBindings);
        }
        #endregion
        private static User.LowLevelKeyboardDelegate hDelegate; //The Hook Delegate

        /// <summary> SetValues()
        /// Sets the Values of all Attributes to there Start-Up value
        /// </summary>
        /// <returns>true</returns>
        private static bool SetValues()
        {
            cHandle   = Kernel.GetConsoleWindow();
            visible   = true;
            Hide();

            Process me = Process.GetCurrentProcess();

            safteyOn  = false; 
            mHandle   = Kernel.GetModuleHandle(me.MainModule.ModuleName);
            pHandle   = me.Id;

            hHandle   = IntPtr.Zero;
            mhHandle  = IntPtr.Zero;
            position  = new FourPoint(0, 0, 0, 0);

            state     = BaltoState.Starting;
            swapSave  = new FourPoint(false);
            bind      = Binds.ControlFreak;
            hDelegate = HookCallback;

            deckApps  = new Deck[0];
            freeApps  = new APPDAT[0];
            myWindow  = new BaltoGUI();

            myWindow.Create();
            UpdateData();
            Update();

            myWindow.Hide();
            return true;
        }
        #endregion

        /// <summary> Main(string[])
        /// The Main Method:
        /// Hides the console running BaltoTab,
        /// Sets the Keyboard hook used to Detect Key press
        /// Starts the Application "Messaging Loop"
        /// Upon Loop termination, Removes the hook and exits.
        /// </summary>
        /// <param name="args">The Command line arguments</param>
        static int Main(string[] args)
        {
            StartUp();
            RemoveHook();
            return 0;
        }

        #region Methodology

        /// <summary> StartUp()
        /// Performs the startup actions such as creating the GUI window.
        /// </summary>
        /// <returns>true if startup is successful</returns>
        public static bool StartUp()
        {
            Custom.ConsoleLine("Starting Up", ConsoleColor.White);
            CreateBindingMaps();
            SetValues();
            SetHook();
            state = BaltoState.Hidden;
            Custom.ConsoleLine("Start Up Complete");
            System.Windows.Forms.Application.Run();
            return false;
        }

        #region Console\Background-Controls:

        /// <summary> Show()
        /// The Show Command, used to reveal the console.
        /// </summary>
        /// <returns>Returns true if the Console is made Visible</returns>
        private static bool Show()
        {
            Custom.ConsoleLine("Show Called", ConsoleColor.White);
            if (!visible)
            {
                User.ShowWindow(cHandle, User.SW_SHOW);
                visible = true;
                Custom.ConsoleLine("Show Complete");
                return true;
            }
            else
            {
                Custom.ConsoleLine("Show Failed", ConsoleColor.Red);
                return false;
            }
        }

        /// <summary> Hide()
        /// Hides the Console Window
        /// </summary>
        /// <returns>true, iff the console was made invisible</returns>
        private static bool Hide()
        {
            Custom.ConsoleLine("Hide Called", ConsoleColor.White);
            if (visible)
            {
                User.ShowWindow(cHandle, User.SW_HIDE);
                visible = false;
                Custom.ConsoleLine("Hide Complete");
                return true;
            }
            else
            {
                Custom.ConsoleLine("Hide Failed", ConsoleColor.Red);
                return false;
            }
        }

        /// <summary> ShutDown()
        /// Shuts the Application down
        /// </summary>
        public static void ShutDown()
        {
            Custom.ConsoleLine("ShutDown Called", ConsoleColor.White);
            Close();
            Report();
            System.Windows.Forms.Application.Exit();
            Custom.ConsoleLine("ShutDown Complete");
        }

        /// <summary> SetHook()
        /// Places the Low Level Keyboard Hook.
        /// Using the Delegate made from Callback
        /// The Main-Module Handle
        /// and the All-Threads(on This Desktop) Option
        /// </summary>
        /// <returns>true if the hook places</returns>
        private static bool SetHook()
        {
            Custom.ConsoleLine("SetHook Called", ConsoleColor.White);
            if (hHandle == IntPtr.Zero && mhHandle == IntPtr.Zero)
            {
                hHandle = User.SetWindowsHookEx(User.WH_KEYBOARD_LL, hDelegate, mHandle, 0);
                mhHandle = User.SetWindowsHookEx(User.WH_MOUSE_LL, hDelegate, mHandle, 0);
                Custom.ConsoleLine("SetHook Complete");
                return true;
            }
            else
            {
                Custom.ConsoleLine("Failed Setting Hook", ConsoleColor.Red);
                return false;
            }
        }

        /// <summary> RemoveHook()
        /// Removes the Hook if it exists.
        /// </summary>
        /// <returns>true iff the hook was removed</returns>
        private static bool RemoveHook()
        {
            Custom.ConsoleLine("RemoveHook Called", ConsoleColor.Yellow);
            if (hHandle != IntPtr.Zero || mhHandle != IntPtr.Zero)
            {
                User.UnhookWindowsHookEx(hHandle);
                User.UnhookWindowsHookEx(mhHandle);
                Custom.ConsoleLine("RemoveHook Complete", ConsoleColor.Green);
                return true;
            }
            else
            {
                Custom.ConsoleLine("RemoveHook Failed", ConsoleColor.Red);
                return false;
            }
        }

        /// <summary> Report()
        /// Prints a report statement to the console.
        /// </summary>
        private static void Report()
        {
            Custom.ConsoleLine("STATUS REPORT:", ConsoleColor.White);
            Custom.ConsoleLine("STATE        : " + state);
            Custom.ConsoleLine("GUI         #: " + myWindow.Handle);
            Custom.ConsoleLine("CONSOLE     #: " + cHandle);
            Custom.ConsoleLine("MODULE      #: " + mHandle);
            Custom.ConsoleLine("-------------|>");
            Custom.ConsoleLine("FOREGROUND  #: " + User.GetForegroundWindow());
            Custom.ConsoleLine("ACTIVE      #: " + User.GetActiveWindow());
            Custom.ConsoleLine("TOP         #: " + User.GetTopWindow(IntPtr.Zero));
            Custom.ConsoleLine("POSITION     : " + position);
            Custom.ConsoleLine("SWAP POSITION: " + swapSave);
            Custom.ConsoleLine("END REPORT!", ConsoleColor.White);
        }

        #endregion

        #region GUI Controls:

        /// <summary> Display()
        /// Opens the GUI window
        /// if it isn't already Open.
        /// </summary>
        /// <returns>true iff the GUI was made visible</returns>
        internal static bool Display()
        {
            Custom.ConsoleLine("Display Called", ConsoleColor.White);
            //ReCalculate GUI data: Width & Icons/Programs.
            UpdateData();
            if (position.Y == 0 && freeApps.Length == 0) position.Y = 1;
            position.X0 = 1;
            myWindow.Show();
            Focus(myWindow.Handle);
            state = BaltoState.Displayed;
            safteyOn = true;
            Update();
            Custom.ConsoleLine("Display Complete");
            return true;
        }

        /// <summary> Close()
        /// Closes the GUI if it is Open.
        /// </summary>
        /// <returns>true iff the GUI was made invisible</returns>
        internal static bool Close()
        {
            Custom.ConsoleLine("Close Called", ConsoleColor.White);
            myWindow.Hide();
            Focus(cHandle);
            state = BaltoState.Hidden;
            Custom.ConsoleLine("Close Complete");
            return false;
        }

        /// <summary> CreateDeck()
        /// Creates a new empty deck at the end of the array
        /// </summary>
        /// <returns>true</returns>
        internal static bool CreateDeck()
        {
            Custom.ConsoleLine("Create Deck Called", ConsoleColor.White);
            Deck[] temp = new Deck[deckApps.Length + 1];
            deckApps.CopyTo(temp, 0);
            temp[deckApps.Length].apps = new APPDAT[0];
            deckApps = temp;
            Update();
            Custom.ConsoleLine("Create Deck Complete");
            return true;
        }

        /// <summary> Left(bool)
        /// Checks if the cursor can move Left,
        /// Then if actionable and movable:
        /// Moves the cursor position to the left.
        /// </summary>
        /// <param name="actionable">if true, actually makes the move, instead of just checking if it can</param>
        /// <returns>True if the Cursor could have been moved</returns>
        internal static bool Left(bool actionable = true)
        {
            Custom.ConsoleLine("Left Called: " + actionable.ToString(), ConsoleColor.White);
            bool movable = position.X() > 0;
            if (actionable && movable)
            {
                Custom.ConsoleLine("Moving Left");
                position.Xplus(-1);
                if (position.Y == 1) position.X2 = 0;
                Update();
            }
            return movable;
        }

        /// <summary> Left(bool, int)
        /// A special version of left, which uses the given row 
        /// instead of the current row.
        /// </summary>
        /// <param name="actionable">should the movement be made</param>
        /// <param name="row">The row to be moved Left</param>
        /// <returns></returns>
        internal static bool Left(bool actionable, int row)
        {
            Custom.ConsoleLine("Left Called: " + actionable.ToString(), ConsoleColor.White);
            bool movable = position.X(row) > 0;
            if (actionable && movable)
            {
                Custom.ConsoleLine("Moving row " + row.ToString() + " Left");
                position.Xplus(row, -1);
                if (position.Y == 1) position.X2 = 0;
                Update();
            }
            return movable;
        }

        /// <summary> Right(bool)
        /// Moves the GUI pointer RIGHT one position
        /// </summary>
        /// <returns>true if the pointer was able to move to the right</returns>
        internal static bool Right(bool actionable = true)
        {
            Custom.ConsoleLine("Right Called: " + actionable.ToString(), ConsoleColor.White);
            bool movable;
            int a;
            switch (position.Y)
            {
                case 0:
                    a = freeApps.Length - 1;
                    if (state == BaltoState.Swapping && swapSave.Y != 0) a++;
                    break;
                case 1:
                    a = deckApps.Length;
                    break;
                case 2:
                    a = deckApps[position.X1].apps.Length - 1;
                    if (state == BaltoState.Swapping && !(position.X1 == swapSave.X1 && swapSave.Y == 2)) a++;
                    break;
                default:
                    a = -1;
                    break;
            }

            //FIX FIX FIX!

            movable = position.X() < a;

            if (actionable && movable)
            {
                Custom.ConsoleLine("Moving Right");
                position.Xplus(1);
                if (position.Y == 1) position.X2 = 0;
                Update();
            }
            return movable;
        }

        /// <summary> Up(bool)
        /// Moves the GUI pointer UP one position
        /// </summary>
        /// <returns>true if the pointer was able to move up</returns>
        internal static bool Up(bool actionable = true)
        {
            Custom.ConsoleLine("Up Called: " + actionable.ToString(), ConsoleColor.White);
            bool movable;
            if      (position.Y == 0) movable = false;
            else if (position.Y == 1) movable = freeApps.Length != 0 || state == BaltoState.Swapping;
            else   /*position.Y == 2*/movable = true;

            if (actionable && movable)
            {
                Custom.ConsoleLine("Moving Up");
                position.Y--;
                Update();
            }
            return movable;
        }

        /// <summary> Down(bool)
        /// Moves the GUI pointer DOWN one position
        /// </summary>
        /// <returns>true if the pointer was able to move down</returns>
        internal static bool Down(bool actionable = true)
        {
            Custom.ConsoleLine("Its Going Down: " + actionable.ToString(), ConsoleColor.Cyan);
            bool movable;
            if      (position.Y == 0) movable = true;
            else if (position.Y == 1) movable = Right(false) && deckApps[position.X1].apps.Length > 0;
            else                      movable = false;
            if (actionable && movable)
            {
                Custom.ConsoleLine("I'm Yelling Timber!", ConsoleColor.Magenta);
                position.Y++;
                Update();
            }
            return movable;
        }

        /// <summary> Select()
        /// Selects the Icon currently under the pointer
        /// </summary>
        /// <returns>true if the select command succeeded</returns>
        internal static bool Select()
        {
            Custom.ConsoleLine("Select Called: " + position.ToString(), ConsoleColor.DarkGray);
            switch (position.Y)
            {
                case 0:
                    Close();
                    BringCurrentToFront();
                    break;
                case 1:
                    if (Right(false)) 
                    {
                    Close();
                    BringCurrentToFront();
                    } 
                    else
                    {
                        CreateDeck();
                    }
                    break;
                case 2:
                    Close();
                    BringCurrentToFront();
                    break;
            }
            Custom.ConsoleLine("Select Complete");
            return true;
        }

        /// <summary> Swap()
        /// Performs the Alt action on the Icon under the pointer
        /// </summary>
        /// <returns>true if the swap can activate</returns>
        internal static bool Swap()
        {
            Custom.ConsoleLine("Swap Called: " + position.ToString(), ConsoleColor.DarkGray);
            if (position.Y == 1 && position.X1 == deckApps.Length)
            {
                User.MessageBeep(User.MB_SIMPLE);
                return false;
            }
            swapSave = position;
            state = BaltoState.Swapping;
            myWindow.ChangeState(1); //Puts the GUI in Swap mode
            Update();
            Custom.ConsoleLine("Swap Start Complete");
            return true;
        }

        /// <summary> EndSwap()
        /// Finalizes the swap and brings console and GUI
        /// out of the swap state.
        /// </summary>
        /// <returns>true</returns>
        internal static bool EndSwap()
        {
            Custom.ConsoleLine("End-Swap Called: " + position.ToString(), ConsoleColor.DarkGray);
            switch (swapSave.Y)
            {
                case 0:
                    SwapFree();
                    break;
                case 1:
                    SwapDeck();
                    break;
                case 2:
                    SwapDeckApp();
                    break;
            }
            swapSave.valid = false;
            state = BaltoState.Displayed;
            myWindow.ChangeState(0); //Reverts the state to default
            Update();
            Custom.ConsoleLine("End-Swap Complete");
            return true;
        }
            #region EndSwap Sub-Functions:
        /// <summary>
        /// Performs the End-Swap Operation when swapping a Free-App somewhere.
        /// </summary>
        private static void SwapFree() {
            int curX2 = position.X1; 
            int deckLength;
            APPDAT[] replacement;
            ArrayList replFree = new ArrayList();
            switch (position.Y)
            {
                case 0:
                    User.MessageBeep(User.MB_SIMPLE);
                    break;
                case 1:
                    if (curX2 == deckApps.Length)
                    {
                        Deck[] replDeck = new Deck[curX2 + 1];
                        deckApps.CopyTo(replDeck, 0);
                        replDeck[curX2].apps = new APPDAT[1];
                        replDeck[curX2].apps[0] = freeApps[swapSave.X0];
                        deckApps = replDeck;
                    }
                    else
                    {
                        deckLength = deckApps[curX2].apps.Length;
                        replacement = new APPDAT[deckLength + 1];
                        deckApps[curX2].apps.CopyTo(replacement, 0);
                        replacement[deckLength] = freeApps[swapSave.X0];
                        deckApps[curX2].apps = replacement;
                    }

                    foreach (APPDAT app in freeApps)
                        if (app.handle != freeApps[swapSave.X0].handle) replFree.Add(app);
                    freeApps = (APPDAT[])replFree.ToArray(typeof(APPDAT));
                    break;
                case 2:
                    deckLength = deckApps[curX2].apps.Length;
                    replacement = new APPDAT[deckLength + 1];
                    int j = 0;
                    for (int i = 0; i < replacement.Length; i++)
                    {
                        if (i == position.X2) replacement[i] = freeApps[swapSave.X0];
                        else
                        {
                            replacement[i] = deckApps[curX2].apps[j];
                            j++;
                        }
                    }
                    deckApps[curX2].apps = replacement;

                    foreach (APPDAT app in freeApps)
                        if (app.handle != freeApps[swapSave.X0].handle) replFree.Add(app);
                    freeApps = (APPDAT[])replFree.ToArray(typeof(APPDAT));
                        break;
            }
        }

        /// <summary>
        /// Preforms the End-Swap Operation when swapping a Deck of Apps.
        /// </summary>
        private static void SwapDeck()
        {
            Deck temp = new Deck();
            Deck curDeck = new Deck();
            if (position.X1 != deckApps.Length) curDeck = deckApps[position.X1];
            Deck prvDeck = deckApps[swapSave.X1];
            switch (position.Y)
            {
                case 0:
                    DitchDeck();
                    break;
                case 1:
                    if (position.X1 != swapSave.X1 && position.X1 != deckApps.Length)
                    {
                        deckApps[position.X1] = prvDeck;
                        deckApps[swapSave.X1] = curDeck;
                    }
                    else
                    {
                        User.MessageBeep(User.MB_SIMPLE);
                    }
                    break;
                case 2:
                    if (position.X1 != swapSave.X1)
                    {
                        temp.apps = new APPDAT[curDeck.apps.Length + prvDeck.apps.Length];
                        curDeck.apps.CopyTo(temp.apps, 0);
                        prvDeck.apps.CopyTo(temp.apps, curDeck.apps.Length);
                        deckApps[position.X1] = temp;
                        Deck[] newDecks = new Deck[deckApps.Length - 1];
                        int j = 0;
                        for (int i = 0; i < deckApps.Length; i++)
                        {
                            if (swapSave.X1 != i) 
                            {
                                newDecks[j] = deckApps[i];
                                j++;
                            }
                        }
                        deckApps = newDecks;
                        if (position.X1 > swapSave.X1) Left(true, 1);                        
                    }
                    break;
            }
        }

        /// <summary>
        /// A Helper for the SwapDeck Case
        /// Removes the deck at the saved X2
        /// </summary>
        private static void DitchDeck() //really ought to remake this without the for loop
        {
            Custom.ConsoleLine("DitchDeck Called: ", ConsoleColor.Magenta);
            ArrayList newFree = new ArrayList();
            foreach (APPDAT fapp in freeApps) newFree.Add(fapp); 
            foreach (APPDAT dapp in deckApps[swapSave.X1].apps) newFree.Add(dapp);
            freeApps = (APPDAT[])newFree.ToArray(typeof(APPDAT));

            int j = 0;
            Deck[] temp = new Deck[deckApps.Length - 1];
            for (int i = 0; i < deckApps.Length; i++)
            {
                if (i != swapSave.X1)
                {
                    temp[j] = deckApps[i];
                    j++;
                }
            }
            deckApps = temp;
        }

        /// <summary>
        /// Preforms the End-Swap Operations when swapping an App from a Deck;
        /// </summary>
        private static void SwapDeckApp() 
        {
            int curX1 = position.X1;
            int deckLength;
            
            APPDAT[] replacement;
            switch (position.Y)
            {
                case 0:
                    replacement = new APPDAT[freeApps.Length + 1];
                    freeApps.CopyTo(replacement, 0);
                    replacement[freeApps.Length] = deckApps[swapSave.X1].apps[swapSave.X2];
                    freeApps = replacement;
                    RemoveSavedFromDeck();
                    break;
                case 1:
                    if (curX1 == swapSave.X1) return;

                    if (curX1 == deckApps.Length)
                    {
                        Deck[] replDeck = new Deck[curX1 + 1];
                        deckApps.CopyTo(replDeck, 0);
                        replDeck[curX1].apps = new APPDAT[1];
                        replDeck[curX1].apps[0] = deckApps[swapSave.X1].apps[swapSave.X2];
                        deckApps = replDeck;
                    }
                    else
                    {
                        deckLength = deckApps[curX1].apps.Length;
                        replacement = new APPDAT[deckLength + 1];
                        deckApps[curX1].apps.CopyTo(replacement, 0);
                        replacement[deckLength] = deckApps[swapSave.X1].apps[swapSave.X2];
                        deckApps[curX1].apps = replacement;
                    }
                    RemoveSavedFromDeck();
                    break;
                case 2:
                    if (curX1 == swapSave.X1)
                    {
                        APPDAT temp = deckApps[curX1].apps[position.X2];
                        APPDAT temp2;
                        deckApps[curX1].apps[position.X2] = deckApps[curX1].apps[swapSave.X2];
                        int i = position.X2;
                        if (swapSave.X2 > i)
                        {
                            i++;
                            while (swapSave.X2 > i)
                            {
                                temp2 = deckApps[curX1].apps[i];
                                deckApps[curX1].apps[i] = temp;
                                temp = temp2;
                                i++;
                            }
                        }
                        else
                        {
                            i--;
                            while (swapSave.X2 < i)
                            {
                                temp2 = deckApps[curX1].apps[i];
                                deckApps[curX1].apps[i] = temp;
                                temp = temp2;
                                i--;
                            }
                        }
                        deckApps[curX1].apps[swapSave.X2] = temp;
                    }
                    else
                    {
                        deckLength = deckApps[curX1].apps.Length;
                        replacement = new APPDAT[deckLength + 1];
                        int k = 0;
                        for (int i = 0; i < replacement.Length; i++)
                        {
                            if (i == position.X2) replacement[i] = deckApps[swapSave.X1].apps[swapSave.X2];
                            else
                            {
                                replacement[i] = deckApps[curX1].apps[k];
                                k++;
                            }
                        }
                        deckApps[curX1].apps = replacement;
                        RemoveSavedFromDeck();
                    }
                    break;
            }
        }

        /// <RemoveSavedFromDeck>
        /// A helper function for SwapDeckApp
        /// Removes the saved position from it's Deck.
        /// </RemoveSavedFromDeck>
        private static void RemoveSavedFromDeck() //modify in the same way you need to modify the other removal...
        {
            Custom.ConsoleLine("RemoveSaved Called: ", ConsoleColor.Magenta);
            APPDAT[] temp = new APPDAT[deckApps[swapSave.X1].apps.Length - 1];
            int j = 0;
            for (int i = 0; i < temp.Length + 1; i++)
            {
                if (i != swapSave.X2)
                {
                    temp[j] = deckApps[swapSave.X1].apps[i];
                    j++;
                }                
            }
            deckApps[swapSave.X1].apps = temp;
        }
        #endregion

        /// <summary> Update()
        /// Updates the GUI using the given parameters
        /// </summary>
        /// <returns></returns>
        public static void Update()
        {
            Custom.ConsoleLine("Update Called! ", ConsoleColor.White);
            //Makes sure all current positions are safe.
            while (position.X0 >= freeApps.Length && position.X0 != 0) position.X0--;
            while (position.X0 < 0) position.X0++;
            while (position.X1 >  deckApps.Length && position.X1 != 0) position.X1--;
            while (position.X1 < 0) position.X0++;
            if (position.X1 != deckApps.Length && state != BaltoState.Swapping) 
                while (position.X2 >= deckApps[position.X1].apps.Length && position.X2 != 0) position.X2--;
            while (position.X2 < 0) position.X0++;
            myWindow.Update(freeApps, deckApps, ref position, ref swapSave);
            Custom.ConsoleLine("Update Complete");
        }

        #endregion

        #region Window Controls:

        /// <summary> BringCurrentToFront()
        /// Brings the Object which the Cursor is currently over
        /// to the front using ShowWindow( ____, SHOW)
        /// </summary>
        internal static void BringCurrentToFront()
        {
            Custom.ConsoleLine("BringCurrentToFront Called", ConsoleColor.White);
            switch (position.Y) 
            {
                case 0:  
                    Focus(freeApps[position.X0].handle);
                    break;
                case 1:
                    if (Right(false)) 
                    {
                        foreach (APPDAT app in deckApps[position.X1].apps.Reverse<APPDAT>())
                        {
                            Focus(app.handle);
                        }
                    }
                    break;
                case 2:
                    Focus(deckApps[position.X1].apps[position.X2].handle);
                    break;
            }
            Custom.ConsoleLine("BringCurrentToFront Complete");
        }

        /// <summary> Focus(IntPtr)
        /// Brings the window associated with the given handle to attention
        /// </summary>
        /// <param name="handle">Handle to the Window being focused</param>
        internal static void Focus(IntPtr handle)
        {
            Custom.ConsoleLine("Focus Called: " + handle.ToString(), ConsoleColor.White);

            User.OpenIcon(handle);
            User.SetWindowPos(handle, User.HWND_TOPMOST, 0, 0, 0, 0, User.SWP_NOMOVE | User.SWP_NOSIZE);
            User.SetActiveWindow(handle);
            User.SetForegroundWindow(handle);
            User.SetWindowPos(handle, User.HWND_NOTOPMOST, 0, 0, 0, 0, User.SWP_NOMOVE | User.SWP_NOSIZE);
            if (myWindow.Handle == User.GetForegroundWindow())
            {
                User.SetActiveWindow(handle);
            }

            Custom.ConsoleLine("Focus Complete");
        }

        #endregion

        #region Data Collection:

        /// <summary> UpdateData()
        /// Updates the Application Data Fields
        /// </summary>
        private static void UpdateData()
        {
            Custom.ConsoleLine("UpdateData Called", ConsoleColor.White);
            int k;
            freeApps = DataMiner.Exclusions(DataMiner.GetWindows());
            for (int i = 0; i < deckApps.Length; i++ )
            {
                for (int j = 0; j < deckApps[i].apps.Length; j++)
                {
                    deckApps[i].apps[j].valid = false;
                    if ((k = Array.IndexOf(freeApps, deckApps[i].apps[j])) >= 0)
                    {
                        freeApps[k].validate(false);
                        deckApps[i].apps[j].validate(true);
                    }

                }
                deckApps[i].apps = APPDAT.ValidOnly(deckApps[i].apps);
            }
            freeApps = APPDAT.ValidOnly(freeApps);
            Custom.ConsoleLine("UpdateData Complete");
        }

        #endregion

        #endregion

        #region CallBack:

        /// <summary> HookCallback(int, IntPtr, IntPtr)
        /// The CallBack function tied to low-level Keyboard events:
        /// Passes sub-zero nCodes straight to the return;
        /// </summary>
        /// <param name="nCode">Usage Code</param>
        /// <param name="wParam">the CallBack Message Type</param>
        /// <param name="lParam">the CallBack Message Info</param>
        /// <returns></returns>
        internal static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            Keys msg = (Keys)Marshal.ReadInt32(lParam);
            if (nCode >= 0)
            {
                //if (wParam == (IntPtr)User.WM_KEYDOWN || wParam == (IntPtr)User.WM_SYSKEYDOWN) Custom.ConsoleLine("Key Down! State: " + state.ToString(), ConsoleColor.DarkCyan);
                switch (state)
                {
                    case BaltoState.Starting:
                        CaseStarting(wParam, msg);
                        break;
                    case BaltoState.Closing:
                        CaseClosing(wParam, msg);
                        break;
                    case BaltoState.Displayed:
                        CaseDisplayed(wParam, msg);
                        break;
                    case BaltoState.Hidden:
                        CaseHidden(wParam, msg);
                        break;
                    case BaltoState.Locked:
                        CaseLocked(wParam, msg);
                        break;
                    case BaltoState.Test:
                        CaseTest(wParam, msg);
                        break;
                    case BaltoState.Swapping:
                        CaseSwapping(wParam, msg);
                        break;
                }                
            }
            if (wParam == (IntPtr)User.WM_SYSKEYDOWN && msg == Keys.Q) return (IntPtr)1;
            return User.CallNextHookEx(hHandle, nCode, wParam, lParam);
        }

            #region State-Case Methods:
        /// <summary> CaseStarting(IntPtr, Keys)
        /// Operations for when the Console is in the Starting state.
        /// </summary>
        /// <param name="type">The Key Code Type, KeyUp KeyDown etc</param>
        /// <param name="message">The Key from the Forms.Keys enum</param>
        internal static void CaseStarting(IntPtr type, Keys message)
        {
            Report();
            state = BaltoState.Hidden;
        }

        /// <summary> CaseClosing(IntPtr, Keys)
        /// Operations for when the Console is in the Closing state
        /// </summary>
        /// <param name="type">The Key Code Type, Key Up KeyDown etc</param>
        /// <param name="message">The Key from the Forms.Keys enum</param>
        internal static void CaseClosing(IntPtr type, Keys message)
        {
            ShutDown();
        }

        /// <summary> CaseDisplayed(IntPtr, Keys)
        /// Operations for when the console is in the Displayed state
        /// </summary>
        /// <param name="type">The Key Code Type, Key Up KeyDown etc</param>
        /// <param name="message">The Key from the Forms.Keys enum</param>
        internal static void CaseDisplayed(IntPtr type, Keys message)
        {

            if (type == (IntPtr)User.WM_KEYDOWN || type == (IntPtr)User.WM_SYSKEYDOWN)
            {
                
                Dictionary<Commands, Keys[]> myBind = KeyConfigs[bind];
                if (myBind[Commands.CloseGUI].Contains(message))
                {

                    Close();
                }
                else if (myBind[Commands.Left].Contains(message))
                {
                    Left();
                }
                else if (myBind[Commands.Right].Contains(message))
                {
                    Right();
                }
                else if (myBind[Commands.Up].Contains(message))
                {
                    Up();
                }
                else if (myBind[Commands.Down].Contains(message))
                {
                    Down();
                }
                else if (myBind[Commands.Select].Contains(message))
                {
                
                }
                else if (myBind[Commands.Swap].Contains(message))
                {
                    Swap();
                }
                else if (message == Keys.Z && type == (IntPtr)User.WM_SYSKEYDOWN)
                {
                    state = BaltoState.Closing;
                }
                else if (myBind[Commands.Report].Contains(message)) 
                {
                    Report();
                }
                else if (myBind[Commands.OpenCnsl].Contains(message) && type == (IntPtr)User.WM_SYSKEYDOWN) 
                {
                    Show();
                }
                else if (myBind[Commands.CloseCnsl].Contains(message) && type == (IntPtr)User.WM_SYSKEYDOWN)
                {
                    Hide();
                }
                else
                {
                    //TODO error
                }
            }
            else if (type == (IntPtr)User.WM_KEYUP || type == (IntPtr)User.WM_SYSKEYUP)
            {
                if (KeyConfigs[bind][Commands.Select].Contains(message))
                {
                    Custom.ConsoleLine("Saftey Off", ConsoleColor.Yellow);
                    if (safteyOn) safteyOn = false;
                    else Select();
                }
            }
            if (myWindow.Handle != User.GetActiveWindow() && state == BaltoState.Displayed)
            {
                Custom.ConsoleLine("NOT THE FOCUS!", ConsoleColor.Red);
                Close();
                return;
            }
        }

        /// <summary> CaseHidden(IntPtr, Keys)
        /// Operations for when the console is in the Hidden state
        /// </summary>
        /// <param name="type">The Key Code Type, Key Up KeyDown etc</param>
        /// <param name="message">The Key from the Forms.Keys enum</param>
        internal static void CaseHidden(IntPtr type, Keys message)
        {
            if (type == (IntPtr)User.WM_SYSKEYDOWN && message == Keys.Q)
            {
                Display();
                state = BaltoState.Displayed;
            }
            else if (type == (IntPtr)User.WM_KEYDOWN && KeyConfigs[bind][Commands.Report].Contains(message))
            {
                Report();
            }
        }

        /// <summary> CaseSwapping(IntPtr, Keys)
        /// Operations for the console during a swap
        /// </summary>
        /// <param name="type">The Key Code Type, Key Up KeyDown etc</param>
        /// <param name="message">The Key from the Forms.Keys enum</param>
        internal static void CaseSwapping(IntPtr type, Keys message)
        {
            if (type == (IntPtr)User.WM_KEYDOWN || type == (IntPtr)User.WM_SYSKEYDOWN)
            {
                Dictionary<Commands, Keys[]> myBind = KeyConfigs[bind];
                if (myBind[Commands.CloseGUI].Contains(message))
                {
                    Close();
                }
                else if (myBind[Commands.Left].Contains(message))
                {
                    Left();
                }
                else if (myBind[Commands.Right].Contains(message))
                {
                    Right();
                }
                else if (myBind[Commands.Up].Contains(message))
                {
                    Up();
                }
                else if (myBind[Commands.Down].Contains(message))
                {
                    Down();
                }
                else if (myBind[Commands.Select].Contains(message))
                {
                    Select();
                }
                else if (myBind[Commands.Swap].Contains(message))
                {
                    EndSwap();
                }
                else if (message == Keys.Z && type == (IntPtr)User.WM_SYSKEYDOWN)
                {
                    state = BaltoState.Closing;
                }
                else if (myBind[Commands.Report].Contains(message))
                {
                    Report();
                }
                else
                {
                    //TODO error
                }
            }
        }

        /// <summary> CaseLocked(IntPtr, Keys)
        /// Operations for when the console is in the Locked state
        /// </summary>
        /// <param name="type">The Key Code Type, Key Up KeyDown etc</param>
        /// <param name="message">The Key from the Forms.Keys enum</param>
        internal static void CaseLocked(IntPtr type, Keys message)
        {
            //Dont do much
        }

        /// <summary> CaseTest(IntPtr, Keys)
        /// Operations for the when the console is in the Test state.
        /// Instantly Locks and Reveals the Console.
        /// Runs full test suite, Records Results into Log
        /// Then Unlocks the Console
        /// </summary>
        /// <param name="type">The Key Code Type, Key Up KeyDown et</param>
        /// <param name="message">The Key from the Forms.Keys enum</param>
        internal static void CaseTest(IntPtr type, Keys message)
        {

        }
        #endregion

        #endregion
    }

    #region Structures:


    /// <summary> FourPoint Structure
    /// The structure used to keep track of the window position
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FourPoint
    {
        public int X0, X1, X2, Y;
        public bool valid;

        #region Constructors:

        /// <summary> Constructor FourPoint(int, int, int, int)
        /// Takes arguments for all the values, except valid. Assumes true.
        /// </summary>
        /// <param name="x0">The First row position</param>
        /// <param name="x1">The Second row position</param>
        /// <param name="x2">The Third row position</param>
        /// <param name="y">The Active Row</param>
        public FourPoint(int x0, int x1, int x2, int y)
        {
            this.X0 = x0;
            this.X1 = x1;
            this.X2 = x2;
            this.Y = y;
            this.valid = true;
        }

        /// <summary> Constructor FourPoint(bool)
        /// Creates a Zeroed FourPoint, 
        /// With the given bool for valid.
        /// </summary>
        /// <param name="valid">The validity of the constructed 4Point</param>
        public FourPoint(bool valid)
        {
            this.X0 = 0;
            this.X1 = 0;
            this.X2 = 0;
            this.Y = 0;
            this.valid = valid;
        }

        #endregion

        #region Methods:

        public int X(int i)
        {
            switch (i)
            {
                case 0:
                    return X0;
                case 1:
                    return X1;
                case 2:
                    return X2;
                default:
                    return -1;
            }
        }

        public int X(string i = "Y")
        {
            switch (i)
            {
                case "Y":
                    return X(Y);
                default:
                    return -1;
            }
        }

        public void Xplus(int row, int i) 
        {
            switch (row)
            {
                case 0: X0 += i;
                    break;
                case 1: X1 += i;
                    break;
                case 2: X2 += i;
                    break;
            }
        }

        public void Xplus(int i)
        {
            Xplus(Y, i);
        }

        /// <summary> ToString()
        /// Override for the ToString Method
        /// </summary>
        /// <returns>String</returns>
        public override String ToString()
        {
            return "(" + this.X0.ToString() + ", " + this.X1.ToString() + ", " + this.X2.ToString() + ", " + this.Y.ToString() + ")";
        }

        #endregion

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Deck
    {
        private static int count;

        internal string name;
        internal APPDAT[] apps;
        internal Color bgColor;
    }

    #endregion 
}
