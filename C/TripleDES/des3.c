#include "des3.h"
#include "des.h"

//Common interface for encryption algorithms
const CipherAlgo des3CipherAlgo =
{
   "3DES",
   sizeof(Des3Context),
   CIPHER_ALGO_TYPE_BLOCK,
   DES3_BLOCK_SIZE,
   (CipherAlgoInit) des3Init,
   0,
   0,
   (CipherAlgoEncryptBlock) des3EncryptBlock,
   (CipherAlgoDecryptBlock) des3DecryptBlock
};


/**
 * @brief Initialize a Triple DES context using the supplied key
 * @param[in] context Pointer to the Triple DES context to initialize
 * @param[in] key Pointer to the key
 * @param[in] keyLen Length of the key
 * @return Error code
 **/

int des3Init(Des3Context *context, const unsigned char *key, int keyLen)
{
   //Check key length
   if(keyLen == 8)
   {
      //This option provides backward compatibility with DES, because the
      //first and second DES operations cancel out
      desInit(&context->k1, key, 8);
      desInit(&context->k2, key, 8);
      desInit(&context->k3, key, 8);
   }
   else if(keyLen == 16)
   {
      //If the key length is 128 bits including parity, the first 8 bytes of the
      //encoding represent the key used for the two outer DES operations, and
      //the second 8 bytes represent the key used for the inner DES operation
      desInit(&context->k1, key, 8);
      desInit(&context->k2, key + 8, 8);
      desInit(&context->k3, key, 8);
   }
   else if(keyLen == 24)
   {
      //If the key length is 192 bits including parity, then three independent DES
      //keys are represented, in the order in which they are used for encryption
      desInit(&context->k1, key, 8);
      desInit(&context->k2, key + 8, 8);
      desInit(&context->k3, key + 16, 8);
   }
   else
   {
      //Invalid key length...
      return 1;
   }

   //No error to report
   return 0;
}


/**
 * @brief Encrypt a 8-byte block using Triple DES algorithm
 * @param[in] context Pointer to the Triple DES context
 * @param[in] input Plaintext block to encrypt
 * @param[out] output Ciphertext block resulting from encryption
 **/

void des3EncryptBlock(Des3Context *context, const unsigned char *input, unsigned char *output)
{
   //The first pass is a DES encryption
   desEncryptBlock(&context->k1, input, output);
   //The second pass is a DES decryption of the first ciphertext result
   desDecryptBlock(&context->k2, output, output);
   //The third pass is a DES encryption of the second pass result
   desEncryptBlock(&context->k3, output, output);
}


/**
 * @brief Decrypt a 8-byte block using Triple DES algorithm
 * @param[in] context Pointer to the Triple DES context
 * @param[in] input Ciphertext block to decrypt
 * @param[out] output Plaintext block resulting from decryption
 **/

void des3DecryptBlock(Des3Context *context, const unsigned char *input, unsigned char *output)
{
   //The first pass is a DES decryption
   desDecryptBlock(&context->k3, input, output);
   //The second pass is a DES encryption of the first pass result
   desEncryptBlock(&context->k2, output, output);
   //The third pass is a DES decryption of the second ciphertext result
   desDecryptBlock(&context->k1, output, output);
}