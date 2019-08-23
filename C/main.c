#include <windows.h>
#include <stdlib.h>
#include <stdio.h>
#include <stdint.h>
#include <string.h>
#include "SHA1/hmac.h"
#include "TripleDES\des3.h"
#include "TripleDES\cbc.h"

#pragma pack(push, 1)
typedef struct {
	unsigned long long XUID;
	unsigned int unknown; //reserved (assume PARTNER.NET accounts)
	char Gamertag[0x10]; //must null term, verification fails if last byte not zero
	unsigned int Flags; //last 4 bits reserved, verification fails if they're set
	unsigned char Passcode[4];
	char Domain[0x14]; //must null term, verification fails if last byte not zero
	char Realm[0x18]; //must null term, verification fails if last byte not zero
	unsigned char Confounder[0x14];
	unsigned char Verification[8];
} ONLINE_USER_ACCOUNT_STRUCT;
#pragma pack(pop)

int SignOnlineUserSignature(ONLINE_USER_ACCOUNT_STRUCT* Account);
int VerifyOnlineUserSignature(ONLINE_USER_ACCOUNT_STRUCT* Account);
void XCryptHMAC(unsigned char* pbKeyMaterial, int cbKeyMaterial, unsigned char* pbData, int cbData, unsigned char* pbData2, int cbData2, unsigned char* pbDigest);
int XCryptBlockCryptCBC(unsigned char* Key, int cbKey, unsigned char* IV, unsigned char* Input, int cbInput, unsigned char* Output, CipherDirection Flag);

//simple rng for testing
unsigned int Xorshift(unsigned int* seed)
{
	unsigned int x = *(unsigned int*)seed;
	x ^= x << 13;
	x ^= x >> 17;
	x ^= x << 5;
	*(unsigned int*)seed = x;
	return x;
}

int main()
{

	//generate, sign, and verify original xbox live account data

	ONLINE_USER_ACCOUNT_STRUCT* account = (ONLINE_USER_ACCOUNT_STRUCT*)calloc(1, sizeof(ONLINE_USER_ACCOUNT_STRUCT));

	unsigned int random = GetTickCount();

	//generate XUID
	*(unsigned int*)(&account->XUID) = Xorshift(&random);
	*(unsigned short*)((unsigned char*)(&account->XUID) + 4) = Xorshift(&random);
	*(unsigned short*)((unsigned char*)(&account->XUID) + 6) = 9;

	//gamertag
	memcpy(account->Gamertag, &"Test Gamertag", 13);

	//domain
	memcpy(account->Domain, &"xbox.com", 8);

	//realm
	memcpy(account->Realm, &"PASSPORT.NET", 12);

	//generate confounder
	*(unsigned int*)(&account->Confounder) = Xorshift(&random);
	*(unsigned int*)((unsigned char*)(&account->Confounder) + 4) = Xorshift(&random);
	*(unsigned int*)((unsigned char*)(&account->Confounder) + 8) = Xorshift(&random);
	*(unsigned int*)((unsigned char*)(&account->Confounder) + 12) = Xorshift(&random);
	*(unsigned int*)((unsigned char*)(&account->Confounder) + 16) = Xorshift(&random);

	//sign/encrypt account data
	if (SignOnlineUserSignature(account)) {

		//decrypt/verify account data
		if (VerifyOnlineUserSignature(account)) {

			//write out to disk
			/*
			FILE* handle = NULL;
			fopen_s(&handle, "C:\\xboxlive_test_account.bin", "wb+");
			if (handle)
			{
				if (account)
				{
					fwrite(account, 1, sizeof(ONLINE_USER_ACCOUNT_STRUCT), handle);
					fflush(handle);
				}
				fclose(handle);
			}
			*/
			printf("Account created successfully");
		}
		else printf("Failed to verify account data");

	}
	else printf("Failed to sign account data");

	free(account);

}

