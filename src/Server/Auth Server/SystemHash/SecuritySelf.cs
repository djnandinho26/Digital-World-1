using System;
using System.Text;

namespace SimpleSecurity
{
    /// <summary>
    /// Classe de segurança para criptografia e descriptografia de strings
    /// </summary>
    public static class SecuritySelf
    {
        #region Constantes Privadas

        private const string Charset = "VX#EDC`qax35OL>)P:}\\~!QAZ@|tg,%TGB^YHN&1UrfvSed-[6yc4$9ol.0p; 2/hn'=]WRFJM*IK<(zwsb7uj?_{\"+m8ik";
        private static readonly int CharsetSize = Charset.Length;

        #endregion

        #region Métodos Públicos

        /// <summary>
        /// Criptografa uma mensagem e retorna em formato hexadecimal
        /// </summary>
        public static bool EncryptHex(string msg, out string output)
        {
            if (!Encrypt(msg, out string encrypted))
            {
                output = string.Empty;
                return false;
            }

            output = TextToHex(encrypted);
            return true;
        }

        /// <summary>
        /// Descriptografa uma mensagem em formato hexadecimal
        /// </summary>
        public static bool DecryptHex(string msg, out string output)
        {
            string hexToText = HexToText(msg);
            if (!Decrypt(hexToText, out output))
            {
                output = string.Empty;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Criptografa uma mensagem
        /// </summary>
        public static bool Encrypt(string msg, out string output)
        {
            output = string.Empty;

            string key = MakeSecurityKeyVector();

            if (!ApEncrypt(key, msg, out string msgEncrypted))
            {
                return false;
            }

            output = TypeXor(key + msgEncrypted);
            return true;
        }

        /// <summary>
        /// Descriptografa uma mensagem
        /// </summary>
        public static bool Decrypt(string msg, out string output)
        {
            output = string.Empty;

            string decrypted = TypeXor(msg);

            if (decrypted.Length <= CharsetSize)
            {
                return false;
            }

            string key = decrypted.Substring(0, CharsetSize);
            string msgValue = decrypted.Substring(key.Length);

            if (string.IsNullOrEmpty(msgValue))
            {
                return false;
            }

            if (!ApDecrypt(key, msgValue, out output))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retorna o tamanho necessário para armazenar a chave de segurança
        /// </summary>
        public static int GetSecuritySize(string authKey)
        {
            return authKey.Length + CharsetSize + 1;
        }

        #endregion

        #region Métodos Privados - Criptografia

        private static string TypeXor(string src)
        {
            const byte xorValue = 28;
            StringBuilder dest = new StringBuilder(src.Length);

            foreach (char c in src)
            {
                byte bt = (byte)c;
                bt = (byte)(bt ^ xorValue);
                dest.Append((char)bt);
            }

            return dest.ToString();
        }

        private static int StrPos(string key, char ch)
        {
            for (int i = 0; i < CharsetSize; i++)
            {
                if (ch == key[i])
                {
                    return i;
                }
            }
            return -1;
        }

        private static char Rotate(string key, char ch, int cnt)
        {
            if (cnt == 0)
            {
                return ch;
            }

            int pos = StrPos(key, ch);
            if (pos < 0)
            {
                return '\0';
            }

            pos += cnt;

            while (pos < 0)
            {
                pos += key.Length;
            }

            pos %= key.Length;

            return key[pos];
        }

        private static bool ApEncrypt(string key, string data, out string output)
        {
            output = string.Empty;
            int len = data.Length;
            int sum = 0;

            foreach (char c in data)
            {
                sum += c;
            }

            sum %= key.Length;

            StringBuilder result = new StringBuilder();
            result.Append(key[sum]);

            for (int i = 0; i < len; i++)
            {
                char ch = Rotate(key, data[i], (i + 1) * 13 - (len + result[0]));

                if (ch == '\0')
                {
                    return false;
                }

                result.Append(ch);
            }

            output = result.ToString();
            return true;
        }

        private static bool ApDecrypt(string key, string data, out string output)
        {
            output = string.Empty;
            string udata = data.Substring(1);
            int len = udata.Length;
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < len; i++)
            {
                char ch = Rotate(key, udata[i], (i + 1) * -13 + (len + data[0]));

                if (ch == '\0')
                {
                    return false;
                }

                result.Append(ch);
            }

            output = result.ToString();
            return true;
        }

        private static string MakeSecurityKeyVector()
        {
            Random rand = new Random();
            char[] keyvec = Charset.ToCharArray();

            for (int n = 0; n < CharsetSize; n++)
            {
                int nStart, nEnd;
                do
                {
                    nStart = rand.Next(CharsetSize);
                    nEnd = rand.Next(CharsetSize);
                } while (nStart == nEnd);

                // Swap
                char temp = keyvec[nStart];
                keyvec[nStart] = keyvec[nEnd];
                keyvec[nEnd] = temp;
            }

            return new string(keyvec);
        }

        #endregion

        #region Métodos Privados - Conversão

        private static string TextToHex(string text)
        {
            StringBuilder hex = new StringBuilder(text.Length * 2);

            foreach (char c in text)
            {
                hex.AppendFormat("{0:x2}", (int)c);
            }

            return hex.ToString();
        }

        private static string HexToText(string hex)
        {
            if (hex.Length % 2 != 0)
            {
                return string.Empty;
            }

            StringBuilder text = new StringBuilder(hex.Length / 2);

            for (int i = 0; i < hex.Length / 2; i++)
            {
                string hexPair = hex.Substring(i * 2, 2);
                int num = Convert.ToInt32(hexPair, 16);
                text.Append((char)num);
            }

            return text.ToString();
        }

        #endregion
    }
}
