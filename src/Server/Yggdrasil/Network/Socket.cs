using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Digital_World.Helpers;

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
                    MultiLogger.LogServer("[AVISO] Servidor ja esta em execucao.");
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
                    MultiLogger.LogServer("[AVISO] Servidor ja esta em execucao.");
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
                MultiLogger.LogServer("[AVISO] Servidor ja esta parado.");
                return;
            }
            
            lock (syncLock)
            {
                isRunning = false;
                
                MultiLogger.LogServer("[INFO] Parando servidor de socket...");
                
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
                        MultiLogger.LogServer($"[ERRO] Ao fechar listener: {ex.Message}");
                    }
                }
                
                try
                {
                    allDone.Set();
                }
                catch { }
                
                MultiLogger.LogServer("[INFO] Servidor de socket parado.");
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
                MultiLogger.LogServer("[INFO] ServerInfo encontrado...");
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
                listener.NoDelay = true; // Desabilitar algoritmo Nagle para menor latencia

                MultiLogger.LogServer($"[INFO] Servidor escutando em {ipAddress}:{Port}");
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
                MultiLogger.LogServer("[INFO] Thread do servidor abortada.");
            }
            catch (SocketException ex)
            {
                MultiLogger.LogServer($"[INFO] Socket exception durante shutdown: {ex.Message}");
            }
            catch (ObjectDisposedException)
            {
                MultiLogger.LogServer("[INFO] Socket disposed durante shutdown.");
            }
            catch (Exception e)
            {
                MultiLogger.LogServer($"[ERRO] Erro critico no servidor de socket:\n{e}");
            }
            finally
            {
                isRunning = false;
                MultiLogger.LogServer($"[INFO] Thread do servidor finalizada (Porta: {Port}).");
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

                MultiLogger.LogServer("[INFO] Cliente conectado: {0}", handler.RemoteEndPoint);

                Client state = new Client();
                state.m_socket = handler;

                if (OnAccept != null)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            OnAccept(state);
                            handler.BeginReceive(state.buffer, 0, Client.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
                        }
                        catch (Exception ex)
                        {
                            MultiLogger.LogServer($"[ERRO] Excecao em OnAccept: {ex.Message}");
                        }
                    });
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
                MultiLogger.LogServer($"[ERRO] Socket error em AcceptCallback: {ex.Message} (Codigo: {ex.ErrorCode})");
            }
            catch (Exception e)
            {
                MultiLogger.LogServer($"[ERRO] Excecao inesperada em AcceptCallback:\n{e}");
            }
        }

        // Método ProcessedAccept removido - agora usando Task.Run no AcceptCallback
        // private void ProcessedAccept(IAsyncResult ar)
        // {
        //     OnAccept.EndInvoke(ar);
        //     ...
        // }

        // Mantido para compatibilidade mas não usado
        private void ProcessedAccept_Obsolete(IAsyncResult ar)
        {
            // Não mais necessário com Task.Run
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
                    {
                        Task.Run(() =>
                        {
                            try { OnClose(state); }
                            catch (Exception ex) { MultiLogger.LogServer($"[ERRO] OnClose: {ex.Message}"); }
                        });
                    }
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
                    // Proteção: Limitar recebimento a 131KB
                    if (state.accumulationBuffer.Length + bytesRead > Client.BUFFER_SIZE)
                    {
                        MultiLogger.LogServer("[AVISO] Buffer excedeu 131KB. Descartando dados excedentes.");
                        state.accumulationBuffer = new byte[0];
                        handler.BeginReceive(state.buffer, 0, Client.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
                        return;
                    }

                    // Descriptografa os dados recebidos
                    byte[] decryptedBuffer = new byte[bytesRead];
                    Array.Copy(state.buffer, decryptedBuffer, bytesRead);
                    decryptedBuffer = PacketCrypto.Decrypt(decryptedBuffer);
                    
                    // Acumula os dados recebidos
                    byte[] newAccumulation = new byte[state.accumulationBuffer.Length + decryptedBuffer.Length];
                    Array.Copy(state.accumulationBuffer, 0, newAccumulation, 0, state.accumulationBuffer.Length);
                    Array.Copy(decryptedBuffer, 0, newAccumulation, state.accumulationBuffer.Length, decryptedBuffer.Length);
                    state.accumulationBuffer = newAccumulation;

                    // Processa múltiplos pacotes
                    ProcessMultiplePackets(state, handler);

                    // Zera o buffer principal para próxima leitura
                    Array.Clear(state.buffer, 0, state.buffer.Length);

                    handler.BeginReceive(state.buffer, 0, Client.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
                    return;
                }
            }
            catch (Exception e)
            {
                if (e is ObjectDisposedException || e is SocketException)
                {
                    if (OnClose != null)
                    {
                        Task.Run(() =>
                        {
                            try { OnClose(state); }
                            catch (Exception ex) { MultiLogger.LogServer($"[ERRO] OnClose: {ex.Message}"); }
                        });
                    }
                }
                else
                {
                    MultiLogger.LogServer($"[ERRO] Exceção em ReadCallback: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Processa múltiplos pacotes do buffer de acumulação
        /// </summary>
        private void ProcessMultiplePackets(Client state, Socket handler)
        {
            int offset = 0;
            
            while (offset < state.accumulationBuffer.Length)
            {
                // Verifica se há bytes suficientes para ler o tamanho do pacote
                if (state.accumulationBuffer.Length - offset < 2)
                {
                    // Dados insuficientes, mantém no buffer para próxima leitura
                    byte[] remaining = new byte[state.accumulationBuffer.Length - offset];
                    Array.Copy(state.accumulationBuffer, offset, remaining, 0, remaining.Length);
                    state.accumulationBuffer = remaining;
                    return;
                }

                // Lê o tamanho do pacote (ushort)
                ushort packetSize = BitConverter.ToUInt16(state.accumulationBuffer, offset);

                // Validação: tamanho do pacote não pode exceder MAX_PACKET_SIZE
                if (packetSize > Client.MAX_PACKET_SIZE || packetSize == 0)
                {
                    MultiLogger.LogServer($"[AVISO] Tamanho de pacote inválido: {packetSize}. Descartando buffer.");
                    state.accumulationBuffer = new byte[0];
                    return;
                }

                // Verifica se o pacote completo está disponível
                if (state.accumulationBuffer.Length - offset < packetSize)
                {
                    // Pacote incompleto, mantém no buffer para próxima leitura
                    byte[] remaining = new byte[state.accumulationBuffer.Length - offset];
                    Array.Copy(state.accumulationBuffer, offset, remaining, 0, remaining.Length);
                    state.accumulationBuffer = remaining;
                    return;
                }

                // Extrai o pacote completo
                byte[] packet = new byte[packetSize];
                Array.Copy(state.accumulationBuffer, offset, packet, 0, packetSize);

                // Processa o pacote
                if (OnRead != null)
                {
                    Task.Run(() =>
                    {
                        try { OnRead(state, packet, packetSize); }
                        catch (Exception ex) { MultiLogger.LogServer($"[ERRO] OnRead: {ex.Message}"); }
                    });
                }

                // Avança o offset
                offset += packetSize;
            }

            // Limpa o buffer de acumulação após processar todos os pacotes
            state.accumulationBuffer = new byte[0];
        }

        // Método ReadCallback_Old mantido para referência
        private void ReadCallback_Old(IAsyncResult ar)
        {
            Client state = (Client)ar.AsyncState;
            Socket handler = state.m_socket;
            try
            {
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // Descriptografa os dados recebidos
                    byte[] decryptedBuffer = new byte[bytesRead];
                    Array.Copy(state.buffer, decryptedBuffer, bytesRead);
                    decryptedBuffer = PacketCrypto.Decrypt(decryptedBuffer);
                    
                    // Garante que o buffer descriptografado tem tamanho suficiente
                    int copyLength = Math.Min(bytesRead, decryptedBuffer.Length);
                    Array.Copy(decryptedBuffer, 0, state.buffer, 0, copyLength);

                    int len = BitConverter.ToInt16(state.buffer, 0);
                    if (bytesRead != len)
                    {
                        //If the packet is incomplete
                        //Check if there is an incomplete packet in memory
                        if (state.oldBuffer != null && state.oldBuffer.Length != 0)
                        {
                            //And concat the two.
                            int actualCopyLength = Math.Min(copyLength, state.buffer.Length);
                            byte[] buffer = new byte[actualCopyLength + state.oldBuffer.Length];
                            Array.Copy(state.oldBuffer, 0, buffer, 0, state.oldBuffer.Length);
                            Array.Copy(state.buffer, 0, buffer, state.oldBuffer.Length, actualCopyLength);
                            state.buffer = buffer;

                            if (OnRead != null)
                            {
                                int readLength = Math.Min(actualCopyLength, state.buffer.Length);
                                byte[] buffer2 = new byte[readLength];
                                Array.Copy(state.buffer, 0, buffer2, 0, readLength);
                                Task.Run(() =>
                                {
                                    try { OnRead(state, buffer2, readLength); }
                                    catch (Exception ex) { MultiLogger.LogServer($"[ERRO] OnRead: {ex.Message}"); }
                                });
                            }
                        }
                        else
                        {
                            //Otherwise, store the received data
                            int storeLength = Math.Min(copyLength, state.buffer.Length);
                            state.oldBuffer = new byte[storeLength];
                            Array.Copy(state.buffer, 0, state.oldBuffer, 0, storeLength);

                            //And listen for more.
                            handler.BeginReceive(state.buffer, 0, Client.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
                            return;
                        }
                    }
                    else
                    {
                        if (OnRead != null)
                        {
                            int actualLength = Math.Min(bytesRead, state.buffer.Length);
                            byte[] buffer = new byte[actualLength];
                            Array.Copy(state.buffer, 0, buffer, 0, actualLength);
                            Task.Run(() =>
                            {
                                try { OnRead(state, buffer, actualLength); }
                                catch (Exception ex) { MultiLogger.LogServer($"[ERRO] OnRead: {ex.Message}"); }
                            });
                        }
                    }
                    handler.BeginReceive(state.buffer, 0, Client.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
                }
            }
            catch (Exception e)
            {
                if (e is ObjectDisposedException || e is SocketException)
                {
                    if (OnClose != null)
                    {
                        Task.Run(() =>
                        {
                            try { OnClose(state); }
                            catch (Exception ex) { MultiLogger.LogServer($"[ERRO] OnClose: {ex.Message}"); }
                        });
                    }
                }
                else
                    throw;
            }
        }

        // Método ProcessedRead removido - não mais necessário com Task.Run
        // private void ProcessedRead(IAsyncResult ar) { ... }

        public void Send(Socket handler, byte[] buffer)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            if (buffer == null || buffer.Length == 0)
                throw new ArgumentException("Buffer nao pode ser nulo ou vazio.", nameof(buffer));
            
            if (!handler.Connected)
            {
                MultiLogger.LogServer("[AVISO] Tentativa de enviar dados para socket desconectado.");
                return;
            }
            
            try
            {
                handler.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(SendCallback), handler);
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer($"[ERRO] Falha ao iniciar envio: {ex.Message}");
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);
                // Logging opcional: MultiLogger.LogServer($"[DEBUG] {bytesSent} bytes enviados.");
            }
            catch (SocketException ex)
            {
                MultiLogger.LogServer($"[ERRO] Erro de socket ao enviar dados: {ex.Message} (C�digo: {ex.ErrorCode})");
            }
            catch (ObjectDisposedException)
            {
                // Socket foi fechado - normal durante shutdown
            }
            catch (Exception e)
            {
                MultiLogger.LogServer($"[ERRO] Excecao ao enviar dados:\n{e}");
            }
        }

        // Método EndClose removido - não mais necessário com Task.Run
        // private void EndClose(IAsyncResult ar) { ... }


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
                        MultiLogger.LogServer($"[ERRO] Excecao ao parar servidor durante Dispose: {ex.Message}");
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
                            MultiLogger.LogServer("[AVISO] Thread do servidor nao finalizou em 5 segundos.");
                        }
                    }
                    catch { }
                }
            }
            
            isDisposed = true;
            MultiLogger.LogServer("[INFO] SocketWrapper disposed.");
        }
        
        ~SocketWrapper()
        {
            Dispose(false);
        }
    }
}