int SignOnlineUserSignature(ONLINE_USER_ACCOUNT_STRUCT* Account)
{
	int result = 0;

	unsigned char* seed_data = (unsigned char[]) { 0xA7, 0x14, 0x21, 0x3D, 0x94, 0x46, 0x1E, 0x05, 0x97, 0x6D, 0xE8, 0x35, 0x21, 0x2A, 0xE5, 0x7C };
	unsigned char* seed_key_a = (unsigned char[]) { 0x2B, 0xB8, 0xD9, 0xEF, 0xD2, 0x04, 0x6D, 0x9D, 0x1F, 0x39, 0xB1, 0x5B, 0x46, 0x58, 0x01, 0xD7 };
	unsigned char* seed_key_b = (unsigned char[]) { 0x1E, 0x05, 0xD7, 0x3A, 0xA4, 0x20, 0x6A, 0x7B, 0xA0, 0x5B, 0xCD, 0xDF, 0xAD, 0x26, 0xD3, 0xDE };
	unsigned char* auth_key = (unsigned char[]) { 0x62, 0xBD, 0x92, 0xB6, 0x4F, 0x45, 0x84, 0x70, 0xD3, 0xFF, 0x4F, 0x22, 0x3C, 0x6E, 0xE7, 0xEA };
	unsigned char* IV = (unsigned char[]) { 0x7B, 0x35, 0xA8, 0xB7, 0x27, 0xED, 0x43, 0x7A };

	//generate tripledes key (3DES)
	//NOTE: for a "machine signed" account, replace seed_data with XboxHDKey (machine signed will make the account 'non-roamable')
	unsigned char* Key = (unsigned char*)malloc(0x18);
	XCryptHMAC(seed_key_a, 0x10, seed_data, 0x10, 0, 0, Key); //store first 4 bytes of resulting hash
	XCryptHMAC(seed_key_b, 0x10, seed_data, 0x10, 0, 0, Key + 0x4); //store entire hash result, beginning from 0x4 in the key

	/*
	  possible to use pre-computed key if not 'machine signing' to save on cpu cycles
	  0x2B, 0x84, 0x95, 0xE8, 0x82, 0xE2, 0xA3, 0x33, 0x30, 0x60, 0x6D, 0x8A, 0xDA, 0x8B, 0x26, 0x93, 0x4E, 0x3A, 0x9D, 0xF6, 0xF5, 0xB8, 0xFA


	  below is the resulting 3DES key layout // Ax = seed_key_a hash result bytes // Bx = seed_key_b hash result bytes
	  0  1  2  3  4  5  6  7  8  9  10  11  12  13  14  15  16  17  19  20  21  22  23  24  // INDEX
	  A0  A1  A2  A3 B0 B1 B2 B3 B4 B5  B6  B7  B8  B9  B10 B11 B12 B13 B14 B15 B16 B17 B18 // 3DES KEY LAYOUT
	*/

	//compute hash of first 0x64 (100) bytes of account data using auth_key, covering all account variables and the confounder
	//NOTE: for a "machine signed" account, replace auth_key with XboxHDKey
	unsigned char* auth_hash = (unsigned char*)malloc(0x14);
	XCryptHMAC(auth_key, 0x10, (unsigned char*)Account, 0x64, 0, 0, auth_hash);

	//encrypt the first 0x10 bytes of confounder (confounder is 0x14 bytes total) in the account data
	if (XCryptBlockCryptCBC(Key, 0x18, IV, (unsigned char*)Account + 0x50, 0x10, (unsigned char*)Account + 0x50, ENCRYPT) == 0)
	{
		//store first 8 bytes of the resulting hash computed over the account data and decrypted confounder, used for authenticating the account
		memcpy((unsigned char*)Account + 0x64, auth_hash, 8);

		result = 1; //success
	}

	free(Key);
	free(auth_hash);
	return result; //0 = failed to encrypt
}

