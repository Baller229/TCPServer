using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace Server
{
    public partial class Server : Form
      {
      public delegate void TextForRichTextBoxEventHandler ( object sender, MyEventArgs e );
      private List<Client> clients = new List<Client> ( );
      private Socket serverSocket;
      private int port;
      private byte [ ] buffer;
      Thread threadCheckingClients;

      public Server ( )
         {
         InitializeComponent ( );
         }

      // -- MAIN SERVER METHODS --
      private void StartServer ( )
         {
         Log.dbg ( "ServerStart" );
         port = Convert.ToInt32 ( textBoxPort.Text );
         buffer = new byte [ 1024 ];
         serverSocket = new Socket ( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
         serverSocket.Bind ( new IPEndPoint ( IPAddress.Any, port ) ); // Any znamena ze pocuva na 0.0.0.0, co znamena ze na vsetkych sietovych kartach
         serverSocket.Listen ( 20 ); // zacne pocuvat ci sa niekto nechce pripojit, limt si dal na 20
         Log.dbg ( "BeginAccept..." );
         serverSocket.BeginAccept ( new AsyncCallback ( AcceptCallback ), null ); // kedze Async tak AcceptCallback sa vyvola v novom threade
         }
       
      private void StopServer ( )
         {
         try
            {
            int count = clients.Count;
            for ( int i = ( count - 1 ); i >= 0; i-- )
               {
               if ( clients [ i ].Socket.Connected == true )
                  {
                  clients [ i ].SendCommand ( clients [ i ].Socket, "server_offline", "", "", "", "", "" );
                  clients [ i ].Socket.Shutdown ( SocketShutdown.Both );
                  }
               clients [ i ].Socket.Close ( );
               clients.RemoveAt ( i );
               }
            if ( serverSocket.Connected == true )
               serverSocket.Shutdown ( SocketShutdown.Both );
            serverSocket.Close ( );
            }
         catch ( Exception ) { }
         }
      private void CheckingClients ( )
         {
         Log.dbg ( "ThreadStart" );
         try
            {
            while ( true )
               {
               if ( clients.Count > 0 )
                  {
                  foreach ( Client item in clients )
                     {
                     try
                        {
                        item.Socket.Blocking = false;
                        item.Socket.Send ( new byte [ 1 ], 0, 0 );
                        if ( item.Socket.Connected == false )
                           throw new Exception ( );
                        }
                     catch ( Exception )
                        {
                        item.Socket.Close ( );
                        clients.Remove ( item );

                        string onlineClients = "";

                        for ( int i = 0; i < clients.Count; i++ )
                           onlineClients += clients [ i ].Nick + ",";

                        if ( onlineClients != "" )
                           onlineClients = onlineClients.Remove ( onlineClients.Length - 1 );

                        for ( int i = 0; i < clients.Count; i++ )
                           clients [ i ].SendCommand ( clients [ i ].Socket, "these_are_online", "", "", "", onlineClients, "" );

                        return;
                        }
                     }

                  }
               Thread.Sleep ( 2000 );
               }
            }
         catch ( Exception ) { }
         Log.dbg ( "ThreadEnd" );
         }

      // -- FRONTEND METHODS --
      private void ChangeFrontEndToOnline ( )
         {
         this.richTextBox1.Clear ( );
         this.textBoxPort.ReadOnly = true;
         this.buttonStart.Text = "Stop";
         this.richTextBox1.AppendText ( DateTime.Now.ToLongTimeString ( ) + ": Server is online!\n" );
         }
      private void ChangeFrontEndToOffline ( )
         {
         this.textBoxPort.ReadOnly = false;
         this.buttonStart.Text = "Start";
         this.richTextBox1.AppendText ( DateTime.Now.ToLongTimeString ( ) + ": Server is offline!\n" );
         }

      // -- ASYNCHRONOUS SERVER SOCKET METHODS --
      // zavola sa v novom threade, ked sa pripoji prvy novy klienta
      private void AcceptCallback ( IAsyncResult result )
         {
         Thread.CurrentThread.Name = "AcceptThread" + Thread.CurrentThread.ManagedThreadId;
            Log.dbg("Pripojil sa novy client {0}", Thread.CurrentThread.Name);

         try
            {
            Log.dbg ( "EndAccept Start" );
            Socket newSocket = serverSocket.EndAccept ( result );  // na BeginAccept sa musi volat EndAccept
            Log.dbg("EndAccept End");
            NewClientConnect( newSocket );
            }
         catch ( System.ObjectDisposedException e )
            {
            // Socket is not alive anymore!
            //Log.err ( "ErrMsg;{0}", e.Message );
            Log.err("ErrMsg;EndAccept");
            }
            Log.dbg ( "koniecThreadu;{0}", Thread.CurrentThread.Name );

         // caka sa na dalsich klientov
         bool whileFlag = true;
         while ( whileFlag )
            {
            try
               {
               Log.dbg ( "Accept..." );
               Socket newSocket = serverSocket.Accept ( ); // tu stoji!!! pokial sa niekto nepripoji
                    Log.dbg("Accepted!!!");
                    NewClientConnect ( newSocket );
               }
            catch ( Exception ex )
               {
               Log.err ( "Error Accept" );
               whileFlag = false;
               }
            }
         }

      private void NewClientConnect ( Socket newSocket )
         {
         Log.dbg ( "ClientSaPripojil;{0}", newSocket.Handle.ToString ( ) );
         Client client = new Client ( newSocket, clients );
         //clients.Add ( client );
         client.UpdateRichTextBox += new Client.TextForRichTextBoxEventHandler ( UpdateRichTextBox );
         }

      private void EndAcceptCallback ( IAsyncResult result )
         {
         Socket newSocket = serverSocket.EndAccept ( result );
         }

      // -- EVENTS --
      private void Form1_Load ( object sender, EventArgs e )
         {
         IPHostEntry host;
         host = Dns.GetHostEntry ( Dns.GetHostName ( ) );
         foreach ( IPAddress ip in host.AddressList )
            {
            if ( ip.AddressFamily == AddressFamily.InterNetwork )
               this.textBoxServerIP.Text = ip.ToString ( );
            }

         port = 8888;

         this.textBoxPort.Text = port.ToString ( );
         }
      private void ButtonStart_Click ( object sender, EventArgs e )
         {
         if ( buttonStart.Text == "Start" )
            {
            ChangeFrontEndToOnline ( );
            StartServer ( );
            threadCheckingClients = new Thread ( CheckingClients );
            }
         else
            {
            ChangeFrontEndToOffline ( );
            StopServer ( );
            }
         }
      private void TextBoxPort_TextChanged ( object sender, EventArgs e )
         {
         try
            {
            port = Convert.ToInt32 ( textBoxPort.Text );
            if ( port > 65535 )
               throw new Exception ( );
            }
         catch ( Exception )
            {
            MessageBox.Show ( "Port is invalid!" );
            port = 8888;
            textBoxPort.Text = port.ToString ( );
            }
         }
      private void UpdateRichTextBox ( object sender, MyEventArgs e )
         {
         string text = e.Text + "\n";
         if ( richTextBox1.InvokeRequired )
            {
            richTextBox1.Invoke ( ( MethodInvoker ) delegate
               {
                  richTextBox1.AppendText ( text );
                  richTextBox1.ScrollToCaret ( );
                  } );
            }
         else
            {
            richTextBox1.AppendText ( text );
            richTextBox1.ScrollToCaret ( );
            }

         }
      }

   public class MyEventArgs : EventArgs
      {
      private string text;

      public MyEventArgs ( string text )
         {
         this.text = text;
         }

      // -- PROPERTIES --
      public string Text
         {
         get
            {
            return text;
            }
         }
      }
   }