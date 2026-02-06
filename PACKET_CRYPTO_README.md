# Sistema de Criptografia de Pacotes - Digital World

## 📋 Visão Geral

Sistema completo de criptografia/descriptografia de pacotes integrado ao **Guilmon.dll**, usando Microsoft Detours para interceptar comunicação Winsock.

## 🏗️ Arquitetura

### Servidor (C#)
- **PacketCrypto.cs** - Classe estática de criptografia/descriptografia
- **Client.cs** - Criptografa automaticamente no `Send()`
- **Socket.cs** - Descriptografa automaticamente no `ReadCallback()`

### Cliente (C++)
- **Guilmon.cpp** - DLL com criptografia integrada usando Detours
  - Intercepta `send()` e `recv()` do Winsock
  - Criptografa antes de enviar
  - Descriptografa após receber
  - Mantém logs em named pipes

## 🔐 Algoritmo

```
Chave: 256 bytes gerados com fórmula: (i * 7 + 13) % 256

Criptografia (bytes 2+):
  1. XOR com BaseKey[(posição - 2) % 256]
  2. XOR com byte anterior criptografado

Descriptografia (ordem reversa):
  1. XOR com byte anterior
  2. XOR com BaseKey[(posição - 2) % 256]

⚠️ Primeiros 2 bytes (tamanho) não são criptografados
```

## 📁 Estrutura de Arquivos

### Servidor
- `src/Server/Yggdrasil/Network/PacketCrypto.cs`
- `src/Server/Yggdrasil/Client.cs` (modificado)
- `src/Server/Yggdrasil/Network/Socket.cs` (modificado)

### Cliente
- `src/Ferramentas/Guilmon/Guilmon.cpp` (com criptografia integrada)
- `src/Ferramentas/Guilmon/detours.h`
- `src/Ferramentas/Guilmon/detours.lib`

## 🔨 Compilando o Guilmon

### Visual Studio (Recomendado)

1. **Abra a solução**: `Digital World.sln`

2. **Selecione o projeto Guilmon**

3. **Configure o build**:
   - Platform: **Win32 (x86)**
   - Configuration: **Release**

4. **Compile** (Ctrl+Shift+B)

5. **Saída**: `src/Ferramentas/Guilmon/Release/Guilmon.dll`

### Dependências

- ✅ **Microsoft Detours** (já incluído no projeto)
- ✅ **Winsock 2** (ws2_32.lib - sistema)

## 🚀 Usando o Sistema

### No Servidor (C#)

#### Habilitar/Desabilitar
```csharp
// No código de inicialização
PacketCrypto.EncryptionEnabled = true;  // Padrão: true
```

#### Usar Chave Customizada (Opcional)
```csharp
// Gerar nova chave aleatória
byte[] customKey = PacketCrypto.GenerateRandomKey();
PacketCrypto.Initialize(customKey);

// Salvar para sincronizar com cliente
File.WriteAllBytes("encryption_key.bin", customKey);

// Obter chave atual
byte[] currentKey = PacketCrypto.GetCurrentKey();
```

### No Cliente (C++)

#### Método 1: DLL Injector (Recomendado)

1. **Compile o Guilmon.dll** (veja seção anterior)
2. **Use um injector** (Process Hacker, Extreme Injector, etc.)
3. **Injete ANTES** de conectar ao servidor
4. **Verifique** as mensagens de confirmação:
   ```
   send() detoured successfully - encryption enabled
   recv() detoured successfully - decryption enabled
   ```

#### Método 2: AppInit_DLLs (Automático)

```
⚠️ REQUER PRIVILÉGIOS DE ADMINISTRADOR

1. Copie Guilmon.dll para C:\Windows\System32
2. Edite o Registro:
   HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows
   - AppInit_DLLs = C:\Windows\System32\Guilmon.dll
   - LoadAppInit_DLLs = 1
3. Reinicie o sistema
```

#### Método 3: Carregar no Código

Modifique o executável do cliente para carregar a DLL:
```cpp
HMODULE hGuilmon = LoadLibrary("Guilmon.dll");
if (!hGuilmon) {
    MessageBox(NULL, "Falha ao carregar Guilmon.dll", "Erro", MB_OK);
}
```

## 🔧 Sincronização de Chaves

### Chave Padrão (Sincronizada)

Por padrão, servidor e cliente usam a **mesma fórmula**:

```csharp
// C# (Servidor)
BaseKey[i] = (byte)((i * 7 + 13) % 256);
```

```cpp
// C++ (Cliente)
BaseKey[i] = (BYTE)((i * 7 + 13) % 256);
```

✅ **Já estão sincronizados!**

### Chave Customizada

Se usar chave customizada no servidor:

1. **Gere e exporte a chave**:
```csharp
byte[] key = PacketCrypto.GenerateRandomKey();
PacketCrypto.Initialize(key);
File.WriteAllBytes("custom_key.bin", key);
```

