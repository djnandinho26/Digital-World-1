using System;
using System.IO;
using System.Security.Cryptography;

namespace Digital_World.Network
{
    /// <summary>
    /// Classe responsável pela criptografia e descriptografia de pacotes
    /// Usa algoritmo XOR customizado com chave dinâmica
    /// </summary>
    public static class PacketCrypto
    {
        // Chave base para criptografia (256 bytes)
        private static readonly byte[] BaseKey = new byte[256];
        
        // Arquivo de configuração
        private static readonly string ConfigFile = "encryption.config";
        
        // Flag para habilitar/desabilitar criptografia
        private static bool _encryptionEnabled = true;
        public static bool EncryptionEnabled 
        { 
            get => _encryptionEnabled;
            set 
            {
                _encryptionEnabled = value;
                SaveConfig();
                Helpers.MultiLogger.LogServer($"[CRYPTO] Criptografia: {(value ? "ATIVADA" : "DESATIVADA")}");
            }
        }

        static PacketCrypto()
        {
            // Gera chave base usando valores pseudo-aleatórios mas fixos
            // Isso permite que servidor e cliente usem a mesma chave
            for (int i = 0; i < 256; i++)
            {
                BaseKey[i] = (byte)((i * 7 + 13) % 256);
            }
            
            // Carrega configuração ao iniciar
            LoadConfig();
        }
        
        /// <summary>
        /// Salva a configuração em arquivo
        /// </summary>
        private static void SaveConfig()
        {
            try
            {
                File.WriteAllText(ConfigFile, _encryptionEnabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                Helpers.MultiLogger.LogServer($"[CRYPTO] Erro ao salvar config: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Carrega a configuração do arquivo
        /// </summary>
        private static void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    string config = File.ReadAllText(ConfigFile).Trim().ToLower();
                    _encryptionEnabled = config == "enabled";
                    Helpers.MultiLogger.LogServer($"[CRYPTO] Config carregada: {(config == "enabled" ? "ATIVADA" : "DESATIVADA")}");
                }
                else
                {
                    // Primeira execução - salva com valor padrão
                    SaveConfig();
                    Helpers.MultiLogger.LogServer($"[CRYPTO] Config criada: {(_encryptionEnabled ? "ATIVADA" : "DESATIVADA")}");
                }
            }
            catch (Exception ex)
            {
                Helpers.MultiLogger.LogServer($"[CRYPTO] Erro ao carregar config: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Alterna entre ativado/desativado
        /// </summary>
        public static void Toggle()
        {
            EncryptionEnabled = !EncryptionEnabled;
        }
        
        /// <summary>
        /// Exibe o status atual da criptografia
        /// </summary>
        public static void ShowStatus()
        {
            Helpers.MultiLogger.LogServer($"================================================");
            Helpers.MultiLogger.LogServer($"  Status da Criptografia de Pacotes");
            Helpers.MultiLogger.LogServer($"================================================");
            Helpers.MultiLogger.LogServer($"  Estado: {(EncryptionEnabled ? "ATIVADA ✓" : "DESATIVADA ✗")}");
            Helpers.MultiLogger.LogServer($"  Arquivo: {ConfigFile}");
            Helpers.MultiLogger.LogServer($"================================================");
        }

        /// <summary>
        /// Inicializa a criptografia com uma chave customizada (opcional)
        /// </summary>
        /// <param name="customKey">Chave customizada de 256 bytes</param>
        public static void Initialize(byte[] customKey = null)
        {
            if (customKey != null && customKey.Length == 256)
            {
                Array.Copy(customKey, BaseKey, 256);
            }
        }

        /// <summary>
        /// Criptografa um pacote usando XOR com chave dinâmica
        /// </summary>
        /// <param name="data">Dados a serem criptografados</param>
        /// <returns>Dados criptografados</returns>
        public static byte[] Encrypt(byte[] data)
        {
            Helpers.MultiLogger.LogServer($"[CRYPTO] Encrypt chamado: Enabled={EncryptionEnabled}, Length={data?.Length ?? 0}");
            
            if (!EncryptionEnabled)
            {
                Helpers.MultiLogger.LogServer("[CRYPTO] Criptografia DESATIVADA - retornando dados originais");
                return data;
            }
                
            if (data == null || data.Length < 4)
            {
                Helpers.MultiLogger.LogServer($"[CRYPTO] Pacote muito pequeno ({data?.Length ?? 0} bytes) - retornando original");
                return data;
            }

            byte[] encrypted = new byte[data.Length];
            Array.Copy(data, encrypted, data.Length);

            // Preserva o tamanho do pacote (primeiros 2 bytes)
            // Criptografa a partir do byte 2
            for (int i = 2; i < encrypted.Length; i++)
            {
                // XOR com chave base rotacionada
                int keyIndex = (i - 2) % 256;
                encrypted[i] = (byte)(encrypted[i] ^ BaseKey[keyIndex]);
                
                // XOR adicional com byte anterior (exceto primeiros 2 bytes)
                if (i > 2)
                {
                    encrypted[i] = (byte)(encrypted[i] ^ encrypted[i - 1]);
                }
            }

            Helpers.MultiLogger.LogServer($"[CRYPTO] Pacote criptografado com sucesso: {data.Length} bytes");
            return encrypted;
        }

        /// <summary>
        /// Descriptografa um pacote
        /// </summary>
        /// <param name="data">Dados criptografados</param>
        /// <returns>Dados descriptografados</returns>
        public static byte[] Decrypt(byte[] data)
        {
            if (!EncryptionEnabled || data == null || data.Length < 4)
                return data;

            byte[] decrypted = new byte[data.Length];
            Array.Copy(data, decrypted, data.Length);

            // Descriptografa DE TRÁS PARA FRENTE (ordem inversa da criptografia)
            // Isso é necessário para desfazer o XOR em cadeia corretamente
            for (int i = decrypted.Length - 1; i >= 2; i--)
            {
                int keyIndex = (i - 2) % 256;
                
                // Desfaz XOR com chave base primeiro
                decrypted[i] = (byte)(decrypted[i] ^ BaseKey[keyIndex]);
                
                // Depois desfaz XOR com byte anterior (ordem inversa da criptografia)
                if (i > 2)
                {
                    decrypted[i] = (byte)(decrypted[i] ^ decrypted[i - 1]);
                }
            }

            return decrypted;
        }

        /// <summary>
        /// Gera uma nova chave aleatória para testes
        /// </summary>
        /// <returns>Chave de 256 bytes</returns>
        public static byte[] GenerateRandomKey()
        {
            byte[] key = new byte[256];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        /// <summary>
        /// Obtém a chave atual (para sincronização com cliente)
        /// </summary>
        /// <returns>Cópia da chave base</returns>
        public static byte[] GetCurrentKey()
        {
            byte[] key = new byte[256];
            Array.Copy(BaseKey, key, 256);
            return key;
        }
    }
}
