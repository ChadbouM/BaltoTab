using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Balto;

namespace Balto
{
    /// <summary>
    /// TA specialized NativeWindow used as the GUI for BALTO-TAB
    /// Inherits IDisposable as well.
    /// </summary>
    class BaltoGUI : NativeWindow, IDisposable
    {
        #region Attributes:
        private const int iconY0 = 124;
        private const int iconY1 = 239;
        private const int iconY2 = 334;

        internal Rectangle  mainScreen;
        internal Point      location;
        internal Size       size;
        internal bool       showing;
        private  bool       disposed;
        private  System.Reflection.Assembly exe;
        private  Image      bgImage;
        private  Image      deckImg;
        private  Image      plusImg;
        private  Size       iconSize;
        private  ULW        ulw;
        private GUIstate   state;
        internal const int  count = 7;

        /// <summary>
        /// Enumeration of the Graphical states of Operation.
        /// </summary>
        private enum GUIstate
        {
            Default,
            Swapping
        }

        #endregion

        /// <summary>Constructor
        /// Sets Attributes to Defaults
        /// </summary>
        public BaltoGUI()
        {

            this.mainScreen =     Screen.PrimaryScreen.Bounds;

            this.size       = new Size(500 , 450);
            this.location   = new Point((mainScreen.Width / 2) - (this.size.Width / 2), (mainScreen.Height / 2) - (this.size.Height / 2));
            this.showing    =     false;
            this.disposed   =     false;
            this.exe        =     typeof(BaltoGUI).Assembly;
            this.bgImage    =     Image.FromStream(this.exe.GetManifestResourceStream("Balto.Graphics.GUIBG.png"));
            this.deckImg    =     Image.FromStream(this.exe.GetManifestResourceStream("Balto.Graphics.DeckSym.png"));
            this.plusImg    =     Image.FromStream(this.exe.GetManifestResourceStream("Balto.Graphics.PlusSym.png"));
            this.iconSize   = new Size(32, 32);
            this.state      =     GUIstate.Default;
            this.ulw        = new ULW(this.location, this.size);
        }

        #region Methodology:

        /// <summary> Create()
        /// Creates the window handle
        /// </summary>
        /// <returns>true if the handle was created</returns>
        public bool Create()
        {
            if (base.Handle == IntPtr.Zero || base.Handle == null)
            {
                CreateParams myParam = new CreateParams();
                uint pop_up     = User.WS_POPUP;
                myParam.X       = this.location.X;
                myParam.Y       = this.location.Y;
                myParam.Height  = size.Height;
                myParam.Width   = size.Width;
                myParam.Parent  = IntPtr.Zero;
                myParam.Style   = (int)pop_up;
                myParam.ExStyle = User.WS_EX_TOPMOST | User.WS_EX_TOOLWINDOW | User.WS_EX_LAYERED | User.WS_EX_TRANSPARENT;
                this.CreateHandle(myParam);
                
                return true;
            }
            else return false;
        }

        internal void ChangeState(int i)
        {
            if (i == 1) this.state = GUIstate.Swapping;
            else this.state = GUIstate.Default;
        }

        /// <summary> Hide()
        /// Makes the GUI invisible
        /// </summary>
        /// <returns>returns true if the GUI was made invisible</returns>
        internal bool Hide()
        {
            if (base.Handle == IntPtr.Zero) return false; //Window does not exist 
            if (this.showing)
            { //Conceal:
                User.ShowWindow(base.Handle, User.SW_HIDE);
                this.showing = false;
                return true;
            } //Window was already Hidden
            else return false;
        }

        /// <summary> Show()
        /// Makes the GUI visible
        /// </summary>
        /// <returns>Returns tru of the windows was made visible</returns>
        internal bool Show()
        {
            if (base.Handle == IntPtr.Zero)
            {   //Window does not exist, needs to be created.
                this.Create();
                this.showing = false;
            }
            if (!this.showing)
            { //Revelations:
                User.ShowWindow(base.Handle, User.SW_SHOW);
                this.showing = true;
                return true;
            } //Window was already showing
            else return false;
        }

