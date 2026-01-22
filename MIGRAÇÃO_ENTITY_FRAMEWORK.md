# Migração para Entity Framework Core

## 📋 Visão Geral
Este documento descreve a migração completa do sistema de banco de dados de consultas SQL diretas (MySql.Data) para Entity Framework Core com Repository Pattern.

## 🏗️ Arquitetura

### Estrutura de Arquivos Criados
```
Yggdrasil/
├── Data/
│   ├── DigitalWorldContext.cs      # DbContext principal
│   ├── DbContextFactory.cs         # Factory pattern para DbContext
│   └── Repository.cs                # Repository genérico
├── Data/Entities/
│   ├── Account.cs                   # Entity para tabela acct
│   ├── Character.cs                 # Entity para tabela chars
│   ├── DigimonEntity.cs            # Entity para tabela digimon
│   └── Server.cs                    # Entity para tabela servers
└── Database-EF.cs                   # Métodos de migração EF Core
```

## 🔄 Mapeamento de Métodos

### Database.cs → Database-EF.cs

| Método Antigo (SQL) | Método Novo (EF Core) | Status |
|---------------------|----------------------|--------|
| `SetInfo()` | `InitializeEF()` | ✅ Completo |
| `Validate()` | `AuthenticateUser()` | ✅ Completo |
| `CreateAcct()` | `CreateAccount()` | ✅ Completo |
| `GetAcct()` | `GetAccountById()` | ✅ Completo |
| `GetServerList()` | `GetServerList()` | ✅ Completo |

### Database - Game.cs → Database-EF.cs

| Método Antigo (SQL) | Método Novo (EF Core) | Status |
|---------------------|----------------------|--------|
| `LoadTamer()` | Usar Repository<Character> | ⏳ Pendente |
| `LoadDigimon()` | `GetDigimonById()` | ✅ Completo |
| `SaveChar()` | `UpdateCharacter()` | ✅ Completo |
| `SaveDigi()` | `UpdateDigimon()` | ✅ Completo |

### Database - Lobby.cs → Database-EF.cs

| Método Antigo (SQL) | Método Novo (EF Core) | Status |
|---------------------|----------------------|--------|
| `NameAvail()` | `CharacterExists()` | ✅ Completo |
| `GetCharacters()` | `GetCharactersByAccountId()` | ✅ Completo |
| `DeleteTamer()` | `DeleteCharacter()` | ✅ Completo |
| `CreateTamer()` | Usar Repository<Character> | ⏳ Pendente |

## 🔧 Como Usar

### 1. Inicialização (em cada servidor)

**Antes (MySql.Data):**
```csharp
SqlDB.SetInfo(host, user, pass, database);
```

**Depois (EF Core):**
```csharp
SqlDB.InitializeEF(host, user, pass, database);
```

### 2. Autenticação de Usuário

**Antes:**
```csharp
int level = SqlDB.Validate(client, username, password);
```

**Depois:**
```csharp
var account = SqlDB.AuthenticateUser(username, password);
if (account != null)
{
    client.AccountID = (uint)account.AccountId;
    client.AccessLevel = account.Level;
    // ... etc
}
```

### 3. Operações com Repository Pattern

**SELECT:**
```csharp
using var context = DbContextFactory.CreateDbContext();
var repository = new Repository<Character>(context);

// Por ID
var character = repository.GetById(characterId);

// Com filtro
var characters = repository.Find(c => c.AccountId == accountId);

// Primeiro resultado
var firstChar = repository.FirstOrDefault(c => c.CharName == "Marcus");
```

**INSERT:**
```csharp
using var context = DbContextFactory.CreateDbContext();
var repository = new Repository<Character>(context);

var newCharacter = new Character
{
    AccountId = accountId,
    CharName = "Marcus",
    Model = 1,
    Level = 1,
    // ... preencher outras propriedades
};

repository.Add(newCharacter);
repository.SaveChanges();
```

**UPDATE:**
```csharp
using var context = DbContextFactory.CreateDbContext();
var repository = new Repository<Character>(context);

var character = repository.GetById(characterId);
if (character != null)
{
    character.Level = 50;
    character.Money = 10000;
    repository.Update(character);
    repository.SaveChanges();
}
```

**DELETE:**
```csharp
using var context = DbContextFactory.CreateDbContext();
var repository = new Repository<Character>(context);

var character = repository.GetById(characterId);
if (character != null)
{
    repository.Remove(character);
    repository.SaveChanges();
}
```

