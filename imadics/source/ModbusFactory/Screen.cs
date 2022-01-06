using System;
using Mindmagma.Curses;

namespace SampleScenes
{
    public class Screen
    {
        IntPtr _screenPtr;
        Screen(IntPtr screenPtr)
        {
            _screenPtr = screenPtr;
        }
        /// <summary>
        /// Creates the screen object and indicates whether NCurses was sucessfuly initialized.
        /// <para>
        /// It is always possible to use this Screen object. In some cases however, 
        /// it is not possible to initialize NCurses because the terminal does not support it.
        /// In this case the dummy screen object is created producing void output.
        /// </para>
        /// </summary>
        /// <param name="created"></param>
        /// <returns></returns>
        public static Screen CreateScreen(out bool success)
        {
            var screenPtr = IntPtr.Zero; 
            try
            {

                screenPtr = NCurses.InitScreen();
                NCurses.NoDelay (screenPtr, true);
                NCurses.NoEcho ();
                NCurses.Erase ();
                success = true;
            }
            catch(Exception)
            {
                success = false;
            }
            return new Screen(screenPtr);
        } 
        public void MoveAddString(int row, int col, string message)
        {
            if (_screenPtr != IntPtr.Zero) NCurses.MoveAddString(row, col, message);
        }
        public void Refresh()
        {
            if (_screenPtr != IntPtr.Zero) NCurses.Refresh();    
        }
    }
}