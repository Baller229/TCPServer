using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace Server
   {
   static class Program
      {
      /// <summary>
      /// Hlavní vstupní bod aplikace.
      /// </summary>
      [STAThread]
      static void Main ( )
         {
         Thread.CurrentThread.Name = "MainThread";
         Log.dbg ( "MainStart" );
         Application.EnableVisualStyles ( );
         Application.SetCompatibleTextRenderingDefault ( false );
         Application.Run ( new Server ( ) );
         
         }
      }
   }