        /// <summary> Update(APPDAT[], APPDAT[], ref FourPoint, ref FourPoint)
        /// ReDraws and Updates the GUI window.
        /// </summary>
        /// <param name="free">The APPDAT[] used to draw Row0</param>
        /// <param name="decks">The APPDAT[][] used to draw Row1 and Row2</param>
        /// <returns></returns>
        internal bool Update(APPDAT[] free, Deck[] decks, ref FourPoint position, ref FourPoint swaPosition)
        {
            // Draw the GUI
            Bitmap bitmap1 = new Bitmap(this.size.Width, this.size.Height, PixelFormat.Format32bppArgb);
            using (Graphics graphics1 = Graphics.FromImage(bitmap1))
            {
                this.Draw(graphics1, free, decks, ref position, ref swaPosition);
            }
            //Update the GUI-Window
            Rectangle rectangle1;
            SIZE size1;
            POINT point1;
            POINT point2;
            BLENDFUNCTION blendfunction1;
            rectangle1 = new Rectangle(0, 0, this.size.Width, this.size.Height);
            IntPtr ptr1 = User.GetDC(IntPtr.Zero);
            IntPtr ptr2 = Gdi.CreateCompatibleDC(ptr1);
            IntPtr ptr3 = bitmap1.GetHbitmap(Color.FromArgb(0));
            IntPtr ptr4 = Gdi.SelectObject(ptr2, ptr3);
            size1.cx = this.size.Width;
            size1.cy = this.size.Height;
            point1.x = this.location.X;
            point1.y = this.location.Y;
            point2.x = 0;
            point2.y = 0;
            blendfunction1 = new BLENDFUNCTION();
            blendfunction1.BlendOp = 0;
            blendfunction1.BlendFlags = 0;
            blendfunction1.SourceConstantAlpha = 255;
            blendfunction1.AlphaFormat = 1;

            User.UpdateLayeredWindow(base.Handle, ptr1, ref point1, ref size1, ptr2, ref point2, 0, ref blendfunction1, 2); //2=ULW_ALPHA
            Gdi.SelectObject(ptr2, ptr4);
            User.ReleaseDC(IntPtr.Zero, ptr1);
            Gdi.DeleteObject(ptr3);
            Gdi.DeleteDC(ptr2);
            return true;
        }

        #region Drawing:

        /// <summary> Draw(Graphics, APPDAT[], APPDAT[], ref FourPoing, ref FourPoint)
        /// Redraws the GUI using data from the console.
        /// </summary>
        /// <param name="graphic"> The graphic object being used to draw the GUI</param>
        /// <param name="free"> The APPDAT[] representing the Free-Apps.</param>
        /// <param name="decks"> The APPDAT[][] representing the Decked-Apps</param>
        /// <param name="position"> The FourPoint used to represent the cursor position</param>
        /// <param name="saved"> The FourPoint used to tell the cursor position at swap-call</param>
        internal void Draw(Graphics graphic, APPDAT[] free, Deck[] decks, ref FourPoint position, ref FourPoint saved)
        {
            bool DrawDeck = position.X1 != decks.Length;
            APPDAT[] CrntDeck;
            if (DrawDeck) CrntDeck = decks[position.X1].apps;
            else CrntDeck = new APPDAT[0];

            GraphicsPath path = new GraphicsPath();
            //Paint BG, should cover up anything that used to be there.
            graphic.DrawImage(this.bgImage, new Point(0, 0));
            switch (state)
            {
                    //Default Case is so straight forward.
                case GUIstate.Default:
                    DrawRow0(graphic, free, position.X0, count);
                    DrawRow1(graphic, decks.Length, position.X1);
                    if (DrawDeck) DrawRow2(graphic, CrntDeck, position.X2, count);
                    DrawCursor(graphic, Color.Black, position.Y);
                    break;
                    //Unlike the swapping case, which is radically different in a million ways, and looks like the ugliest code ever.
                case GUIstate.Swapping:
                    Color theBG = Color.Transparent;
                    switch  (saved.Y)
                    {
                        case 0:
                            theBG = free[saved.X0].iconBG;
                            DrawRow0b(graphic, free, ref position, ref saved, count, theBG, free[saved.X0].icon);
                            DrawRow1b(graphic, decks.Length, ref position, ref saved, theBG, free[saved.X0].icon);
                            if (DrawDeck) DrawRow2b(graphic, CrntDeck, ref position, ref saved, count, theBG, free[saved.X0].icon);
                            break;
                        case 1:
                            DrawRow0b(graphic, free, ref position, ref saved, count, theBG, deckImg);
                            DrawRow1b(graphic, decks.Length, ref position, ref saved, theBG, deckImg);
                            if (DrawDeck) DrawRow2b(graphic, CrntDeck, ref position, ref saved, count, theBG, deckImg);
                            break;
                        case 2:
                            theBG = decks[saved.X1].apps[saved.X2].iconBG;
                            DrawRow0b(graphic, free, ref position, ref saved, count, theBG, decks[saved.X1].apps[saved.X2].icon);
                            DrawRow1b(graphic, decks.Length, ref position, ref saved, theBG, decks[saved.X1].apps[saved.X2].icon);
                            if (DrawDeck) DrawRow2b(graphic, CrntDeck, ref position, ref saved, count, theBG, decks[saved.X1].apps[saved.X2].icon);
                            break;
                    }
                    DrawCursor(graphic, Color.Blue, position.Y);
                    break;
            }
            if (this.state == GUIstate.Default)
            {
                switch (position.Y)
                {
                    case 0:
                        DrawWindowText(graphic, free[position.X0].windowText, Color.Black);
                        break;
                    case 1:
                        if (position.X1 == decks.Length) DrawWindowText(graphic, "New Deck", Color.Black);
                        else DrawWindowText(graphic, "Deck", Color.Black);
                        break;
                    case 2:
                        DrawWindowText(graphic, decks[position.X1].apps[position.X2].windowText, Color.Black);
                        break;
                }
            }
            else if (this.state == GUIstate.Swapping)
            {
                switch (saved.Y)
                {
                    case 0:
                        DrawWindowText(graphic, free[saved.X0].windowText, Color.Black);
                        break;
                    case 1:
                        DrawWindowText(graphic, "Deck", Color.Black);
                        break;
                    case 2:
                        DrawWindowText(graphic, decks[saved.X1].apps[saved.X2].windowText, Color.Black);
                        break;
                }
            }
        }

            #region Sub-Draw Functions:

        /// <summary> DrawCursor(Graphics, Color, int)
        /// Draws the GUI Cursor using the given graphic and color
        /// Using the position of this window to determine where it
        /// should be drawn.
        /// </summary>
        /// <param name="graphic">The graphic used to draw the Cursor</param>
        /// <param name="color">The Color to draw the Cursor in</param>
        /// <param name="y">The row to draw the cursour in</param>
        internal void DrawCursor(Graphics graphic, Color color, int y)
        {
            switch (y)
            {
                case 0:
                    graphic.DrawRectangle(new Pen(new SolidBrush(color), 8), new Rectangle(221, iconY0 - 13, 58, 58));
                    break;
                case 1:
                    graphic.DrawRectangle(new Pen(new SolidBrush(color), 8), new Rectangle(221, iconY1 - 13, 58, 58));
                    break;
                case 2:
                    graphic.DrawRectangle(new Pen(new SolidBrush(color), 8), new Rectangle(221, iconY2 - 13, 58, 58));
                    break;
            }
        }

