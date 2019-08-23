#include "des.h"
#include "crypto.h"

//Triple DES block size
#define DES3_BLOCK_SIZE 8
//Common interface for encryption algorithms
#define DES3_CIPHER_ALGO (&des3CipherAlgo)

/**
 * @brief Triple DES algorithm context
 **/

typedef struct
{
   DesContext k1;
   DesContext k2;
   DesContext k3;
} Des3Context;


//Triple DES related constants
extern const CipherAlgo des3CipherAlgo;

//Triple DES related functions
int des3Init(Des3Context *context, const unsigned char *key, int keyLen);
void des3EncryptBlock(Des3Context *context, const unsigned char *input, unsigned char *output);
void des3DecryptBlock(Des3Context *context, const unsigned char *input, unsigned char *output);