int VerifyOnlineUserSignature(ONLINE_USER_ACCOUNT_STRUCT* Account)
{
	int result = 0;

	unsigned char* seed_data = (unsigned char[]) { 0xA7, 0x14, 0x21, 0x3D, 0x94, 0x46, 0x1E, 0x05, 0x97, 0x6D, 0xE8, 0x35, 0x21, 0x2A, 0xE5, 0x7C };
	unsigned char* seed_key_a = (unsigned char[]) { 0x2B, 0xB8, 0xD9, 0xEF, 0xD2, 0x04, 0x6D, 0x9D, 0x1F, 0x39, 0xB1, 0x5B, 0x46, 0x58, 0x01, 0xD7 };
	unsigned char* seed_key_b = (unsigned char[]) { 0x1E, 0x05, 0xD7, 0x3A, 0xA4, 0x20, 0x6A, 0x7B, 0xA0, 0x5B, 0xCD, 0xDF, 0xAD, 0x26, 0xD3, 0xDE };
	unsigned char* auth_key = (unsigned char[]) { 0x62, 0xBD, 0x92, 0xB6, 0x4F, 0x45, 0x84, 0x70, 0xD3, 0xFF, 0x4F, 0x22, 0x3C, 0x6E, 0xE7, 0xEA };
	unsigned char* IV = (unsigned char[]) { 0x7B, 0x35, 0xA8, 0xB7, 0x27, 0xED, 0x43, 0x7A };

	//generate tripledes key (3DES)
	//NOTE: for a "machine signed" account, replace seed_data with XboxHDKey (machine signed will make the account 'non-roamable')
	unsigned char* Key = (unsigned char*)malloc(0x18);
	XCryptHMAC(seed_key_a, 0x10, seed_data, 0x10, 0, 0, Key); //store first 4 bytes of resulting hash
	XCryptHMAC(seed_key_b, 0x10, seed_data, 0x10, 0, 0, Key + 4); //store entire hash result, beginning from 0x4 in the key

	/*
	  possible to use pre-computed key if not 'machine signing' to save on cpu cycles
	  0x2B, 0x84, 0x95, 0xE8, 0x82, 0xE2, 0xA3, 0x33, 0x30, 0x60, 0x6D, 0x8A, 0xDA, 0x8B, 0x26, 0x93, 0x4E, 0x3A, 0x9D, 0xF6, 0xF5, 0xB8, 0xFA


	  below is the resulting 3DES key layout // Ax = seed_key_a hash result bytes // Bx = seed_key_b hash result bytes
	  0  1  2  3  4  5  6  7  8  9  10  11  12  13  14  15  16  17  19  20  21  22  23  24  // INDEX
	  A0  A1  A2  A3 B0 B1 B2 B3 B4 B5  B6  B7  B8  B9  B10 B11 B12 B13 B14 B15 B16 B17 B18 // 3DES KEY LAYOUT
	*/

	//decrypt the first 0x10 bytes of confounder (confounder is 0x14 bytes total) in the account data
	if (XCryptBlockCryptCBC(Key, 24, IV, (unsigned char*)Account + 80, 0x10, (unsigned char*)Account + 80, DECRYPT) == 0)
	{
		//compute hash of first 0x64 (100) bytes of account data using auth_key, covering all account variables and the confounder
		//NOTE: for a "machine signed" account, replace auth_key with XboxHDKey
		unsigned char* auth_hash = (unsigned char*)malloc(0x14);
		XCryptHMAC(auth_key, 0x10, (unsigned char*)Account, 100, 0, 0, auth_hash);

		//compare first 8 bytes of the resulting auth_hash to the 8 stored bytes of the validation bytes (aka the 8 bytes of the last auth_hash)
		if (memcmp(auth_hash, (unsigned char*)Account + 0x64, 8) == 0)
		{
			//other validation checks, these checks are done by the XOnline API and must comply to be considered valid account data

			//check flags, last 4 bits are resevered and must be zero
			if ((((unsigned char*)Account)[0x1F] & 0xF) == 0)
			{
				//check unknown int value stored at 0x8, must be zero (reserved for PARTNER.NET accounts?)
				if ((unsigned int)*((unsigned char*)Account + 8) == 0)
				{
					//check gamertag for overflow and is null-terminated
					if (((unsigned char*)Account)[0x1B] == 0)
					{
						//check domain for overflow and is null-terminated
						if (((unsigned char*)Account)[0x37] == 0)
						{
							//check realm for overflow and is null-terminated
							if (((unsigned char*)Account)[0x4F] == 0)
							{
								result = 1; //valid account
							}
						}
					}
				}
			}

		}

		free(auth_hash);
	}

	free(Key);
	return result; //0 = failed decrypt, auth_hash was invalid, or additional data checks were invalid
}

void XCryptHMAC(unsigned char* pbKeyMaterial, int cbKeyMaterial, unsigned char* pbData, int cbData, unsigned char* pbData2, int cbData2, unsigned char* pbDigest)
{
	//ignore second data input, its not used but kept original function signature^
	hmac_sha1(pbKeyMaterial, cbKeyMaterial, pbData, cbData, pbDigest);
}