        /// <summary> DrawWindowText(Graphics, string, Color)
        /// Draws the Text portion of the GUI
        /// </summary>
        /// <param name="graphic"></param>
        /// <param name="wText"></param>
        /// <param name="color"></param>
        internal void DrawWindowText(Graphics graphic, string wText, Color color)
        {
            string[][] delimiters = new string[][] 
            {
                new string[1] { " - " },
                new string[1] { " " },
                new string[2] { "\\", "/" },
                new string[1] { "." }
            };
            SolidBrush textBrush = new SolidBrush(color);
            Font textFont = new Font("Arial", 12);

            string textBase   = wText.Trim();
            Size   textSize   = graphic.MeasureString(textBase, textFont).ToSize();

            string overflow = "";
            Size ofSize = graphic.MeasureString(overflow, textFont).ToSize();

            string[] splitString;
            bool delimited = false;

            while (textSize.Width >= 375)
            {
                foreach (string[] delimiter in delimiters)
                {
                    splitString = textBase.Reverse().Split(delimiter, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (splitString.Length == 2)
                    {
                        //Error if delimited true; TODO
                        textBase = splitString[1].Reverse();
                        textSize = graphic.MeasureString(textBase, textFont).ToSize();
                        overflow = splitString[0].Reverse() + overflow;
                        ofSize = graphic.MeasureString(overflow, textFont).ToSize();
                        delimited = true;
                        break;
                    }
                }
                if (!delimited)
                {
                    overflow = textBase[textBase.Length - 1] + overflow;
                    textBase = textBase.Substring(0, textBase.Length - 1);
                }
                delimited = false;
            }
            while (ofSize.Width >= 350)
            {
                foreach (string[] delimiter in delimiters)
                {
                    splitString = overflow.Reverse().Split(delimiter, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (splitString.Length == 2)
                    {
                        //Error if delimited true; TODO
                        overflow = splitString[1].Reverse();
                        ofSize = graphic.MeasureString(overflow, textFont).ToSize();
                        delimited = true;
                        break;
                    }
                }
                if (!delimited)
                {
                    overflow = textBase.Substring(0, textBase.Length - 1);
                    ofSize = graphic.MeasureString(overflow, textFont).ToSize();
                }
                delimited = false;
            }
            Point textLoc = new Point(250 - textSize.Width/2, 175);
            graphic.DrawString(textBase, textFont, textBrush, textLoc);
            textLoc = new Point(250 - ofSize.Width/2, 205);
            graphic.DrawString(overflow, textFont, textBrush, textLoc);
        }

        /// <summary> DrawIcon(Graphics, Icon, Color, int, int)
        ///  Draws the Icon specifed by the Icon and Location
        ///  With the given Background Color
        /// </summary>
        /// <param name="g">The Graphics object used to paint the icon</param>
        /// <param name="icon">The Icon to be drawn</param>
        /// <param name="color">The Background Color</param>
        /// <param name="x"> the x position</param>
        /// <param name="y"> the y position</param>
        internal void DrawIcon(Graphics g, Icon icon, Color color, int x, int y)
        {
            g.FillEllipse(new SolidBrush(color), new Rectangle(x - 7, y - 7, 46, 46));
            if (color != Color.Transparent) g.DrawEllipse(new Pen(new SolidBrush(Color.Black)), new Rectangle(x - 7, y - 7, 46, 46));
            g.DrawIcon(icon, new Rectangle(x, y, 32, 32));
        }

        /// <summary> DrawRow0(Graphics, APPDAT[], int, int)
        /// Draws the default Row0
        /// </summary>
        /// <param name="graphic">The graphic object used to draw</param>
        /// <param name="apps">The APPDAT[] being drawn as row0</param>
        /// <param name="x">The current X0 position</param>
        /// <param name="count">The number of icons to draw on each side of the focus</param>
        internal void DrawRow0(Graphics graphic, APPDAT[] apps, int x, int count)
        {
            int avg = (this.size.Width / 2 - 109) / count;
            int buffer = 75;

            int LeftX;
            int RightX;
            for (int i = 0; i < count; i++)
            {
                LeftX = x - count + i;
                RightX = x + count - i;
                if (LeftX >= 0) DrawIcon(graphic, apps[LeftX].icon, apps[LeftX].iconBG, buffer + avg - (count - i) * 2, iconY0);
                if (RightX < apps.Length) DrawIcon(graphic, apps[RightX].icon, apps[RightX].iconBG, this.size.Width - 16 - buffer - avg + (count - i) * 2, iconY0);
                buffer += (avg - (count - i));
            }
            if (apps.Length > x) DrawIcon(graphic, apps[x].icon, apps[x].iconBG, 234, iconY0); 
        }

        /// <summary> DrawRow0b(Graphics, APPDAT[], ref FourPoint, ref FourPoint, int, Object) 
        /// The Swap-State draw method.
        /// Detects the state it should be in and draws the correct form of Row0
        /// </summary>
        /// <param name="graphic">The graphic object used to draw</param>
        /// <param name="apps">The APPDAT[] being drawn as row0</param>
        /// <param name="cur">The Current position</param>
        /// <param name="sav">The saved position for swap</param>
        /// <param name="count">The number of icons to draw on each side of the focus</param>
        /// <param name="img">The saved icon to draw given as an object</param>
        internal void DrawRow0b(Graphics graphic, APPDAT[] apps, ref FourPoint cur, ref FourPoint sav, int count, Color bgColor, Object img = null)
        {

            int avg = (this.size.Width / 2 - 109) / count;
            int buffer = 75;

            int LeftX;
            int RightX;
            bool LeftCheck;
            bool RightCheck;
            if (sav.Y == 0)
            {
                for (int i = 0; i < count; i++)
                {
                    LeftX = cur.X0 - count + i;
                    RightX = cur.X0 + count - i;
                    LeftCheck = cur.X0 - sav.X0 - count + i >= 0;
                    RightCheck = sav.X0 - cur.X0 - count + i >= 0;
                    if (LeftCheck)
                    {
                        if (LeftX + 1 >= 0) DrawIcon(graphic, apps[LeftX + 1].icon, apps[LeftX + 1].iconBG, buffer + avg - (count - i) * 2, iconY0);
                    }
                    else if (LeftX >= 0) DrawIcon(graphic, apps[LeftX].icon, apps[LeftX].iconBG, buffer + avg - (count - i) * 2, iconY0);
                    if (RightCheck)
                    {
                        if (RightX - 1 >= 0) DrawIcon(graphic, apps[RightX - 1].icon, apps[RightX - 1].iconBG, this.size.Width - 16 - buffer - avg + (count - i) * 2, iconY0);
                    }
                    else if (RightX < apps.Length) DrawIcon(graphic, apps[RightX].icon, apps[RightX].iconBG, this.size.Width - 16 - buffer - avg + (count - i) * 2, iconY0);
                    buffer += (avg - (count - i));
                }
                DrawIcon(graphic, (Icon)img, bgColor, 234, iconY0);
            }
            else if (cur.Y == 0)
            {
                for (int i = 0; i < count; i++)
                {
                    LeftX = cur.X0 - count + i;
                    RightX = cur.X0 + count - i;
                    if (LeftX >= 0) DrawIcon(graphic, apps[LeftX].icon, apps[LeftX].iconBG, buffer + avg - (count - i) * 2, iconY0);
                    if (RightX - 1 < apps.Length) DrawIcon(graphic, apps[RightX - 1].icon, apps[RightX - 1].iconBG, this.size.Width - 16 - buffer - avg + (count - i) * 2, iconY0);
                    buffer += (avg - (count - i));
                }
                if (sav.Y == 1) graphic.DrawImage((Image)img, new Point(225, 125)); /*WAYPOINT 1*/
                else DrawIcon(graphic, (Icon)img, bgColor, 234, iconY0);
            }
            else DrawRow0(graphic, apps, cur.X0, count);
        }

        /// <summary> DrawRow1(Graphics, int, int)
        /// Draws the default Image of Row1
        /// </summary>
        /// <param name="graphic">The graphic object being used to draw</param>
        /// <param name="length">the number of decks to draw</param>
        /// <param name="x">the current Row1 position</param>
        internal void DrawRow1(Graphics graphic, int length, int x)
        {
            int count = 3;

            int LeftX;
            int RightX;
            for (int i = 0; i < count; i++)
            {
                LeftX = x - count + i;
                RightX = x + count - i;
                if (LeftX >= 0) graphic.DrawImage(deckImg, new Point(55 + 55 * i, iconY1 - 9));
                if (RightX < length) graphic.DrawImage(deckImg, new Point(400 - (55 * i), iconY1 - 9));
                if (RightX == length) graphic.DrawImage(plusImg, new Point(400 - (55 * i), iconY1 - 9));
            }
            if (x < length) graphic.DrawImage(deckImg, new Point(225, iconY1 - 9));
            else graphic.DrawImage(plusImg, new Point(225, iconY1 - 9));
        }

        /// <summary> DrawRow1b(Graphics, int, ref FourPoint, ref FourPoint, Object)
        /// The Swap-State draw method.
        /// Detects the state it should be in and draws the correct form of Row1
        /// </summary>
        /// <param name="graphic">The graphic object used to draw</param>
        /// <param name="length">The number of decks to draw</param>
        /// <param name="cur">the current position</param>
        /// <param name="sav">the saved position for swapping</param>
        /// <param name="img">the saved icon to draw</param>
        internal void DrawRow1b(Graphics graphic, int length, ref FourPoint cur, ref FourPoint sav, Color bgColor, Object img = null)
        {
            int count = 3;

            int LeftX;
            int RightX;
            bool LeftCheck;
            bool RightCheck;
            if (sav.Y == 1)
            {
                for (int i = 0; i < count; i++)
                {
                    LeftX = cur.X1 - count + i;
                    RightX = cur.X1 + count - i;
                    LeftCheck = cur.X1 - sav.X1 - count + i >= 0;
                    RightCheck = sav.X1 - cur.X1 - count + i >= 0;
                    if (LeftCheck)
                    {
                        if (LeftX + 1 >= 0) graphic.DrawImage(deckImg, new Point(55 + 55 * i, iconY1 - 9));
                    }
                    else if (LeftX >= 0) graphic.DrawImage(deckImg, new Point(55 + 55 * i, iconY1 - 9));
                    if (RightCheck)
                    {
                        if (RightX - 1 < length) graphic.DrawImage(deckImg, new Point(400 - (55 * i), iconY1 - 9));
                        else if (RightX - 1 == length) graphic.DrawImage(plusImg, new Point(400 - (55 * i), iconY1 - 9));
                    }
                    else if (RightX < length) graphic.DrawImage(deckImg, new Point(400 - (55 * i), iconY1 - 9));
                    else if (RightX == length) graphic.DrawImage(plusImg, new Point(400 - (55 * i), iconY1 - 9));
                }
                graphic.DrawImage((Image)img, new Rectangle(234, iconY1, 32, 32));
            }
            else 
            {
                DrawRow1(graphic, length, cur.X1);
                if (cur.Y == 1 && img != null) DrawIcon(graphic, (Icon)img, bgColor, 234, iconY1); 
            }

        }

        /// <summary> DrawRow2(Graphics, apps, int, int)
        /// Draws the default image of Row2
        /// </summary>
        /// <param name="graphic">The graphic object used to draw</param>
        /// <param name="apps">The APPDAT[] being drawn</param>
        /// <param name="x">the current position in the X2 row</param>
        /// <param name="count">the number of icons to draw on each side of the focus</param>
        internal void DrawRow2(Graphics graphic, APPDAT[] apps, int x, int count) 
        {
            int avg = (this.size.Width / 2 - 109) / count;
            int buffer = 75;

            int LeftX;
            int RightX;
            for (int i = 0; i < count; i++)
                    {
                        LeftX = x - count + i;
                        RightX = x + count - i;
                        if (LeftX >= 0 && apps[LeftX].icon != null)
                        {
                            DrawIcon(graphic, apps[LeftX].icon, apps[LeftX].iconBG, buffer + avg - (count - i) * 2, iconY2);
                        }
                        if (RightX < apps.Length)
                        {
                            DrawIcon(graphic, apps[RightX].icon, apps[RightX].iconBG, this.size.Width - 16 - buffer - avg + (count - i) * 2, iconY2);
                        }
                        buffer += (avg - (count - i));
                    }
                    if (apps.Length > x) 
                    {
                        DrawIcon(graphic, apps[x].icon, apps[x].iconBG, 234, iconY2);
                    }
        }

        /// <summary> DrawRow2b(Graphics, APPDAT[], ref FourPoint, ref FourPoint, int, Object)
        /// The Swap-State draw method.
        /// Detects the state it should be in and draws the correct form of Row2
        /// </summary>
        /// <param name="graphic">The graphic object used to draw</param>
        /// <param name="apps">The APPDAT[] being drawn as row0</param>
        /// <param name="cur">The Current position</param>
        /// <param name="sav">The saved position for swap</param>
        /// <param name="count">The number of icons to draw on each side of the focus</param>
        /// <param name="img">The saved icon to draw</param>
        internal void DrawRow2b(Graphics graphic, APPDAT[] apps, ref FourPoint cur, ref FourPoint sav, int count, Color bgColor, Object img = null)
        {
            int avg = (this.size.Width / 2 - 109) / count;
            int buffer = 75;

            int LeftX;
            int RightX;
            bool LeftCheck;
            bool RightCheck;
            if (sav.Y == 2 && cur.X1 == sav.X1) //swapping within same deck
            {
                Console.Write("\n");
                for (int i = 0; i < count; i++)
                {
                    LeftX = cur.X2 - count + i;
                    RightX = cur.X2 + count - i;
                    LeftCheck = cur.X2 - sav.X2 - count + i >= 0; //on the left of save
                    RightCheck = sav.X2 - cur.X2 - count + i >= 0; //on the right of save
                    if (LeftCheck)
                    {
                        if (LeftX + 1 >= 0) DrawIcon(graphic, apps[LeftX + 1].icon, apps[LeftX + 1].iconBG, buffer + avg - (count - i) * 2, iconY2);
                    }
                    else if (LeftX >= 0) DrawIcon(graphic, apps[LeftX].icon, apps[LeftX].iconBG, buffer + avg - (count - i) * 2, iconY2);
                    if (RightCheck)
                    {
                        if (RightX - 1 >= 0) DrawIcon(graphic, apps[RightX - 1].icon, apps[RightX - 1].iconBG, this.size.Width - 16 - buffer - avg + (count - i) * 2, iconY2);
                    }
                    else if (RightX < apps.Length) DrawIcon(graphic, apps[RightX].icon, apps[RightX].iconBG, this.size.Width - 16 - buffer - avg + (count - i) * 2, iconY2);
                    buffer += (avg - (count - i));
                }
                DrawIcon(graphic, (Icon)img, bgColor, 234, iconY2);
            }
            else if (cur.Y == 2)
            {
                Console.Write("\n");
                for (int i = 0; i < count; i++)
                {
                    LeftX = cur.X2 - count + i;
                    RightX = cur.X2 + count - i;
                    if (LeftX >= 0) DrawIcon(graphic, apps[LeftX].icon, apps[LeftX].iconBG, buffer + avg - (count - i) * 2, iconY2);
                    if (RightX - 1 < apps.Length) DrawIcon(graphic, apps[RightX - 1].icon, apps[RightX - 1].iconBG, this.size.Width - 16 - buffer - avg + (count - i) * 2, iconY2);
                    buffer += (avg - (count - i));
                }
                if (sav.Y == 1) graphic.DrawImage((Image)img, new Rectangle(234, iconY2, 32, 32));
                else DrawIcon(graphic, (Icon)img, bgColor, 234, iconY2);
            }
            else DrawRow2(graphic, apps, cur.X2, count);
        }
            #endregion
        #endregion
        #endregion

        #region IDispose:
        /// <summmary>
        /// Required function to be an IDisposable.
        /// Destroyies this window.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.DestroyHandle();
                this.disposed = true;
                GC.SuppressFinalize(this);
            }
            else
            {
                // TODO Error?!
            }
        }
        #endregion
    }

    #region Structures:
    #endregion
}