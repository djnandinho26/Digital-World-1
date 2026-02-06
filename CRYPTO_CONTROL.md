# Controle de Criptografia - Digital World Server

## 🎮 Como Ligar/Desligar a Criptografia

### Método 1: Arquivo de Configuração (Mais Fácil)

Edite o arquivo **`encryption.config`** na pasta do servidor:

```
enabled    // Para ATIVAR
disabled   // Para DESATIVAR
```

O servidor carrega automaticamente ao iniciar.

### Método 2: Via Código (Durante Execução)

#### A) Menu Interativo
```csharp
using Digital_World.Tools;

// Mostra menu interativo
CryptoManager.ShowMenu();
```

#### B) Comandos Diretos
```csharp
using Digital_World.Network;

// Ativar
PacketCrypto.EncryptionEnabled = true;

// Desativar
PacketCrypto.EncryptionEnabled = false;

// Alternar (toggle)
PacketCrypto.Toggle();

// Ver status
PacketCrypto.ShowStatus();
```

#### C) Comandos Rápidos
```csharp
using Digital_World.Tools;

CryptoManager.Quick.Enable();   // Ativar
CryptoManager.Quick.Disable();  // Desativar
CryptoManager.Quick.Toggle();   // Alternar
CryptoManager.Quick.Status();   // Status
```

### Método 3: No Auth Server (Exemplo)

Edite o arquivo **Auth Server/DigitalWorldAuth.xaml.cs**:

```csharp
private void Window_Loaded(object sender, RoutedEventArgs e)
{
    // ... código existente ...
    
    // Desativar criptografia para debug
    PacketCrypto.EncryptionEnabled = false;
    
    // Ou ativar para produção
    // PacketCrypto.EncryptionEnabled = true;
}
```

### Método 4: No Lobby Server

Edite **Lobby Server/DigitalWorldLobby.xaml.cs**:

```csharp
private void Window_Loaded(object sender, RoutedEventArgs e)
{
    // ... código existente ...
    
    // Controlar criptografia
    PacketCrypto.ShowStatus();  // Ver status atual
    // PacketCrypto.Toggle();   // Alternar se necessário
}
```

## 📝 Exemplos de Uso

### Exemplo 1: Debug (Desativar Temporariamente)
```csharp
// Desativar para debug
PacketCrypto.EncryptionEnabled = false;
Console.WriteLine("Modo DEBUG - Criptografia desativada");

// ... seu código de debug ...

// Reativar
PacketCrypto.EncryptionEnabled = true;
```

### Exemplo 2: Alternar com Comando
```csharp
// Em algum handler de comando
if (comando == "/togglecrypto")
{
    PacketCrypto.Toggle();
    return "Criptografia alterada!";
}
```

### Exemplo 3: Verificar Antes de Iniciar
```csharp
private void IniciarServidor()
{
    PacketCrypto.ShowStatus();  // Mostra status
    
    if (!PacketCrypto.EncryptionEnabled)
    {
        Console.WriteLine("[AVISO] Servidor iniciando SEM criptografia!");
    }
    
    // ... iniciar servidor ...
}
```

## ⚙️ Configuração Persistente

O sistema salva automaticamente em **`encryption.config`**:

```
enabled    // Criptografia ativada
disabled   // Criptografia desativada
```

### Localização do Arquivo
- Auth Server: pasta do `Auth Server.exe`
- Lobby Server: pasta do `Lobby Server.exe`
- Digital World: pasta do `Digital World.exe`

## 🔄 Sincronização Cliente/Servidor

**IMPORTANTE**: Cliente e servidor devem usar a **mesma configuração**!

### No Servidor (C#)
```csharp
PacketCrypto.EncryptionEnabled = true;
```

### No Cliente (Guilmon.cpp)
```cpp
bool EncryptionEnabled = true;  // Linha 15
```

## 📊 Logs de Status

Quando você altera a criptografia, verá:
```
[CRYPTO] Criptografia: ATIVADA
[CRYPTO] Criptografia: DESATIVADA
[CRYPTO] Config carregada: ATIVADA
[CRYPTO] Config criada: DESATIVADA
```

## ⚠️ Avisos Importantes

1. **Sincronize sempre**: Cliente e servidor devem ter a mesma configuração
2. **Reinicie conexões**: Após alterar, reconecte os clientes
3. **Produção**: Mantenha sempre **ATIVADA** em produção
4. **Debug**: Use **DESATIVADA** apenas para debug local

## 🎯 Recomendações

- ✅ **Produção**: `enabled` (ativado)
- 🔧 **Development**: `disabled` (desativado) para debug fácil
- 🧪 **Testing**: Alterne conforme necessário

## 🛠️ Troubleshooting

### Cliente não conecta
- Verifique se ambos têm a mesma configuração
- Use `PacketCrypto.ShowStatus()` para verificar

### Pacotes corrompidos
- Provavelmente há dessincronização
- Garanta que ambos estão com a mesma configuração

### Como verificar rapidamente
```csharp
// No código do servidor
Console.WriteLine($"Crypto: {PacketCrypto.EncryptionEnabled}");
```

---

**Dica Pro**: Use `encryption.config` para não precisar recompilar!