//result != 0 == error
int XCryptBlockCryptCBC(unsigned char* Key, int cbKey, unsigned char* IV, unsigned char* Input, int cbInput, unsigned char* Output, CipherDirection Flag)
{
	//tripledes only (not 1:1 to the xbox code but wasn't nessasary for my purpose anyway)
	int result = -1;
	Des3Context* context = (Des3Context*)malloc(sizeof(Des3Context));
	CipherAlgo cipher = des3CipherAlgo;
	cipher.init(context, Key, cbKey);
	if (Flag == ENCRYPT)
		result = cbcEncrypt(&cipher, context, IV, Input, cbInput, Output);
	else
		result = cbcDecrypt(&cipher, context, IV, Input, cbInput, Output);
	free(context);
	return result;
}

#pragma region original xonline code

//int CXo::VerifyOnlineUserSignature(struct _XC_ONLINE_USER_ACCOUNT_STRUCT *Account, unsigned char* a2)
//{
//	int v2; // edx@2
//	int result; // eax@5
//	unsigned __int8 *v4; // [sp-1Ch] [bp-1DCh]@2
//	int v5; // [sp-14h] [bp-1D4h]@2
//	unsigned __int8 *v6; // [sp-4h] [bp-1C4h]@2
//	unsigned __int8 *KeyTable; // [sp+Ch] [bp-1B4h]@2
//	unsigned __int8 v8; // [sp+18Ch] [bp-34h]@2
//	unsigned __int8 pbDigest; // [sp+1A0h] [bp-20h]@2
//	unsigned __int8 v10; // [sp+1A4h] [bp-1Ch]@2
//	int v11; // [sp+1B8h] [bp-8h]@2
//	int v12; // [sp+1BCh] [bp-4h]@2
//
//	if (a2)
//	{
//		v2 = *(_DWORD *)a2;
//		v12 = *(_DWORD *)(a2 + 4);
//		v11 = v2;
//		XcHMAC(&byte_14BA94, 0x10, XboxHDKey, 0x10, 0, 0, &pbDigest);
//		XcHMAC(&byte_14BA80, 0x10, XboxHDKey, 0x10, 0, 0, &v10);
//		XcDESKeyParity((int)&pbDigest, 24);
//		XcKeyTable(1, &KeyTable, &pbDigest);
//		XcBlockCryptCBC(1, 0x10, Account + 80, Account + 80, (int)&KeyTable, 0, (int)&v11);
//		v6 = &v8;
//		v5 = Account;
//		v4 = XboxHDKey;
//	}
//	else
//	{
//		v11 = dword_14B9A8;
//		v12 = dword_14B9AC;
//		XcHMAC(&byte_14BA94, 0x10, &byte_14BAA8, 0x10, 0, 0, &pbDigest);
//		XcHMAC(&byte_14BA80, 0x10, &byte_14BAA8, 0x10, 0, 0, &v10);
//		XcDESKeyParity((int)&pbDigest, 24);
//		XcKeyTable(1, &KeyTable, &pbDigest);
//		XcBlockCryptCBC(1, 0x10, Account + 0x50, Account + 0x50, (int)&KeyTable, 0, (int)&v11);
//		v6 = &v8;
//		v5 = Account;
//		v4 = &byte_14BABC;
//	}
//	XcHMAC(v4, 0x10, (unsigned __int8 *)v5, 100, 0, 0, v6);
//	if (memcmp((const void *)(Account + 100), &v8, 8u)
//		|| *(_BYTE *)(Account + 31) & 0xF0
//		|| *(_DWORD *)(Account + 8)
//		|| *(_BYTE *)(Account + 27)
//		|| *(_BYTE *)(Account + 55))
//		result = 0;
//	else
//		result = *(_BYTE *)(Account + 79) == 0;
//	return result;
//}
//
//int CXo::SignOnlineUserStruct(struct _XC_ONLINE_USER_ACCOUNT_STRUCT *Account, unsigned char* a2)
//{
//	int v2; // edx@2
//	int result; // eax@4
//	unsigned __int8 *v4; // [sp-14h] [bp-1D4h]@2
//	unsigned __int8 *v5; // [sp-4h] [bp-1C4h]@2
//	char v6; // [sp+Ch] [bp-1B4h]@4
//	unsigned __int8 pbDigest[4]; // [sp+18Ch] [bp-34h]@2
//	int v8; // [sp+190h] [bp-30h]@4
//	unsigned __int8 v9; // [sp+1A0h] [bp-20h]@2
//	unsigned __int8 v10; // [sp+1A4h] [bp-1Ch]@2
//	int v11; // [sp+1B8h] [bp-8h]@2
//	int v12; // [sp+1BCh] [bp-4h]@2
//
//	if (a2)
//	{
//		XcHMAC(XboxHDKey, 16, (unsigned char*)Account, 100, 0, 0, pbDigest);
//		v2 = *(DWORD *)a2;
//		v12 = *((DWORD *)a2 + 1);
//		v11 = v2;
//		XcHMAC(&byte_14BA94, 16, XboxHDKey, 16, 0, 0, &v9);
//		v5 = &v10;
//		v4 = XboxHDKey;
//	}
//	else
//	{
//		XcHMAC(&byte_14BABC, 16, (unsigned char*)Account, 100, 0, 0, pbDigest);
//		v11 = dword_14B9A8;
//		v12 = dword_14B9AC;
//		XcHMAC(&byte_14BA94, 16, &byte_14BAA8, 16, 0, 0, &v9);
//		v5 = &v10;
//		v4 = &byte_14BAA8;
//	}
//	XcHMAC(&byte_14BA80, 16, v4, 16, 0, 0, v5);
//	XcDESKeyParity((int)&v9, 24);
//	XcKeyTable(1, &v6, &v9);
//	XcBlockCryptCBC(1, 16, (int)((char *)Account + 80), (int)((char *)Account + 80), (int)&v6, 1, (int)&v11);
//	*((DWORD *)Account + 25) = *(DWORD *)pbDigest;
//	result = v8;
//	*((DWORD *)Account + 26) = v8;
//	return result;
//}