## ⚡ Vantagens do EF Core

1. **Type-Safe**: Sem erros de digitação em nomes de colunas
2. **LINQ**: Queries expressivas e legíveis
3. **Change Tracking**: Detecta automaticamente modificações
4. **Lazy Loading**: Carrega relacionamentos sob demanda
5. **Migrations**: Versionamento do schema do banco
6. **Performance**: Query optimization automático

## 🔍 Queries SQL Comuns → LINQ

### SELECT com WHERE
```csharp
// SQL:
// SELECT * FROM chars WHERE accountId = @id

// LINQ:
var characters = context.Characters
    .Where(c => c.AccountId == accountId)
    .ToList();
```

### SELECT com JOIN
```csharp
// SQL:
// SELECT * FROM chars c
// INNER JOIN acct a ON c.accountId = a.accountId
// WHERE a.username = @user

// LINQ:
var characters = context.Characters
    .Include(c => c.Account)
    .Where(c => c.Account.Username == username)
    .ToList();
```

### UPDATE
```csharp
// SQL:
// UPDATE chars SET level = @level WHERE characterId = @id

// LINQ:
var character = context.Characters.Find(characterId);
character.Level = newLevel;
context.SaveChanges();
```

### DELETE
```csharp
// SQL:
// DELETE FROM chars WHERE characterId = @id

// LINQ:
var character = context.Characters.Find(characterId);
context.Characters.Remove(character);
context.SaveChanges();
```

### COUNT
```csharp
// SQL:
// SELECT COUNT(*) FROM chars WHERE accountId = @id

// LINQ:
var count = context.Characters
    .Count(c => c.AccountId == accountId);
```

### EXISTS
```csharp
// SQL:
// SELECT COUNT(*) FROM chars WHERE charName = @name

// LINQ:
var exists = context.Characters
    .Any(c => c.CharName == name);
```

## 📦 Trabalhando com BLOBs

### Serializar/Deserializar Inventory
```csharp
// Salvar
var character = context.Characters.Find(characterId);
character.Inventory = ItemList.Serialize(itemList);
context.SaveChanges();

// Carregar
var character = context.Characters.Find(characterId);
var itemList = ItemList.Deserialize(character.Inventory);
```

## 🔐 Relacionamentos

### Account → Characters (1:N)
```csharp
// Carregar account com todos os personagens
var account = context.Accounts
    .Include(a => a.Characters)
    .FirstOrDefault(a => a.Username == username);

foreach (var character in account.Characters)
{
    Console.WriteLine(character.CharName);
}
```

### Character → Digimons (1:N)
```csharp
// Carregar personagem com todos os digimons
var character = context.Characters
    .Include(c => c.Digimons)
    .FirstOrDefault(c => c.CharacterId == charId);

foreach (var digimon in character.Digimons)
{
    Console.WriteLine(digimon.DigiName);
}
```

## 🚀 Próximos Passos

1. ✅ Infraestrutura EF Core criada
2. ✅ Entidades mapeadas
3. ✅ DbContext configurado
4. ✅ Repository Pattern implementado
5. ⏳ Migrar métodos restantes de Database.cs
6. ⏳ Migrar métodos restantes de Database - Game.cs
7. ⏳ Migrar métodos restantes de Database - Lobby.cs
8. ⏳ Atualizar chamadas nos servidores (Auth, Lobby, Digital World)
9. ⏳ Testes de integração
10. ⏳ Deploy gradual

## 📝 Notas Importantes

- **Não deletar** os arquivos antigos ainda (Database.cs, Database - Game.cs, Database - Lobby.cs)
- Manter ambos os sistemas funcionando durante a transição
- Testar cada migração antes de remover código antigo
- Usar transações para operações críticas:
  ```csharp
  using var transaction = context.Database.BeginTransaction();
  try
  {
      // operações...
      context.SaveChanges();
      transaction.Commit();
  }
  catch
  {
      transaction.Rollback();
      throw;
  }
  ```

## 🐛 Debugging

### Ver SQL gerado pelo EF Core
```csharp
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

// No DbContextFactory:
optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
```

### Problemas comuns

1. **Connection timeout**: Aumentar timeout no connection string
2. **Lazy loading não funciona**: Usar `.Include()` para eager loading
3. **Tracking errors**: Usar `.AsNoTracking()` para queries read-only

## 📚 Referências

- [EF Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Pomelo MySQL Provider](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- [Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
