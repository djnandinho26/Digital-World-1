# Como Usar MultiLogger - Separação de Logs

## Métodos Disponíveis

O `MultiLogger` agora possui dois métodos estáticos para garantir que os logs apareçam no painel correto:

### 1. `MultiLogger.LogServer(string message)`
- **Uso**: Logs dos servidores (**Auth**, **Lobby**, **Game**)
- **Destino**: Painel **esquerdo** (tLogAuth/tLog)
- **Exemplos**: Conexões de clientes, login, autenticação, criação de personagens, etc.

### 2. `MultiLogger.LogWeb(string message)`
- **Uso**: Logs de **HTTP**, **HTTPS** e **FTP**
- **Destino**: Painel **direito** (tLogWeb)
- **Exemplos**: Requisições web, uploads FTP, certificados SSL

---

## ✅ Implementado Automaticamente

Todos os arquivos principais já foram atualizados para usar `MultiLogger`:

### Servidores com Suporte MultiLogger:
- ✅ **Auth Server** - DigitalWorldAuth.xaml.cs
- ✅ **Lobby Server** - DigitalWorldLobby.xaml.cs
- ✅ **Digital World (Game)** - Digital World.xaml.cs

### Arquivos Convertidos:

**Network (LogWeb):**
- ✅ HttpServer.cs - Todos os logs HTTP/HTTPS
- ✅ FtpServer.cs - Todos os logs FTP

**Socket & Database (LogServer):**
- ✅ Socket.cs - Logs de conexão
- ✅ DigimonDB.cs, ItemDB.cs, MapDB.cs, MapPortals.cs, TacticsDB.cs, Evolve.cs, MonsterDB.cs

**PacketLogic (LogServer):**
- ✅ Auth Server\PacketLogic.cs
- ✅ Lobby Server\PacketLogic.cs
- ✅ Digital World\Systems\PacketLogic.cs

---

## Como Foi Implementado

### Auth Server
```csharp
MultiLogger _writer = new MultiLogger(tLogAuth, tLogWeb);
// tLogAuth = painel esquerdo (servidor)
// tLogWeb = painel direito (HTTP/FTP)
```

### Lobby Server & Digital World
```csharp
MultiLogger _writer = new MultiLogger(tLog, tLog);
// Usa um único TextBox (sem separação)
```
