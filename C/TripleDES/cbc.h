#include "crypto.h"

int cbcEncrypt(const CipherAlgo *cipher, void *context, unsigned char *iv, const unsigned char *input, int length, unsigned char *output);

int cbcDecrypt(const CipherAlgo *cipher, void *context, unsigned char*iv, const unsigned char* input, int length, unsigned char *output);