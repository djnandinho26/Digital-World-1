using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace Digital_World.Network
{
    public class SocketWrapper : IDisposable
    {
        public int ListenPort = 0;

        private ManualResetEvent allDone = new ManualResetEvent(false);
        private Thread tWorker;
        private Socket listener;
        private volatile bool isRunning = false;
        private volatile bool isDisposed = false;
        private readonly object syncLock = new object();

        public SocketWrapper()
        {
        }

        public void Listen(int Port)
        {
            ThrowIfDisposed();
            
            lock (syncLock)
            {
                if (isRunning)
                {
                    Console.WriteLine("[AVISO] Servidor já está em execução.");
                    return;
                }
                
                ListenPort = Port;
                tWorker = new Thread(new ParameterizedThreadStart(Start));
                tWorker.IsBackground = true;
                tWorker.Name = $"SocketListener-{Port}";
                tWorker.Start(Port);
            }
        }

        public void Listen(ServerInfo info)
        {
            ThrowIfDisposed();
            
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            
            lock (syncLock)
            {
                if (isRunning)
                {
                    Console.WriteLine("[AVISO] Servidor já está em execução.");
                    return;
                }
                
                ListenPort = info.Port;
                tWorker = new Thread(new ParameterizedThreadStart(Start));
                tWorker.IsBackground = true;
                tWorker.Name = $"SocketListener-{info.Port}";
                tWorker.Start(info);
            }
        }
    
        public void Stop()
        {
            if (!isRunning)
            {
                Console.WriteLine("[AVISO] Servidor já está parado.");
                return;
            }
            
            lock (syncLock)
            {
                isRunning = false;
                
                Console.WriteLine("[INFO] Parando servidor de socket...");
                
                if (listener != null)
                {
                    try
                    {
                        if (listener.Connected)
                            listener.Shutdown(SocketShutdown.Both);
                    }
                    catch { }
                    
                    try
                    {
                        listener.Close();
                        listener.Dispose();
                        listener = null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERRO] Ao fechar listener: {ex.Message}");
                    }
                }
                
                try
                {
                    allDone.Set();
                }
                catch { }
                
                Console.WriteLine("[INFO] Servidor de socket parado.");
            }
        }

        public delegate void dlgAccept(Client client);
        public delegate void dlgRead(Client client, byte[] buffer, int length);
        public delegate void dlgClose(Client client);

        /// <summary>
        /// Called when a connection is accepted
        /// </summary>
        public event dlgAccept OnAccept;
        /// <summary>
        /// Called when a complete packet is read
        /// </summary>
        public event dlgRead OnRead;
        public event dlgClose OnClose;

        private void Start(object state)
        {
            IPAddress ipAddress;
            int Port = 0;
            if (state.GetType() == typeof(ServerInfo))
            {
                Console.WriteLine("ServerInfo found...");
                ServerInfo info = (ServerInfo)state;
                Port = info.Port;
                ipAddress = info.Host;
            }
            else
            {
                Port = (int)state;

                IPHostEntry hostInfo = Dns.GetHostEntry("localhost");
                ipAddress = hostInfo.AddressList[1];
            }
            
            byte[] bytes = new byte[Client.BUFFER_SIZE];

            IPEndPoint localEP = new IPEndPoint(ipAddress, Port);

            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEP);
                listener.Listen(100);
                listener.NoDelay = true; // Desabilitar algoritmo Nagle para menor latência

                Console.WriteLine($"[INFO] Servidor escutando em {ipAddress}:{Port}");
                isRunning = true;

                while (isRunning)
                {
                    allDone.Reset();

                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    allDone.WaitOne();
                }
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine("[INFO] Thread do servidor abortada.");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[INFO] Socket exception durante shutdown: {ex.Message}");
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("[INFO] Socket disposed durante shutdown.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERRO] Erro crítico no servidor de socket:\n{e}");
            }
            finally
            {
                isRunning = false;
                Console.WriteLine($"[INFO] Thread do servidor finalizada (Porta: {Port}).");
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                allDone.Set();

                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                // Configurar socket do cliente para melhor performance
                handler.NoDelay = true;
                handler.SendBufferSize = 8192;
                handler.ReceiveBufferSize = 8192;

                Console.WriteLine("[INFO] Cliente conectado: {0}", handler.RemoteEndPoint);

                Client state = new Client();
                state.m_socket = handler;

                if (OnAccept != null)
                {
                    OnAccept.BeginInvoke(state, new AsyncCallback(ProcessedAccept), state);
                }
                else
                    handler.BeginReceive(state.buffer, 0, Client.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
            }
            catch (ObjectDisposedException) 
            { 
                // Socket was closed during shutdown - this is expected
            }
            catch (SocketException ex) when (ex.ErrorCode == 995)
            {
                // Operation aborted - this happens when the server is stopped
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[ERRO] Socket error em AcceptCallback: {ex.Message} (Código: {ex.ErrorCode})");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERRO] Exceção inesperada em AcceptCallback:\n{e}");
            }
        }

        private void ProcessedAccept(IAsyncResult ar)
        {
            OnAccept.EndInvoke(ar);

            Client state = (Client)ar.AsyncState;
            Socket handler = state.m_socket;

            try
            {
                handler.BeginReceive(state.buffer, 0, Client.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
            }
            catch (Exception e)
            {
                if (e is ObjectDisposedException || (e is SocketException))
                {
                    if (OnClose != null)
                        OnClose.BeginInvoke(state, new AsyncCallback(EndClose), state);
                }
                else
                    throw;
            }
        }

        private void ReadCallback(IAsyncResult ar)
        {
            Client state = (Client)ar.AsyncState;
            Socket handler = state.m_socket;
            try
            {
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    int len = BitConverter.ToInt16(state.buffer, 0);
                    if (bytesRead != len)
                    {
                        //If the packet is incomplete
                        //Check if there is an incomplete packet in memory
                        if (state.oldBuffer != null && state.oldBuffer.Length != 0)
                        {
                            //And concat the two.
                            byte[] buffer = new byte[bytesRead + state.oldBuffer.Length];
                            Array.Copy(state.oldBuffer, buffer, state.oldBuffer.Length);
                            Array.Copy(state.buffer, 0, buffer, state.oldBuffer.Length, bytesRead);
                            state.buffer = buffer;

                            if (OnRead != null)
                            {
                                byte[] buffer2 = new byte[bytesRead];
                                Array.Copy(state.buffer, buffer2, bytesRead);
                                OnRead.BeginInvoke(state, buffer2, bytesRead, new AsyncCallback(ProcessedRead), null);
                            }
                        }
                        else
                        {
                            //Otherwise, store the received data
                            state.oldBuffer = new byte[state.buffer.Length];
                            state.buffer.CopyTo(state.oldBuffer, 0);

                            //And listen for more.
                            handler.BeginReceive(state.buffer, 0, Client.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
                            return;
                        }
                    }
                    else
                    {
                        if (OnRead != null)
                        {
                            byte[] buffer = new byte[bytesRead];
                            Array.Copy(state.buffer, buffer, bytesRead);
                            OnRead.BeginInvoke(state, buffer, bytesRead, new AsyncCallback(ProcessedRead), null);
                        }
                    }
                    handler.BeginReceive(state.buffer, 0, Client.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
                }
            }
            catch (Exception e)
            {
                if (e is ObjectDisposedException || e is SocketException)
                    if (OnClose != null)
                        OnClose.BeginInvoke(state, new AsyncCallback(EndClose), state);
                    else
                        throw;
            }
        }

        private void ProcessedRead(IAsyncResult ar)
        {
            try
            {
                OnRead.EndInvoke(ar);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Send(Socket handler, byte[] buffer)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            if (buffer == null || buffer.Length == 0)
                throw new ArgumentException("Buffer não pode ser nulo ou vazio.", nameof(buffer));
            
            if (!handler.Connected)
            {
                Console.WriteLine("[AVISO] Tentativa de enviar dados para socket desconectado.");
                return;
            }
            
            try
            {
                handler.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(SendCallback), handler);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Falha ao iniciar envio: {ex.Message}");
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);
                // Logging opcional: Console.WriteLine($"[DEBUG] {bytesSent} bytes enviados.");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[ERRO] Erro de socket ao enviar dados: {ex.Message} (Código: {ex.ErrorCode})");
            }
            catch (ObjectDisposedException)
            {
                // Socket foi fechado - normal durante shutdown
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERRO] Exceção ao enviar dados:\n{e}");
            }
        }

        private void EndClose(IAsyncResult ar)
        {
            try
            {
                Console.WriteLine("[INFO] Uma conexão foi fechada.");
                OnClose.EndInvoke(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Exceção ao finalizar fechamento: {ex.Message}");
            }
        }


        public bool Running
        {
            get
            {
                return isRunning && !isDisposed;
            }
        }
        
        private void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(SocketWrapper));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;
            
            if (disposing)
            {
                // Parar servidor se estiver rodando
                if (isRunning)
                {
                    try
                    {
                        Stop();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERRO] Exceção ao parar servidor durante Dispose: {ex.Message}");
                    }
                }
                
                // Limpar recursos gerenciados
                if (listener != null)
                {
                    try
                    {
                        listener.Close();
                        listener.Dispose();
                    }
                    catch { }
                    finally
                    {
                        listener = null;
                    }
                }
                
                if (allDone != null)
                {
                    try
                    {
                        allDone.Dispose();
                    }
                    catch { }
                }
                
                // Aguardar thread terminar
                if (tWorker != null && tWorker.IsAlive)
                {
                    try
                    {
                        if (!tWorker.Join(TimeSpan.FromSeconds(5)))
                        {
                            Console.WriteLine("[AVISO] Thread do servidor não finalizou em 5 segundos.");
                        }
                    }
                    catch { }
                }
            }
            
            isDisposed = true;
            Console.WriteLine("[INFO] SocketWrapper disposed.");
        }
        
        ~SocketWrapper()
        {
            Dispose(false);
        }
    }
}
