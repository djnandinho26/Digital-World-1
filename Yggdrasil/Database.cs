using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Digital_World.Data;
using Digital_World.Data.Entities;
using Digital_World.Data.Repositories;
using Digital_World.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Digital_World
{
    /// <summary>
    /// Classe SqlDB usando Entity Framework Core
    /// </summary>
    public partial class SqlDB
    {
        private static Repository<Account> _accountRepo = null!;
        private static Repository<Character> _characterRepo = null!;
        private static Repository<DigimonEntity> _digimonRepo = null!;
        private static Repository<Server> _serverRepo = null!;

        /// <summary>
        /// Inicializa o Entity Framework e os repositórios
        /// </summary>
        public static void InitializeEF(string host, string user, string pass, string database)
        {
            try
            {
                DbContextFactory.Initialize(host, user, pass, database);
                
                // Testar conexão
                if (!DbContextFactory.TestConnection(out string errorMessage))
                {
                    MultiLogger.LogServer("[INFO] Falha ao testar conexão: {0}", errorMessage);
                    
                    // Tentar abrir formulário de configuração
                    if (!TryShowConfigurationForm(errorMessage, out host, out user, out pass, out database))
                    {
                        throw new Exception($"Não foi possível conectar ao banco de dados: {errorMessage}");
                    }
                    
                    // Tentar novamente com novas configurações
                    DbContextFactory.Initialize(host, user, pass, database);
                }
                
                using var context = DbContextFactory.CreateDbContext();
                _accountRepo = new Repository<Account>(context);
                _characterRepo = new Repository<Character>(context);
                _digimonRepo = new Repository<DigimonEntity>(context);
                _serverRepo = new Repository<Server>(context);

                MultiLogger.LogServer("[INFO] Database initialized successfully");
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error initializing database: {0}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Tenta abrir o formulário de configuração do banco de dados
        /// </summary>
        private static bool TryShowConfigurationForm(string errorMessage, out string host, out string user, out string pass, out string database)
        {
            string tempHost = string.Empty;
            string tempUser = string.Empty;
            string tempPass = string.Empty;
            string tempDatabase = string.Empty;

            try
            {
                // Executar no thread da UI
                bool result = false;
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var configForm = new Forms.DatabaseConfigForm(errorMessage);
                    result = configForm.ShowDialog() == true;
                    
                    if (result)
                    {
                        tempHost = configForm.Host;
                        tempUser = configForm.Username;
                        tempPass = configForm.Password;
                        tempDatabase = configForm.Database;
                    }
                });

                host = tempHost;
                user = tempUser;
                pass = tempPass;
                database = tempDatabase;
                return result;
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Erro ao abrir formulário de configuração: {0}", ex.Message);
                host = string.Empty;
                user = string.Empty;
                pass = string.Empty;
                database = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// Autentica um usuário - Substitui GetAcct
        /// </summary>
        public static Account? AuthenticateUser(string username, string password)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Account>(context);
                
                var account = repo.FirstOrDefault(a => a.Username == username);
                
                if (account != null && VerifyPassword(password, account.Password))
                {
                    return account;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in AuthenticateUser: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Busca conta por ID - Substitui GetAcctById
        /// </summary>
        public static Account? GetAccountById(uint accountId)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Account>(context);
                return repo.GetById(accountId);
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in GetAccountById: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Busca personagem por nome - Substitui CharExists
        /// </summary>
        public static bool CharacterExists(string charName)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Character>(context);
                return repo.Any(c => c.CharName == charName);
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in CharacterExists: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Busca personagem por ID
        /// </summary>
        public static Character? GetCharacterById(int characterId)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Character>(context);
                return repo.GetById(characterId);
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in GetCharacterById: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Busca personagens de uma conta - Substitui GetCharList
        /// </summary>
        public static System.Collections.Generic.List<Character> GetCharactersByAccountId(uint accountId)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Character>(context);
                return repo.Find(c => c.AccountId == accountId).ToList();
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in GetCharactersByAccountId: {0}", ex.Message);
                return new System.Collections.Generic.List<Character>();
            }
        }

        /// <summary>
        /// Busca Digimon por ID
        /// </summary>
        public static DigimonEntity? GetDigimonById(int digimonId)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<DigimonEntity>(context);
                return repo.GetById(digimonId);
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in GetDigimonById: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Atualiza personagem - Substitui SaveChar
        /// </summary>
        public static bool UpdateCharacter(Character character)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Character>(context);
                repo.Update(character);
                repo.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in UpdateCharacter: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Atualiza Digimon
        /// </summary>
        public static bool UpdateDigimon(DigimonEntity digimon)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<DigimonEntity>(context);
                repo.Update(digimon);
                repo.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in UpdateDigimon: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Deleta personagem e seus Digimons
        /// </summary>
        public static bool DeleteCharacter(uint accountId, int slot)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var accountRepo = new Repository<Account>(context);
                var charRepo = new Repository<Character>(context);
                var digiRepo = new Repository<DigimonEntity>(context);

                var account = accountRepo.GetById(accountId);
                if (account == null) return false;

                // Busca o ID do personagem no slot
                int? charId = slot switch
                {
                    0 => account.Char1,
                    1 => account.Char2,
                    2 => account.Char3,
                    3 => account.Char4,
                    _ => null
                };

                if (charId == null) return false;

                // Deleta Digimons do personagem
                var digimons = digiRepo.Find(d => d.CharacterId == charId.Value).ToList();
                digiRepo.RemoveRange(digimons);

                // Deleta o personagem
                var character = charRepo.GetById(charId.Value);
                if (character != null)
                {
                    charRepo.Remove(character);
                }

                // Atualiza a conta
                switch (slot)
                {
                    case 0: account.Char1 = null; break;
                    case 1: account.Char2 = null; break;
                    case 2: account.Char3 = null; break;
                    case 3: account.Char4 = null; break;
                }
                accountRepo.Update(account);

                charRepo.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in DeleteCharacter: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Cria nova conta - Substitui CreateAcct
        /// </summary>
        public static bool CreateAccount(string username, string password)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Account>(context);

                // Verifica se já existe
                if (repo.Any(a => a.Username == username))
                {
                    return false;
                }

                var account = new Account
                {
                    Username = username,
                    Password = HashPassword(password),
                    Premium = 0,
                    Cash = 0,
                    Silk = 0
                };

                repo.Add(account);
                repo.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in CreateAccount: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Busca lista de servidores
        /// </summary>
        public static System.Collections.Generic.List<Server> GetServerList()
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Server>(context);
                return repo.GetAll().ToList();
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in GetServerList: {0}", ex.Message);
                return new System.Collections.Generic.List<Server>();
            }
        }

        /// <summary>
        /// Atualiza último personagem usado
        /// </summary>
        public static bool UpdateLastChar(uint accountId, int characterId)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Account>(context);
                
                var account = repo.GetById(accountId);
                if (account == null) return false;

                account.LastChar = characterId;
                repo.Update(account);
                repo.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in UpdateLastChar: {0}", ex.Message);
                return false;
            }
        }

        // ===== Métodos de Hash de Senha (mantidos do código original) =====
        
        private static string HashPassword(string password)
        {
            StringBuilder sb = new StringBuilder();

            using (SHA256 shaM = new SHA256Managed())
            {
                byte[] buffer = Encoding.UTF8.GetBytes(password);
                buffer = shaM.ComputeHash(buffer);

                for (int i = 0; i < buffer.Length; i++)
                    sb.Append(buffer[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private static bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }

        // ===== Métodos Adicionais para Database.cs =====

        /// <summary>
        /// Carrega usuário para o cliente - Substitui LoadUser
        /// </summary>
        public static void LoadUserEF(Client client)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Account>(context);
                
                var account = repo.FirstOrDefault(a => a.Username == client.Username);
                
                if (account != null)
                {
                    client.AccessLevel = account.Level;
                    client.AccountID = (uint)account.AccountId;
                    
                    // Gera e atualiza UniqueID
                    var random = new Random();
                    int uniId = random.Next(1, int.MaxValue);
                    
                    account.UniId = (uint)uniId;
                    repo.Update(account);
                    repo.SaveChanges();
                    
                    client.UniqueID = uniId;
                }
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in LoadUserEF: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Carrega usuário por AccountID e UniId - Substitui LoadUser(Client, uint, int)
        /// </summary>
        public static void LoadUserEF(Client client, uint accountId, int uniId)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Account>(context);
                
                var account = repo.FirstOrDefault(a => 
                    a.AccountId == accountId && a.UniId == (uint)uniId);
                
                if (account != null)
                {
                    client.AccessLevel = account.Level;
                    client.AccountID = accountId;
                    client.UniqueID = uniId;
                }
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in LoadUserEF: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Cria novo usuário - Substitui CreateUser
        /// </summary>
        public static bool CreateUserEF(string username, string password)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Account>(context);
                
                // Verifica se usuário já existe
                if (repo.Any(a => a.Username == username))
                {
                    return false;
                }
                
                var newAccount = new Account
                {
                    Username = username,
                    Password = HashPassword(password),
                    Email = "",
                    UniId = 0,
                    Char1 = -1,
                    Char2 = -1,
                    Char3 = -1,
                    Char4 = -1,
                    LastChar = -1,
                    Premium = 0,
                    Cash = 0,
                    Silk = 0,
                    Level = 1
                };
                
                repo.Add(newAccount);
                repo.SaveChanges();
                
                return true;
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in CreateUserEF: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Obtém número de personagens por conta - Substitui GetNumChars
        /// </summary>
        public static int GetNumCharsEF(uint accountId)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Character>(context);
                
                return repo.Count(c => c.AccountId == accountId);
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in GetNumCharsEF: {0}", ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Obtém servidor por ID - Substitui GetServer
        /// </summary>
        public static (int Port, string Ip) GetServerEF(int serverId)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Server>(context);
                
                var server = repo.GetById(serverId);
                
                if (server != null)
                {
                    return (server.Port, server.Ip);
                }
                
                return (6999, "127.0.0.1");
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in GetServerEF: {0}", ex.Message);
                return (6999, "127.0.0.1");
            }
        }

        /// <summary>
        /// Wrapper para compatibilidade com código antigo que usa KeyValuePair
        /// </summary>
        public static KeyValuePair<int, string> GetServer(int serverId)
        {
            var result = GetServerEF(serverId);
            return new KeyValuePair<int, string>(result.Port, result.Ip);
        }

        /// <summary>
        /// Obtém dicionário de servidores - Substitui GetServers
        /// </summary>
        public static Dictionary<int, string> GetServersEF()
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Server>(context);
                
                return repo.GetAll()
                    .ToDictionary(s => s.ServerId, s => s.Name);
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in GetServersEF: {0}", ex.Message);
                return new Dictionary<int, string>();
            }
        }

        /// <summary>
        /// Atualiza UniqueID do cliente
        /// </summary>
        public static void UpdateUniIdEF(uint accountId, int uniId)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Account>(context);
                
                var account = repo.GetById(accountId);
                if (account != null)
                {
                    account.UniId = (uint)uniId;
                    repo.Update(account);
                    repo.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in UpdateUniIdEF: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Conta caracteres em uma conta e atualiza o client
        /// </summary>
        public static void CountCharactersEF(Client client)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Character>(context);
                
                    client.Characters = (byte)repo.Count(c => c.AccountId == client.AccountID);
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in CountCharactersEF: {0}", ex.Message);
                client.Characters = 0;
            }
        }

        /// <summary>
        /// Cria um novo personagem
        /// </summary>
        public static int CreateCharacter(uint accountId, int position, int model, string name, int digiModel)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Character>(context);
                
                var character = new Character
                {
                    AccountId = accountId,
                    CharName = name,
                    Model = (ushort)model,
                    Level = 1,
                    Map = 5300, // Mapa inicial
                    X = 33000,
                    Y = 31000,
                    Hp = 1000,
                    Ds = 1000,
                    Money = 0
                };

                repo.Add(character);
                context.SaveChanges();
                
                MultiLogger.LogServer("[INFO] Character created: {0} (ID: {1})", name, character.CharacterId);
                return character.CharacterId;
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in CreateCharacter: {0}", ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Cria um novo Digimon
        /// </summary>
        public static uint CreateDigimon(uint characterId, string digiName, int digiModel)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<DigimonEntity>(context);
                
                var digimon = new DigimonEntity
                {
                    CharacterId = (int)characterId,
                    DigiName = digiName,
                    DigiModel = (uint)digiModel,
                    Level = 1,
                    Size = 10000,
                    Hp = 500,
                    Ds = 500,
                    Exp = 0,
                    DigiSlot = 1
                };

                repo.Add(digimon);
                context.SaveChanges();
                
                MultiLogger.LogServer("[INFO] Digimon created: {0} (ID: {1})", digiName, digimon.DigimonId);
                return (uint)digimon.DigimonId;
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in CreateDigimon: {0}", ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Define o Digimon parceiro do personagem
        /// </summary>
        public static void SetPartner(int characterId, int digimonId)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Character>(context);
                
                var character = repo.GetById(characterId);
                if (character != null)
                {
                    character.Partner = digimonId;
                    repo.Update(character);
                    context.SaveChanges();
                    MultiLogger.LogServer("[INFO] Partner set: Char {0} -> Digi {1}", characterId, digimonId);
                }
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in SetPartner: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Define o tamer (dono) do Digimon
        /// </summary>
        public static void SetTamer(int characterId, int digimonId)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<DigimonEntity>(context);
                
                var digimon = repo.GetById(digimonId);
                if (digimon != null)
                {
                    digimon.CharacterId = characterId;
                    repo.Update(digimon);
                    context.SaveChanges();
                    MultiLogger.LogServer("[INFO] Tamer set: Digi {0} -> Char {1}", digimonId, characterId);
                }
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in SetTamer: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Verifica código de segurança para exclusão de personagem
        /// </summary>
        public static bool VerifyCode(uint accountId, string code)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Account>(context);
                
                var account = repo.GetById(accountId);
                if (account != null && account.SecondPassword != null)
                {
                    return VerifyPassword(code, account.SecondPassword);
                }
                
                // Se não tem código de segurança, permite exclusão
                return true;
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in VerifyCode: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Valida autenticação completa (compatibilidade com código antigo)
        /// </summary>
        public static int ValidateEF(Client client, string user, string pass)
        {
            int level = 0;
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Account>(context);
                
                var account = repo.FirstOrDefault(a => a.Username == user);

                if (account == null)
                {
                    return -3; // Usuário não encontrado
                }

                if (!VerifyPassword(pass, account.Password))
                {
                    return -2; // Senha incorreta
                }

                level = account.Level;
                client.AccessLevel = account.Level;
                client.Username = user;
                client.AccountID = (uint)account.AccountId;

                // Conta caracteres
                CountCharactersEF(client);
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in ValidateEF: {0}", ex.Message);
                level = -1; // Erro
            }
            return level;
        }

        // ==================== MÉTODOS DE COMPATIBILIDADE COM CÓDIGO ANTIGO ====================
        // Esses métodos mantêm compatibilidade com o código que ainda usa as classes de jogo
        
        /// <summary>
        /// Carrega dados completos do personagem para o jogo
        /// TODO: Implementar conversão completa entre entidade DB e classe de jogo
        /// </summary>
        public static void LoadTamer(Client client)
        {
            try
            {
                MultiLogger.LogServer("[INFO] LoadTamer: Method needs full implementation");
                // TODO: Este método precisa:
                // 1. Carregar Character da DB pelo AccountID
                // 2. Converter para Entities.Character (classe de jogo)
                // 3. Carregar Digimon parceiro e lista
                // 4. Carregar inventário, warehouse, archive (BLOBs)
                // 5. Atribuir a client.Tamer
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in LoadTamer: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Salva dados completos do personagem
        /// TODO: Implementar conversão completa entre classe de jogo e entidade DB
        /// </summary>
        public static void SaveTamer(Client client)
        {
            try
            {
                MultiLogger.LogServer("[INFO] SaveTamer: Method needs full implementation");
                // TODO: Este método precisa:
                // 1. Converter client.Tamer (Entities.Character) para Data.Entities.Character
                // 2. Serializar inventário, warehouse, archive para BLOBs
                // 3. Salvar via UpdateCharacter
                // 4. Salvar Digimon parceiro e lista via SaveDigimon
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in SaveTamer: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Salva apenas posição do personagem
        /// TODO: Implementar
        /// </summary>
        public static void SaveTamerPosition(Client client)
        {
            try
            {
                MultiLogger.LogServer("[INFO] SaveTamerPosition: Method needs full implementation");
                // TODO: Atualizar apenas Map, X, Y do Character
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in SaveTamerPosition: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Carrega Digimon completo do banco
        /// TODO: Implementar conversão entre entidade DB e classe de jogo
        /// </summary>
        public static Entities.Digimon LoadDigimon(uint digimonId)
        {
            try
            {
                MultiLogger.LogServer("[INFO] LoadDigimon: Method needs full implementation");
                // TODO: Este método precisa:
                // 1. Carregar DigimonEntity da DB
                // 2. Converter para Entities.Digimon (classe de jogo)
                // 3. Desserializar evolutions (BLOB)
                return null;
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in LoadDigimon: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Salva Digimon completo no banco
        /// TODO: Implementar conversão entre classe de jogo e entidade DB
        /// </summary>
        public static void SaveDigimon(Entities.Digimon digimon)
        {
            try
            {
                MultiLogger.LogServer("[INFO] SaveDigimon: Method needs full implementation");
                // TODO: Este método precisa:
                // 1. Converter Entities.Digimon para Data.Entities.DigimonEntity
                // 2. Serializar evolutions para BLOB
                // 3. Salvar via UpdateDigimon
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in SaveDigimon: {0}", ex.Message);
            }
        }


        /// <summary>
        /// Wrapper para LoadUser compat�vel com c�digo antigo (Digital World)
        /// </summary>
        public static void LoadUser(Client client, uint accountId, int accessCode)
        {
            LoadUserEF(client, accountId, accessCode);
        }

        /// <summary>
        /// Retorna lista de personagens - Wrapper para compatibilidade  
        /// </summary>
        public static System.Collections.Generic.List<Entities.Character> GetCharacters(uint accountId)
        {
            try
            {
                MultiLogger.LogServer("[INFO] GetCharacters: Method needs full conversion implementation");
                return new System.Collections.Generic.List<Entities.Character>();
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in GetCharacters: {0}", ex.Message);
                return new System.Collections.Generic.List<Entities.Character>();
            }
        }

        /// <summary>
        /// Define �ltimo personagem utilizado
        /// </summary>
        public static void SetLastChar(uint accountId, int characterId)
        {
            UpdateLastChar(accountId, characterId);
        }

        /// <summary>
        /// Retorna posi��o do personagem
        /// </summary>
        public static (uint Map, int X, int Y) GetTamerPosition(int characterId)
        {
            try
            {
                using var context = DbContextFactory.CreateDbContext();
                var repo = new Repository<Data.Entities.Character>(context);
                var character = repo.GetById(characterId);
                
                if (character != null)
                {
                    return (character.Map, character.X, character.Y);
                }
                
                return (5300, 33000, 31000);
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in GetTamerPosition: {0}", ex.Message);
                return (5300, 33000, 31000);
            }
        }

        /// <summary>
        /// Retorna posi��o do personagem como objeto Position (sobrecarga para compatibilidade)
        /// Nota: Implementa��o tempor�ria - precisa buscar o personagem correto por accountId e slot
        /// </summary>
        public static Position GetTamerPosition(uint accountId, int slot)
        {
            try
            {
                // TODO: Implementar busca por accountId e slot quando schema tiver campo slot
                // Por enquanto retorna posi��o padr�o
                MultiLogger.LogServer("[INFO] GetTamerPosition(accountId={0}, slot={1}): Returning default position - needs implementation", accountId, slot);
                return new Position(5300, 33000, 31000);
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in GetTamerPosition: {0}", ex.Message);
                return new Position(5300, 33000, 31000);
            }
        }

        /// <summary>
        /// Cria um Digimon mercen�rio
        /// Assinaturas: (characterId, name, species, level, size, attribute) ou (characterId, name, model, level, size, attribute)
        /// </summary>
        public static uint CreateMercenary(uint characterId, string name, int model, int level, int size, int attribute)
        {
            try
            {
                MultiLogger.LogServer("[INFO] CreateMercenary: Method needs implementation");
                // TODO: Criar Digimon mercenário no banco
                return 0; // Retorna 0 em caso de erro, ID do digimon em sucesso
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in CreateMercenary: {0}", ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Busca Digimon por ID - aceita uint ou int
        /// </summary>
        public static Entities.Digimon GetDigimon(uint digimonId)
        {
            try
            {
                MultiLogger.LogServer("[INFO] GetDigimon: Method needs implementation");
                // TODO: Carregar DigimonEntity e converter para Entities.Digimon
                return null;
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[INFO] Error in GetDigimon: {0}", ex.Message);
                return null;
            }
        }
    }
}