#pragma endregion

#pragma region original kernel code

//void XCryptDESKeyParity(unsigned char* pbKey, unsigned int dwKeyLength)
//{
//	desparityonkey(pbKey, dwKeyLength);
//}
//
//void XCryptHMAC(unsigned char* pbKeyMaterial, int cbKeyMaterial, unsigned char* pbData, int cbData, unsigned char* pbData2, int cbData2, unsigned char* pbDigest)
//{
//	unsigned char HMACTmp[84];
//	A_SHA_CTX shaHash;
//	unsigned char Kipad[64];
//	unsigned char Kopad[64];
//
//	int count = cbKeyMaterial;
//	if (cbKeyMaterial > 0x40) 
//		count = 64;
//	memset(Kipad, 0, sizeof(Kipad));
//	memcpy(Kipad, pbKeyMaterial, count);
//	memset(Kopad, 0, sizeof(Kopad));
//	memcpy(Kopad, pbKeyMaterial, count);
//	count = 0;
//	do
//	{
//		*(DWORD *)&Kipad[count] ^= 0x36363636u;
//		*(DWORD *)&Kopad[count] ^= 0x5C5C5C5Cu;
//		count += 4;
//	} while (count < 0x40);
//	A_SHAInit(&shaHash);
//	A_SHAUpdate(&shaHash, (int *)Kipad, 64);
//	if (cbData)
//		A_SHAUpdate(&shaHash, (int *)pbData, cbData);
//	if (cbData2)
//		A_SHAUpdate(&shaHash, (int *)pbData2, cbData2);
//	A_SHAFinal(&shaHash, &HMACTmp[64]);
//	memcpy(HMACTmp, Kopad, 0x40u);
//	A_SHAInit(&shaHash);
//	A_SHAUpdate(&shaHash, (int *)HMACTmp, 84);
//	A_SHAFinal(&shaHash, pbDigest);
//}
//
//void XCryptKeyTable(unsigned int dwCipher, unsigned char* pbKeyTable, unsigned char* pbKey)
//{
//	if (dwCipher)
//		tripledes3key((DES3TABLE*)pbKeyTable, pbKey);
//	else
//		deskey((DESTable*)pbKeyTable, pbKey);
//}
//
//void XCryptBlockCryptCBC(unsigned int dwCipher, unsigned int dwInputLength, char* pbOutput, char* pbInput, char* pbKeyTable, unsigned int dwOp, char* pbFeedback)
//{
//	char* inputBuffer = pbInput;
//	void* cipher = des;
//	if (dwCipher) 
//		cipher = tripledes;
//	while (inputBuffer < &pbInput[dwInputLength])
//	{
//		CBC((void(__stdcall*)(unsigned char*, unsigned char*, void*, int))cipher,
//			8u,
//			pbOutput,
//			inputBuffer,
//			pbKeyTable,
//			dwOp,
//			pbFeedback);
//		inputBuffer += 8;
//		pbOutput += 8;
//	}
//}
//
//void desparityonkey(char *pbKey, unsigned int cbKey)
//{
//	unsigned int v2;
//	char v3;
//	unsigned int v4;
//	v2 = 0;
//	if (cbKey)
//	{
//		do
//		{
//			v4 = ((unsigned __int8)DESParityTable[pbKey[v2] & 0xF]
//				+ (unsigned __int8)DESParityTable[(unsigned int)(unsigned __int8)pbKey[v2] >> 4]) & 0x80000001;
//			v3 = v4 == 0;
//			if ((v4 & 0x80000000) != 0)
//				v3 = (((BYTE)v4 - 1) | 0xFFFFFFFE) == -1;
//			if (v3)
//				pbKey[v2] ^= 1u;
//			++v2;
//		} while (v2 < cbKey);
//	}
//}
//
//void CBC(void(__stdcall *Cipher)(char *, char *, void *, int), unsigned int dwBlockLen, char *output, char *input, void *keyTable, int op, char *feedback)
//{
//	unsigned int v7; // ebp@1
//	__int64 *v8; // eax@2
//	char *v9; // ebx@2
//	int v10; // ecx@4
//	char *v11; // ecx@11
//	signed int v12; // ebp@12
//	int v13; // esi@15
//	int v14; // eax@15
//	char *v15; // eax@15
//	int v16; // ecx@17
//	char *v17; // ecx@17
//	int v18; // esi@18
//	int v19; // edi@18
//	int v20; // edx@18
//	char *v21; // eax@28
//	unsigned int v22; // edx@28
//	char *v23; // eax@31
//	char *v24; // eax@36
//	signed int v25; // ecx@36
//	char *v26; // eax@41
//	signed int v27; // ecx@41
//	char v28; // dl@42
//	char *outputOld; // [sp+10h] [bp-44h]@0
//	char *inputOld; // [sp+14h] [bp-40h]@0
//	char *feedbackOld; // [sp+18h] [bp-3Ch]@0
//	__int64 OutputAlignedBuffer; // [sp+1Ch] [bp-38h]@7
//	__int64 InputAlignedBuffer; // [sp+24h] [bp-30h]@4
//	__int64 FeedbackAlignedBuffer; // [sp+2Ch] [bp-28h]@12
//	char temp[32]; // [sp+34h] [bp-20h]@40
//	signed int dwBlockLena; // [sp+5Ch] [bp+8h]@4
//	signed int fInputAligned; // [sp+60h] [bp+Ch]@9
//
//	v7 = dwBlockLen;
//	if (dwBlockLen == 8)
//	{
//		v8 = (__int64 *)input;
//		v9 = output;
//		if ((unsigned __int8)input & 7 && input != output)
//		{
//			v10 = *(DWORD *)input;
//			inputOld = input;
//			v8 = &InputAlignedBuffer;
//			HIDWORD(InputAlignedBuffer) = *((DWORD *)input + 1);
//			LODWORD(InputAlignedBuffer) = v10;
//			input = (char *)&InputAlignedBuffer;
//			dwBlockLena = 0;
//		}
//		else
//		{
//			dwBlockLena = 1;
//		}
//		if ((unsigned __int8)output & 7)
//		{
//			OutputAlignedBuffer = *(QWORD *)output;
//			if ((__int64 *)output == v8)
//			{
//				v8 = &OutputAlignedBuffer;
//				input = (char *)&OutputAlignedBuffer;
//			}
//			outputOld = output;
//			v9 = (char *)&OutputAlignedBuffer;
//			fInputAligned = 0;
//		}
//		else
//		{
//			fInputAligned = 1;
//		}
//		v11 = feedback;
//		if ((unsigned __int8)feedback & 7)
//		{
//			LODWORD(FeedbackAlignedBuffer) = *(DWORD *)feedback;
//			feedbackOld = feedback;
//			v11 = (char *)&FeedbackAlignedBuffer;
//			HIDWORD(FeedbackAlignedBuffer) = *((DWORD *)feedback + 1);
//			feedback = (char *)&FeedbackAlignedBuffer;
//			v12 = 0;
//		}
//		else
//		{
//			v12 = 1;
//		}
//		if (op == 1)
//		{
//			v13 = *(DWORD *)v11 ^ *(DWORD *)v8;
//			v14 = *((DWORD *)v11 + 1) ^ *((DWORD *)v8 + 1);
//			*(DWORD *)v9 = v13;
//			*((DWORD *)v9 + 1) = v14;
//			Cipher(v9, v9, keyTable, 1);
//			v15 = feedback;
//			*(DWORD *)feedback = *(DWORD *)v9;
//			*((DWORD *)feedback + 1) = *((DWORD *)v9 + 1);
//		}
//		else
//		{
//			if ((__int64 *)v9 != v8)
//			{
//				Cipher(v9, (char *)v8, keyTable, 0);
//				v15 = feedback;
//				v16 = *((DWORD *)v9 + 1);
//				*(DWORD *)v9 ^= *(DWORD *)feedback;
//				*((DWORD *)v9 + 1) = *((DWORD *)feedback + 1) ^ v16;
//				v17 = input;
//				*(DWORD *)feedback = *(DWORD *)input;
//				*((DWORD *)feedback + 1) = *((DWORD *)input + 1);
//			LABEL_20:
//				if (!fInputAligned)
//				{
//					*(DWORD *)outputOld = *(DWORD *)v9;
//					*((DWORD *)outputOld + 1) = *((DWORD *)v9 + 1);
//				}
//				if (!dwBlockLena)
//				{
//					*(DWORD *)inputOld = *(DWORD *)v17;
//					*((DWORD *)inputOld + 1) = *((DWORD *)v17 + 1);
//				}
//				if (!v12)
//				{
//					*(DWORD *)feedbackOld = *(DWORD *)v15;
//					*((DWORD *)feedbackOld + 1) = *((DWORD *)v15 + 1);
//				}
//				return;
//			}
//			v18 = *(DWORD *)v8;
//			v19 = *((DWORD *)v8 + 1);
//			Cipher(v9, (char *)v8, keyTable, 0);
//			v15 = feedback;
//			v20 = *((DWORD *)v9 + 1);
//			*(DWORD *)v9 ^= *(DWORD *)feedback;
//			*((DWORD *)v9 + 1) = *((DWORD *)feedback + 1) ^ v20;
//			*(DWORD *)feedback = v18;
//			*((DWORD *)feedback + 1) = v19;
//		}
//		v17 = input;
//		goto LABEL_20;
//	}
//	if (op == 1)
//	{
//		if (dwBlockLen)
//		{
//			v21 = input;
//			v22 = dwBlockLen;
//			do
//			{
//				*v21 ^= v21[feedback - input];
//				++v21;
//				--v22;
//			} while (v22);
//		}
//		Cipher(output, input, keyTable, 1);
//		if (dwBlockLen)
//		{
//			v23 = feedback;
//			do
//			{
//				*v23 = v23[output - feedback];
//				++v23;
//				--v7;
//			} while (v7);
//		}
//	}
//	else if (input == output)
//	{
//		if (dwBlockLen <= 32)
//		{
//			qmemcpy(temp, input, dwBlockLen);
//			Cipher(output, input, keyTable, 0);
//			if (dwBlockLen)
//			{
//				v26 = output;
//				v27 = feedback - output;
//				do
//				{
//					v28 = v26[temp - output];
//					*v26 ^= v26[v27];
//					(v26++)[v27] = v28;
//					--v7;
//				} while (v7);
//			}
//		}
//	}
//	else
//	{
//		Cipher(output, input, keyTable, 0);
//		if (dwBlockLen)
//		{
//			v24 = output;
//			v25 = feedback - output;
//			do
//			{
//				*v24 ^= v24[v25];
//				v24[v25] = v24[input - output];
//				++v24;
//				--v7;
//			} while (v7);
//		}
//	}
//}

#pragma endregion