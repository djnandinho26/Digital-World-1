using System;
using Digital_World.Network;

namespace Digital_World.Tools
{
    /// <summary>
    /// Ferramenta para gerenciar criptografia de pacotes via console
    /// </summary>
    public static class CryptoManager
    {
        /// <summary>
        /// Exibe menu interativo para gerenciar criptografia
        /// </summary>
        public static void ShowMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("╔════════════════════════════════════════════════╗");
                Console.WriteLine("║   Gerenciador de Criptografia de Pacotes      ║");
                Console.WriteLine("╚════════════════════════════════════════════════╝");
                Console.WriteLine();
                Console.WriteLine($"  Status: {(PacketCrypto.EncryptionEnabled ? "[ATIVADA ✓]" : "[DESATIVADA ✗]")}");
                Console.WriteLine();
                Console.WriteLine("  1 - Ativar criptografia");
                Console.WriteLine("  2 - Desativar criptografia");
                Console.WriteLine("  3 - Alternar (Toggle)");
                Console.WriteLine("  4 - Ver status");
                Console.WriteLine("  0 - Sair");
                Console.WriteLine();
                Console.Write("Escolha uma opção: ");

                string input = Console.ReadLine();
                Console.WriteLine();

                switch (input)
                {
                    case "1":
                        PacketCrypto.EncryptionEnabled = true;
                        Console.WriteLine("[✓] Criptografia ATIVADA");
                        break;
                    case "2":
                        PacketCrypto.EncryptionEnabled = false;
                        Console.WriteLine("[✗] Criptografia DESATIVADA");
                        break;
                    case "3":
                        PacketCrypto.Toggle();
                        Console.WriteLine($"[↔] Alterado para: {(PacketCrypto.EncryptionEnabled ? "ATIVADA" : "DESATIVADA")}");
                        break;
                    case "4":
                        PacketCrypto.ShowStatus();
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("[!] Opção inválida");
                        break;
                }

                if (input != "0")
                {
                    Console.WriteLine();
                    Console.WriteLine("Pressione qualquer tecla para continuar...");
                    Console.ReadKey();
                }
            }
        }

        /// <summary>
        /// Ativa criptografia e loga
        /// </summary>
        public static void EnableEncryption()
        {
            PacketCrypto.EncryptionEnabled = true;
            Helpers.MultiLogger.LogServer("[INFO] Criptografia de pacotes ATIVADA");
        }

        /// <summary>
        /// Desativa criptografia e loga
        /// </summary>
        public static void DisableEncryption()
        {
            PacketCrypto.EncryptionEnabled = false;
            Helpers.MultiLogger.LogServer("[AVISO] Criptografia de pacotes DESATIVADA");
        }

        /// <summary>
        /// Retorna status da criptografia
        /// </summary>
        public static bool IsEnabled => PacketCrypto.EncryptionEnabled;

        /// <summary>
        /// Comandos rápidos para uso em código
        /// </summary>
        public static class Quick
        {
            public static void Enable()
            {
                PacketCrypto.EncryptionEnabled = true;
            }

            public static void Disable()
            {
                PacketCrypto.EncryptionEnabled = false;
            }

            public static void Toggle()
            {
                PacketCrypto.Toggle();
            }

            public static void Status()
            {
                PacketCrypto.ShowStatus();
            }
        }
    }
}
