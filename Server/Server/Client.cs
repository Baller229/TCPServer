using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    public class Client
      {
      private List<Client> clients;
      private byte [ ] buffer;
      private Socket socket;
      private string nick;
      public delegate void TextForRichTextBoxEventHandler ( object sender, MyEventArgs e );
      public event TextForRichTextBoxEventHandler UpdateRichTextBox;

      public Client ( Socket socket, List<Client> clients )
         {
         this.clients = clients;
         clients.Add ( this );
         buffer = new byte [ 1024 ];
         nick = "-";
         this.socket = socket;
         Log.dbg ( "BeginReceiveCallback;{0};{1}", socket.Handle.ToString (), Nick );
         this.socket.BeginReceive ( buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback ( ReceiveCallback ), this.socket );
         }

      // -- PROPERTIES --
      public string Nick
         {
         set
            {
            this.nick = value;
            }
         get
            {
            return this.nick;
            }
         }
      public Socket Socket
         {
         set
            {
            this.socket = value;
            }
         get
            {
            return this.socket;
            }
         }

      // -- MAIN CLIENT METHODS --
      private void ConnectMsg ( Socket socket, string nick )
         {
         // check if exists another client with the same nick
         if ( clients.Exists ( item => item.Nick.Equals ( nick ) ) == true )
            {
            // tu zrusi klienta ktory uz existoval na serveri
            Client client = clients.Find ( item => item.Socket.RemoteEndPoint.Equals ( socket.RemoteEndPoint ) );
            client.SendCommand ( client.Socket, "connection_refused", "", "", "", "", "" );
            clients.Remove ( client );
            //return; // tento return je zbytocny, ked som vymazal predchadzajuceho, noveho mozem akceptovat, bolo by dobre pokracovat
            }
         this.nick = nick;
         string onlineClients = "";
         foreach ( var client in clients )
            {
            onlineClients += client.Nick + ",";
            }
         onlineClients = onlineClients.Remove ( onlineClients.Length - 1 );
         foreach ( var client in clients )
            {
            client.SendCommand ( client.Socket, "these_are_online", "server", "", client.Nick, onlineClients, "" );
            }
         Log.dbg ( "BeginReceiveCallback;{0}", Nick );
         socket.BeginReceive ( buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback ( ReceiveCallback ), socket );

         }
      private void DisconnectMsg ( Socket socket, string param )
         {
         int i = clients.FindIndex ( item => item.Nick.Equals ( param ) );
         clients.RemoveAt ( i );
         string onlineClients = "";
         foreach ( var client in clients )
            {
            onlineClients += client.Nick + ",";
            }
         if ( onlineClients != "" )
            onlineClients = onlineClients.Remove ( onlineClients.Length - 1 );
         foreach ( var client in clients )
            {
            client.SendCommand ( client.Socket, "these_are_online", "server", "", client.Nick, onlineClients, "" );
            }
         socket.Shutdown ( SocketShutdown.Both );
         socket.Close ( );
         }
      private void SendCommandMessage ( Socket socket, string msgFrom, string param2, string msgTo, string msg, string param5 )
         {
         // hlada v zozname klientov take meno, ake prislo v param3 (Miro)
         int i = clients.FindIndex ( item => item.Nick.Equals ( msgTo ) );

         // ak ho najde, i hovori ze kolkaty klient ma to meno, a posle mu message, napr. Mirovi
         clients [ i ].SendCommand ( clients [ i ].Socket, "message", msgFrom, param2, msgTo, msg, param5 );

         }
      public void SendCommand ( Socket socket, string command, string param1, string param2, string param3, string param4, string param5 )
         {
         try
            {
            string stringData = command + "|" + param1 + "|" + param2 + "|" + param3 + "|" + param4 + "|" + param5;
            Log.dbg ( "Msg;From;{0};To;{1};Msg;{2}", param1, param3, stringData );

            byte [ ] byteData = Encoding.UTF8.GetBytes ( stringData );
            socket.BeginSend ( byteData, 0, byteData.Length, 0, new AsyncCallback ( SendCallback ), socket );
            //SendToServerFrontEnd(DateTime.Now.ToLongTimeString() + ": >> " + stringData);
            }
         catch ( Exception ) { }
         }
      private void ParseAndExecute ( string text, Socket socket )
         {
         // obmedzenie je, ze nemozem posielat v sprave znak '|' !!!!
         Log.dbg ( "Msg;{0};{1};{2}", socket.Handle.ToString (), Nick, text );
         string [ ] words = text.Split ( '|' );

         string command = words [ 0 ];
         string nickFrom = words [ 1 ];
         string param2 = words [ 2 ];
         string nickTo = words [ 3 ];
         string msg = words [ 4 ];
         string param5 = words [ 5 ];

         switch ( command )
            {
            case ( "connect" ):
               {
               ConnectMsg ( socket, nickFrom ); // param1 je nick
               SendToServerFrontEnd ( DateTime.Now.ToLongTimeString ( ) + ": Client " + nickFrom + " connected!" );
               break;
               }
            case ( "disconnect" ):
               {
               DisconnectMsg ( socket, nickFrom );
               SendToServerFrontEnd ( DateTime.Now.ToLongTimeString ( ) + ": Client " + nickFrom + " disconnected!" );
               break;
               }
            case ( "message" ):
               {
               SendCommandMessage ( socket, nickFrom, param2, nickTo, msg, param5 );

               // zacne cakat na dalsiu message
               Log.dbg ( "BeginReceiveCallback;{0}", Nick );
               socket.BeginReceive ( buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback ( ReceiveCallback ), socket );

               SendToServerFrontEnd ( DateTime.Now.ToLongTimeString ( ) + ": " + nickFrom + " >> " + nickTo + ": " + msg );
               break;
               }
            default:
               {
               break;
               }
            }
         }

      // -- FRONTEND METHODS --
      private void SendToServerFrontEnd ( string text )
         {
         MyEventArgs myArgs = new MyEventArgs ( text );
         if ( UpdateRichTextBox != null )
            {
            UpdateRichTextBox ( this, myArgs );
            }
         }

      // -- ASYNCHRONOUS SERVER SOCKET METHODS --
      // vykonava sa pre kazdeho jedneho klienta v inom threade, ked pride sprava of klienta!!!
      public void ReceiveCallback ( IAsyncResult result )
         {
         if ( ( Thread.CurrentThread.Name == string.Empty ) | ( Thread.CurrentThread.Name == null ) )
            {
            Thread.CurrentThread.Name = "ReceiveThread" + "_" + Thread.CurrentThread.ManagedThreadId;
            }

         Socket socket = ( Socket ) result.AsyncState;
         try
            {
            Log.dbg ( "EndReceive;{0};{1}", socket.Handle, Nick );
            int size = socket.EndReceive ( result );
            Log.dbg ( "MsgPrijataOd;{0};{1};{2}", socket.Handle, Nick, size );
            byte [ ] buffertemp = new byte [ size ];
            Array.Copy ( buffer, buffertemp, size );
            string receivedText = Encoding.UTF8.GetString ( buffertemp ); //dekoduje vsetky bajty do retazca
                Log.dbg("DekodovanaSprava;{0}", receivedText);
                //SendToServerFrontEnd(DateTime.Now.ToLongTimeString() + ": << " + receivedText);
                ParseAndExecute ( receivedText, socket );
            }
         catch ( Exception ) { }
         }
      private void SendCallback ( IAsyncResult result )
         {
         }
      }
   }