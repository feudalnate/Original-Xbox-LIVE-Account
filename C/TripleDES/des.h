#ifndef DES_H
#define DES_H
#include "crypto.h"

//DES block size
#define DES_BLOCK_SIZE 8
//Common interface for encryption algorithms
#define DES_CIPHER_ALGO (&desCipherAlgo)

/**
 * @brief DES algorithm context
 **/

typedef struct
{
   unsigned int ks[32];
} DesContext;


//DES related constants
extern const CipherAlgo desCipherAlgo;

//DES related functions
int desInit(DesContext *context, const unsigned char *key, int keyLen);
void desEncryptBlock(DesContext *context, const unsigned char *input, unsigned char *output);
void desDecryptBlock(DesContext *context, const unsigned char *input, unsigned char *output);
#endif