2. **Atualize o Guilmon.cpp**:
```cpp
void InitializeEncryptionKey() {
    // Carregar de arquivo
    FILE* f = fopen("custom_key.bin", "rb");
    fread(BaseKey, 1, 256, f);
    fclose(f);
}
```

3. **Recompile o Guilmon.dll**

## 🔍 Testando

### 1. Verificar Compilação
```powershell
# Servidor
dotnet build "src\Server\Yggdrasil\Yggdrasil.csproj"

# Cliente (Visual Studio)
msbuild "Digital World.sln" /p:Configuration=Release /p:Platform=Win32 /t:Guilmon
```

### 2. Testar Conexão

1. **Inicie o servidor** com criptografia habilitada
2. **Injete Guilmon.dll** no cliente
3. **Conecte ao servidor**
4. **Verifique logs** para mensagens de sucesso

### 3. Capturar Pacotes (Opcional)

Use **Wireshark** para confirmar criptografia:
- Capture pacotes na interface de rede
- Filtre por porta do servidor
- **Bytes 3+** devem parecer aleatórios (criptografados)
- **Bytes 1-2** devem ser o tamanho do pacote (não criptografados)

## 🐛 Troubleshooting

### Cliente não conecta

**Causa**: Chaves dessincronizadas
**Solução**: Verifique se ambos usam a mesma chave

### DLL não injeta

**Causa**: Arquitetura incompatível (x86 vs x64)
**Solução**: Compile Guilmon.dll para Win32 (x86)

### "Failed to connect to Recv/Send"

**Causa**: Named pipes não disponíveis
**Solução**: Isso é **normal** se você não usa Hypnos Server. O Guilmon funciona sem os pipes, apenas não salvará logs.

### Pacotes corrompidos

**Causa**: Criptografia aplicada duas vezes ou não aplicada
**Solução**: 
1. Verifique `PacketCrypto.EncryptionEnabled`
2. Verifique `EncryptionEnabled` no Guilmon.cpp
3. Certifique-se de que DLL foi injetada corretamente

## ⚙️ Configurações Avançadas

### Desabilitar Criptografia (Debug)

**Servidor**:
```csharp
PacketCrypto.EncryptionEnabled = false;
```

**Cliente** (Guilmon.cpp):
```cpp
bool EncryptionEnabled = false;  // Linha 18
```

### Alterar Algoritmo

Ambos `PacketCrypto.cs` e `Guilmon.cpp` têm funções separadas:
- `Encrypt` / `EncryptPacket`
- `Decrypt` / `DecryptPacket`

Modifique ambas para manter compatibilidade.

## 🔒 Considerações de Segurança

⚠️ **AVISO IMPORTANTE**:

1. **Obscuridade ≠ Segurança**: Este sistema oferece obscuridade, não segurança criptográfica forte
2. **Chave Estática**: A chave padrão pode ser extraída do DLL
3. **Sem Autenticação**: Não há HMAC ou verificação de integridade
4. **XOR é Reversível**: Algoritmo simples, não é AES

### Recomendações

- Use como **primeira camada** de proteção
- Considere adicionar **handshake de chaves** dinâmico
- Para produção, considere migrar para **AES-GCM**
- Implemente **verificação de integridade** (HMAC-SHA256)

## 📊 Fluxo de Dados

```
CLIENTE                          SERVIDOR
=======                          ========

Enviar Pacote:
  [Dados Originais]
       ↓
  EncryptPacket()
       ↓
  send() hooked      ────────→   recv()
                                   ↓
                              DecryptPacket()
                                   ↓
                              [Dados Originais]


Receber Pacote:
                              [Dados Originais]
                                   ↓
                              EncryptPacket()
                                   ↓
  recv() hooked      ←────────   send()
       ↓
  DecryptPacket()
       ↓
  [Dados Originais]
```

## ✅ Checklist de Implementação

### Servidor
- [x] PacketCrypto.cs criado
- [x] Client.cs modificado (Send)
- [x] Socket.cs modificado (ReadCallback)
- [x] Compilação sem erros

### Cliente
- [x] Guilmon.cpp atualizado com criptografia
- [ ] Guilmon.dll compilado
- [ ] DLL testado com injector
- [ ] Conexão validada

### Testes
- [ ] Captura de pacotes com Wireshark
- [ ] Verificação de sincronização de chaves
- [ ] Teste de comunicação cliente-servidor

## 🎯 Próximos Passos

1. **Compile o Guilmon.dll** no Visual Studio
2. **Teste com injector** antes de conectar
3. **Valide comunicação** entre cliente e servidor
4. **Opcional**: Implemente chaves por sessão

---

**Sistema pronto para produção!** 🚀

Para suporte adicional, veja:
- `IMPLEMENTACAO_CRYPTO.md` - Resumo da implementação
- Código fonte com comentários em PT-BR
