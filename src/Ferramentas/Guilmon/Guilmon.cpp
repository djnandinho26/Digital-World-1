#include <windows.h>
#include <stdio.h>
#include "detours.h"

#pragma comment(lib, "ws2_32.lib")
#pragma comment(lib, "detours.lib")

// ============================================
// PACKET ENCRYPTION SYSTEM
// ============================================
// Chave de criptografia (256 bytes) - sincronizada com o servidor
BYTE BaseKey[256];

// Flag para habilitar/desabilitar criptografia
bool EncryptionEnabled = true;

// Console para debug
HANDLE hConsole = NULL;

// Função auxiliar para imprimir dados em hexadecimal
void PrintHex(const char* label, const char* data, int length) {
    if (hConsole) {
        printf("%s (%d bytes): ", label, length);
        for (int i = 0; i < length && i < 64; i++) {  // Limita a 64 bytes para não poluir
            printf("%02X ", (unsigned char)data[i]);
        }
        if (length > 64) printf("...");
        printf("\n");
    }
}

// Inicializa a chave de criptografia (mesmo algoritmo do servidor)
void InitializeEncryptionKey() {
    for (int i = 0; i < 256; i++) {
        BaseKey[i] = (BYTE)((i * 7 + 13) % 256);
    }
}

// Criptografa um pacote (XOR com chave + XOR com byte anterior)
void EncryptPacket(char* data, int length) {
    if (!EncryptionEnabled || length <= 2) return;
    
    // Preserva os primeiros 2 bytes (tamanho do pacote)
    for (int i = 2; i < length; i++) {
        int keyIndex = (i - 2) % 256;
        BYTE original = (BYTE)data[i];
        
        // XOR com a chave
        data[i] = original ^ BaseKey[keyIndex];
        
        // XOR com o byte anterior (criptografado)
        if (i > 2) {
            data[i] ^= (BYTE)data[i - 1];
        }
    }
}

// Descriptografa um pacote (ordem inversa da criptografia)
void DecryptPacket(char* data, int length) {
    if (!EncryptionEnabled || length <= 2) return;
    
    // Preserva os primeiros 2 bytes (tamanho do pacote)
    // Descriptografia deve ser feita de trás para frente por causa do XOR encadeado
    for (int i = length - 1; i >= 2; i--) {
        int keyIndex = (i - 2) % 256;
        
        // XOR com o byte anterior (se não for o primeiro byte de dados)
        if (i > 2) {
            data[i] ^= (BYTE)data[i - 1];
        }
        
        // XOR com a chave
        data[i] ^= BaseKey[keyIndex];
    }
}
// ============================================

int (WINAPI *pSend)(SOCKET s, const char* buf, int len, int flags) = send;
int WINAPI MySend(SOCKET s, const char* buf, int len, int flags);
int (WINAPI *pRecv)(SOCKET s, char* buf, int len, int flags) = recv;
int WINAPI MyRecv(SOCKET s, char* buf, int len, int flags);

int WINAPI MySend(SOCKET s, const char *buf, int len, int flags){
	// Cria uma cópia para criptografar
	char* encryptedBuf = new char[len];
	memcpy(encryptedBuf, buf, len);
	
	// Mostra dados originais
	if (hConsole) {
		printf("\n[SEND] Dados Originais:\n");
		PrintHex("  Original", buf, len);
	}
	
	// Criptografa os dados antes de enviar
	EncryptPacket(encryptedBuf, len);
	
	// Mostra dados criptografados
	if (hConsole) {
		PrintHex("  Encrypted", encryptedBuf, len);
	}
	
	// Envia os dados criptografados
	int result = pSend(s, encryptedBuf, len, flags);
	
	delete[] encryptedBuf;
	return result;
}

int WINAPI MyRecv(SOCKET s, char *buf, int len, int flags){
	// Recebe os dados
	int result = pRecv(s, buf, len, flags);
	
	if (result > 0)
	{
		// Mostra dados criptografados recebidos
		if (hConsole) {
			printf("\n[RECV] Dados Criptografados:\n");
			PrintHex("  Encrypted", buf, result);
		}
		
		// Descriptografa os dados recebidos
		DecryptPacket(buf, result);
		
		// Mostra dados descriptografados
		if (hConsole) {
			PrintHex("  Decrypted", buf, result);
		}
	}
	
	return result;
}

BOOL APIENTRY DllMain(HANDLE hModule, DWORD ul_reason_for_call, LPVOID lpReserved){
	switch(ul_reason_for_call){
		case DLL_PROCESS_ATTACH:
		{
			// Criar console para debug
			AllocConsole();
			hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
			freopen("CONOUT$", "w", stdout);
			freopen("CONOUT$", "w", stderr);
			
			printf("=================================================\n");
			printf("    Guilmon - Digital World Packet Encryption   \n");
			printf("=================================================\n\n");
			
			// Inicializa a chave de criptografia
			InitializeEncryptionKey();
			printf("[+] Chave de criptografia inicializada\n");

			pSend = (int (WINAPI *)(SOCKET, const char*, int, int))	DetourFindFunction("Ws2_32.dll", "send");
			pRecv = (int (WINAPI *)(SOCKET, char*, int, int)) DetourFindFunction("Ws2_32.dll", "recv");

			DetourTransactionBegin();
			DetourUpdateThread(GetCurrentThread());
            DetourAttach(&(PVOID&)pSend, MySend);
            bool sendHooked = (DetourTransactionCommit() == NO_ERROR);
            if(sendHooked)
                printf("[+] send() hooked - encryption enabled\n");
            else
                printf("[-] Falha ao hookar send()\n");
                
            DetourTransactionBegin();
            DetourUpdateThread(GetCurrentThread());
            DetourAttach(&(PVOID&)pRecv, MyRecv);
            bool recvHooked = (DetourTransactionCommit() == NO_ERROR);
            if(recvHooked)
                printf("[+] recv() hooked - decryption enabled\n");
            else
                printf("[-] Falha ao hookar recv()\n");
            
            printf("\n[*] Guilmon ativo! Monitorando pacotes...\n\n");
            
            // Mostrar mensagem de sucesso
            if(sendHooked && recvHooked)
            {
                MessageBox(NULL, 
                    TEXT("Guilmon conectado com sucesso!\n\nCriptografia de pacotes ativa.\n\nConsole aberto para debug."),
                    TEXT("Guilmon - Digital World"),
                    MB_OK | MB_ICONINFORMATION);
            }
            else
            {
                MessageBox(NULL,
                    TEXT("Falha ao conectar Guilmon!\n\nVerifique o console."),
                    TEXT("Guilmon - Erro"),
                    MB_OK | MB_ICONERROR);
            }
            break;
		}

		case DLL_PROCESS_DETACH:
			if (hConsole) {
				printf("\n[*] Guilmon desconectado\n");
			}
			DetourTransactionBegin();
			DetourUpdateThread(GetCurrentThread());
			DetourDetach(&(PVOID&)pSend, MySend);
			DetourDetach(&(PVOID&)pRecv, MyRecv);
			DetourTransactionCommit();
			FreeConsole();
			break;
	}

	return TRUE;
}
