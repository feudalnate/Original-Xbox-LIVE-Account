#include "cbc.h"

/**
* @brief CBC encryption
* @param[in] cipher Cipher algorithm
* @param[in] context Cipher algorithm context
* @param[in,out] iv Initialization vector
* @param[in] p Plaintext to be encrypted
* @param[out] c Ciphertext resulting from the encryption
* @param[in] length Total number of data bytes to be encrypted
* @return Error code
**/
int cbcEncrypt(const CipherAlgo *cipher, void *context,	unsigned char *iv, const unsigned char *input, int length, unsigned char *output)
{
   int i;

   //CBC mode operates in a block-by-block fashion
   while(length >= cipher->blockSize)
   {
      //XOR input block with IV contents
      for(i = 0; i < cipher->blockSize; i++)
         output[i] = input[i] ^ iv[i];

      //Encrypt the current block based upon the output
      //of the previous encryption
      cipher->encryptBlock(context, output, output);

      //Update IV with output block contents
      cryptoMemcpy(iv, output, cipher->blockSize);

      //Next block
      input += cipher->blockSize;
      output += cipher->blockSize;
      length -= cipher->blockSize;
   }

   //The plaintext must be a multiple of the block size
   if(length != 0)
      return 1;

   //Successful encryption
   return 0;
}


/**
 * @brief CBC decryption
 * @param[in] cipher Cipher algorithm
 * @param[in] context Cipher algorithm context
 * @param[in,out] iv Initialization vector
 * @param[in] c Ciphertext to be decrypted
 * @param[out] p Plaintext resulting from the decryption
 * @param[in] length Total number of data bytes to be decrypted
 * @return Error code
 **/
int cbcDecrypt(const CipherAlgo *cipher, void *context,	unsigned char*iv, const unsigned char* input, int length, unsigned char *output)
{
   int i;
   unsigned char t[16];

   //CBC mode operates in a block-by-block fashion
   while(length >= cipher->blockSize)
   {
      //Save input block
      cryptoMemcpy(t, input, cipher->blockSize);

      //Decrypt the current block
      cipher->decryptBlock(context, input, output);

      //XOR output block with IV contents
      for(i = 0; i < cipher->blockSize; i++)
		  output[i] ^= iv[i];

      //Update IV with input block contents
      cryptoMemcpy(iv, t, cipher->blockSize);

      //Next block
	  input += cipher->blockSize;
	  output += cipher->blockSize;
      length -= cipher->blockSize;
   }

   //The ciphertext must be a multiple of the block size
   if(length != 0)
      return 1;

   //Successful encryption
   return 0;
}
