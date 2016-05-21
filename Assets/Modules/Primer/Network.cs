using System;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Primer
{
	#region ���ͺͽ��ջ�����
	public class ByteBuffer
	{
		private byte[] _array;
		public byte[] array
		{
			get { return _array; }
		}
		private int _length;
		public int length
		{
			get { return _length; }
		}
		private int _offset;
		public int offset
		{
			get { return _offset; }
		}

		public ByteBuffer()
		{
			_array = new byte[1024];
			_length = 0;
			_offset = 0;
		}

		public void Write(byte[] bytes)
		{
			Write(bytes, 0, bytes.Length);
		}

		public void Write(byte[] bytes, int length)
		{
			Write(bytes, 0, length);
		}

		private void checksize(int length)
		{
			if (_offset + _length + length > _array.Length)
			{
				int need = _length + length;
				if (need <= _array.Length && _offset >= (_array.Length + 1) >> 1)
				{
					Array.Copy(_array, _offset, _array, 0, _length);
					_offset = 0;
				}
				else
				{
					int size = _array.Length << 1;
					while (size < need)
					{
						size = size << 1;
					}
					byte[] array = new byte[size];
					Array.Copy(_array, _offset, array, 0, _length);
					_offset = 0;
					_array = array;
				}
			}
		}

		public void Write(byte[] bytes, int offset, int length)
		{
			checksize(length);
			Array.Copy(bytes, offset, _array, _offset + _length, length);
			_length += length;
		}

		public void Write(IntPtr bytes, int length)
		{
			checksize(length);
			Marshal.Copy(bytes, _array, _offset + _length, length);
			_length += length;
		}

		public void Pop(int length)
		{
			if (length > _length)
				length = _length;
			_offset += length;
			_length -= length;
			if (_length == 0)
			{
				Reset();
			}
		}

		public void Reset()
		{
			_offset = 0;
			_length = 0;
		}
	}
	#endregion

	/// <summary>
	/// ����������Ϣ����
	/// </summary>
	public interface NetListener
	{
		void OnReceive(NetHandler handler, ByteBuffer buffer);
		void OnError(NetHandler handler, Exception error);
		void OnClose(NetHandler handler);
	}

	/// <summary>
	/// �������ӳ�����
	/// </summary>
	public interface NetHandler
	{
		bool Write(byte[] bytes);
		bool Write(byte[] bytes, int length);
		bool Write(byte[] bytes, int offset, int length);
		bool Write(IntPtr bytes, int length);
		void Destroy();
		NetListener Listen { get; set; }
		bool IsConnected { get; }
	}

	/// <summary>
	/// �������ȫ�ֽӿ�
	/// </summary>
	public class NetManager : IDisposable
	{
		private static bool IsRunning = true;

		private bool inited = false;
		private bool running = true;
		private readonly List<NetHandlerImpl> sockets = new List<NetHandlerImpl>();
		private readonly List<NetHandlerImpl> newsockets = new List<NetHandlerImpl>();
		private readonly List<NetHandlerImpl> deletesockets = new List<NetHandlerImpl>();
		private readonly Dictionary<int, Control> listens = new Dictionary<int, Control>();
		private readonly Dictionary<NetHandlerImpl, int> receives = new Dictionary<NetHandlerImpl, int>();
		private readonly List<UpdateOrder> updates = new List<UpdateOrder>();
		private readonly List<Connecting> connectings = new List<Connecting>();
		private readonly List<Accepting> acceptings = new List<Accepting>();

		#region �ڲ�ʹ�õ���
		private class Control
		{
			public bool running;
			public Action<NetHandler> action;
		}

		private struct Accepting
		{
			public NetHandler socket;
			public Action<NetHandler> action;
		}

		private enum OrderType
		{
			Receive,
			Error,
			Close,
		}

		private struct UpdateOrder
		{
			public NetHandlerImpl socket;
			public OrderType type;
			public object param;
		}

		private struct Connecting
		{
			public Action<NetHandler, bool> callback;
			public NetHandler socket;
			public bool result;
		}
		#endregion

		public static void ExitAll()
		{
			IsRunning = false;
		}

		public void Listen(int port, Action<NetHandler> callback)
		{
			if (!running)
				throw new ObjectDisposedException(ToString());
			Init();
			new Thread(delegate()
			{
				TcpListener server = new TcpListener(IPAddress.Any, port);
				server.Start();
				Control ctrl;
				lock (listens)
				{
					if (listens.TryGetValue(port, out ctrl))
					{
						ctrl.running = true;
					}
					else
					{
						ctrl = new Control { running = true };
						listens[port] = ctrl;
					}
				}
				ctrl.action = callback;
				while (running && IsRunning)
				{
					if (!server.Server.Poll(1000, SelectMode.SelectRead))
					{
						if (!ctrl.running)
							break;
						continue;
					}
					if (!server.Pending())
						break;
					NetHandlerImpl socket = new NetHandlerImpl(server.AcceptSocket()) { Manager = this, Blocking = false };
					lock (acceptings)
					{
						acceptings.Add(new Accepting { socket = socket, action = ctrl.action });
					}
					lock (newsockets)
					{
						newsockets.Add(socket);
					}
				}
				lock (listens)
				{
					Control ctrlnow;
					if (listens.TryGetValue(port, out ctrlnow) && ctrlnow == ctrl)
					{
						listens.Remove(port);
					}
				}
				server.Stop();
			}).Start();
		}

		public void Stop(int port)
		{
			if (!running)
				throw new ObjectDisposedException(ToString());
			lock (listens)
			{
				Control ctrl;
				if (listens.TryGetValue(port, out ctrl))
				{
					ctrl.running = false;
					listens.Remove(port);
				}
			}
		}

		public NetHandler Connect(string ipport, int timeout)
		{
			Regex regex = new Regex("^(.+):(\\d+)$", RegexOptions.Singleline);
			Match match = regex.Match(ipport);
			return !match.Success ? null :
				Connect(match.Captures[0].Value, Convert.ToInt32(match.Captures[1].Value), timeout);
		}

		public NetHandler Connect(string ipport, int timeout, Action<NetHandler, bool> callback)
		{
			Regex regex = new Regex("^(.+):(\\d+)$", RegexOptions.Singleline);
			Match match = regex.Match(ipport);
			return !match.Success ? null :
				Connect(match.Captures[0].Value, Convert.ToInt32(match.Captures[1].Value), timeout, callback);
		}

		public NetHandler Connect(string ip, int port, int timeout)
		{
			if (!running)
				throw new ObjectDisposedException(ToString());
			Init();
			NetHandlerImpl socket = new NetHandlerImpl { Manager = this, Blocking = false };
			try
			{
				socket.Connect(ip, port);
			}
			catch (SocketException e)
			{
				if (e.SocketErrorCode == SocketError.WouldBlock || e.SocketErrorCode == SocketError.InProgress)
				{
					if (!(socket.Poll(timeout * 1000, SelectMode.SelectWrite) && !socket.Poll(0, SelectMode.SelectError)))
					{
						socket.Close();
						return null;
					}
					lock (newsockets)
					{
						newsockets.Add(socket);
					}
					return socket;
				}
				throw;
			}
			if (!(socket.Poll(0, SelectMode.SelectWrite) && !socket.Poll(0, SelectMode.SelectError)))
			{
				socket.Close();
				return null;
			}
			lock (newsockets)
			{
				newsockets.Add(socket);
			}
			return socket;
		}

		public NetHandler Connect(string ip, int port, int timeout, Action<NetHandler, bool> callback)
		{
			if (!running)
				throw new ObjectDisposedException(ToString());
			Init();
			NetHandlerImpl socket = new NetHandlerImpl { Manager = this, Blocking = false };
			try
			{
				socket.Connect(ip, port);
			}
			catch (SocketException e)
			{
				if (e.SocketErrorCode == SocketError.WouldBlock || e.SocketErrorCode == SocketError.InProgress)
				{
					new Thread(delegate()
					{
						bool result = socket.Poll(timeout * 1000, SelectMode.SelectWrite) && !socket.Poll(0, SelectMode.SelectError);
						if (result)
						{
							lock (newsockets)
							{
								newsockets.Add(socket);
							}
						}
						else
						{
							socket.Close();
						}
						lock (connectings)
						{
							connectings.Add(new Connecting { callback = callback, socket = socket, result = result });
						}
					}).Start();
					return socket;
				}
				throw;
			}
			{
				bool result = socket.Poll(0, SelectMode.SelectWrite) && !socket.Poll(0, SelectMode.SelectError);
				if (result)
				{
					lock (newsockets)
					{
						newsockets.Add(socket);
					}
				}
				else
				{
					socket.Close();
				}
				lock (connectings)
				{
					connectings.Add(new Connecting { callback = callback, socket = socket, result = result });
				}
			}
			return socket;
		}

		public void Dispose()
		{
			running = false;
		}

		public void Close()
		{
			Dispose();
		}

		private void Init()
		{
			if (inited)
				return;

			inited = true;
			new Thread(delegate()
			{
				List<NetHandlerImpl> reads = new List<NetHandlerImpl>();
				List<NetHandlerImpl> writers = new List<NetHandlerImpl>();
				List<NetHandlerImpl> errors = new List<NetHandlerImpl>();
				List<NetHandlerImpl> news = new List<NetHandlerImpl>();
				List<UpdateOrder> orders = new List<UpdateOrder>();

				byte[] bytes = new byte[64 * 1024];

				while (running && IsRunning)
				{
					if (newsockets.Count > 0)
					{
						lock (newsockets)
						{
							sockets.AddRange(newsockets);
							newsockets.Clear();
						}
					}
					if (deletesockets.Count > 0)
					{
						lock (deletesockets)
						{
							for (int i = 0; i < deletesockets.Count; i++)
							{
								NetHandlerImpl socket = deletesockets[i];
								sockets.Remove(socket);
								socket.Close();
								orders.Add(new UpdateOrder { socket = socket, type = OrderType.Close });
							}
							deletesockets.Clear();
						}
					}
					if (orders.Count > 0)
					{
						lock (updates)
						{
							updates.AddRange(orders);
						}
						orders.Clear();
					}
					if (sockets.Count == 0)
					{
						Thread.Sleep(0);
						continue;
					}
					for (int i = 0; i < sockets.Count; i++)
					{
						NetHandlerImpl socket = sockets[i];
						reads.Add(socket);
						errors.Add(socket);
						if (socket.need_send)
							writers.Add(socket);
					}
					Socket.Select(reads, writers, errors, 5);
					for (int i = 0; i < writers.Count; i++)
					{
						NetHandlerImpl socket = writers[i];
						ByteBuffer buffer = socket.write_buffer;
						lock (buffer)
						{
							int length = socket.Send(buffer.array, buffer.offset, buffer.length, SocketFlags.None);
							buffer.Pop(length);
							socket.need_send = buffer.length != 0;
						}
					}
					for (int i = 0; i < reads.Count; i++)
					{
						NetHandlerImpl socket = reads[i];
						ByteBuffer buffer = socket.read_buffer_tmp;
						try
						{
							int total = 0;
							while (reads[i].Available > 0)
							{
								int length = reads[i].Receive(bytes);
								if (length == 0)
								{
									break;
								}
								total += length;
								buffer.Write(bytes, length);
							}
							if (total > 0)
							{
								news.Add(socket);
							}
							else
							{
								lock (deletesockets)
								{
									deletesockets.Add(socket);
								}
							}
						}
						catch (SocketException e)
						{
							if (e.SocketErrorCode == SocketError.ConnectionReset ||
								e.SocketErrorCode == SocketError.ConnectionAborted ||
								e.SocketErrorCode == SocketError.NotConnected ||
								e.SocketErrorCode == SocketError.Shutdown)
							{
								lock (deletesockets)
								{
									deletesockets.Add(socket);
								}
							}
							else
							{
								orders.Add(new UpdateOrder { socket = socket, type = OrderType.Error, param = e });
							}
						}
						catch (Exception e)
						{
							orders.Add(new UpdateOrder { socket = socket, type = OrderType.Error, param = e });
						}
					}
					if (news.Count > 0)
					{
						lock (updates)
						{
							for (int i = 0; i < news.Count; i++)
							{
								NetHandlerImpl socket = news[i];
								socket.read_buffer.Write(socket.read_buffer_tmp.array, socket.read_buffer_tmp.offset, socket.read_buffer_tmp.length);
								socket.read_buffer_tmp.Reset();
								if (!receives.ContainsKey(socket))
								{
									receives.Add(socket, updates.Count);
									updates.Add(new UpdateOrder { socket = socket, type = OrderType.Receive });
								}
							}
						}
						news.Clear();
					}
					reads.Clear();
					writers.Clear();
					errors.Clear();
				}
			}).Start();
			Loop.RunAlways(this.ToString(), delegate()
			{
				if (connectings.Count > 0)
				{
					lock (connectings)
					{
						for (int i = 0; i < connectings.Count; i++)
						{
							Connecting connecting = connectings[i];
							connecting.callback(connecting.socket, connecting.result);
						}
						connectings.Clear();
					}
				}
				if (acceptings.Count > 0)
				{
					lock (acceptings)
					{
						for (int i = 0; i < acceptings.Count; i++)
						{
							Accepting accepting = acceptings[i];
							accepting.action(accepting.socket);
						}
						acceptings.Clear();
					}
				}
				if (updates.Count > 0)
				{
					lock (updates)
					{
						for (int i = 0; i < updates.Count; i++)
						{
							UpdateOrder order = updates[i];
							if (order.socket.listen != null)
							{
								switch (order.type)
								{
									case OrderType.Receive:
										order.socket.listen.OnReceive(order.socket, order.socket.read_buffer);
										break;
									case OrderType.Close:
										order.socket.listen.OnClose(order.socket);
										break;
									case OrderType.Error:
										order.socket.listen.OnError(order.socket, (Exception)order.param);
										break;
									default:
										throw new ArgumentOutOfRangeException();
								}
							}
						}
						updates.Clear();
						receives.Clear();
					}
				}
			});
		}

		#region �������ӵľ�����
		private class NetHandlerImpl : Socket, NetHandler
		{
			public NetManager Manager;
			public bool need_send = false;
			public readonly ByteBuffer write_buffer = new ByteBuffer();
			public readonly ByteBuffer read_buffer = new ByteBuffer();
			public readonly ByteBuffer read_buffer_tmp = new ByteBuffer();
			public NetListener listen = null;

			public NetHandlerImpl()
				: base(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
			{
				SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(false, 0));
			}

			public NetHandlerImpl(Socket socket)
				: base(socket.DuplicateAndClose(Process.GetCurrentProcess().Id))
			{
				SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(false, 0));
			}

			public void Destroy()
			{
				lock (Manager.deletesockets)
				{
					Manager.deletesockets.Add(this);
				}
			}

			public bool Write(byte[] bytes)
			{
				return Write(bytes, 0, bytes.Length);
			}

			public bool Write(byte[] bytes, int length)
			{
				return Write(bytes, 0, length);
			}

			public bool Write(byte[] bytes, int offset, int length)
			{
				lock (write_buffer)
				{
					if (write_buffer.length >= (2 << 24))
						return false;
					need_send = true;
					write_buffer.Write(bytes, offset, length);
				}
				return true;
			}

			public bool Write(IntPtr bytes, int length)
			{
				lock (write_buffer)
				{
					if (write_buffer.length >= (2 << 24))
						return false;
					need_send = true;
					write_buffer.Write(bytes, length);
				}
				return true;
			}

			public NetListener Listen
			{
				get { return listen; }
				set { listen = value; }
			}

			public bool IsConnected
			{
				get { return Connected; }
			}
		}
		#endregion
	}
}