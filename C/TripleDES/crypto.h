#ifndef _CRYPTO_H
#define _CRYPTO_H

//Allocate memory block
#ifndef cryptoAllocMem
   #define cryptoAllocMem(size) osAllocMem(size)
#endif

//Deallocate memory block
#ifndef cryptoFreeMem
   #define cryptoFreeMem(p) osFreeMem(p)
#endif

//Fill block of memory
#ifndef cryptoMemset
   #include <string.h>
   #define cryptoMemset(p, value, length) (void) memset(p, value, length)
#endif

//Copy block of memory
#ifndef cryptoMemcpy
   #include <string.h>
   #define cryptoMemcpy(dest, src, length) (void) memcpy(dest, src, length)
#endif

//Move block of memory
#ifndef cryptoMemmove
   #include <string.h>
   #define cryptoMemmove(dest, src, length) (void) memmove(dest, src, length)
#endif

//Compare two blocks of memory
#ifndef cryptoMemcmp
   #include <string.h>
   #define cryptoMemcmp(p1, p2, length) memcmp(p1, p2, length)
#endif

//Get string length
#ifndef cryptoStrlen
   #include <string.h>
   #define cryptoStrlen(s) strlen(s)
#endif

//Copy string
#ifndef cryptoStrcpy
   #include <string.h>
   #define cryptoStrcpy(s1, s2) (void) strcpy(s1, s2)
#endif

//Copy characters from string
#ifndef cryptoStrncpy
   #include <string.h>
   #define cryptoStrncpy(s1, s2, length) (void) strncpy(s1, s2, length)
#endif

//Convert a character to lowercase
#ifndef cryptoTolower
   #include <ctype.h>
   #define cryptoTolower(c) tolower((uint8_t) (c))
#endif

//Check if a character is a decimal digit
#ifndef cryptoIsdigit
   #include <ctype.h>
   #define cryptoIsdigit(c) isdigit((uint8_t) (c))
#endif

//Maximum context size (block cipher algorithms)
#if (DES3_SUPPORT == ENABLED)
   #define MAX_CIPHER_CONTEXT_SIZE sizeof(Des3Context)
#elif (DES_SUPPORT == ENABLED)
   #define MAX_CIPHER_CONTEXT_SIZE sizeof(DesContext)
#endif

//Maximum block size (block cipher algorithms)
#if (DES3_SUPPORT == ENABLED)
   #define MAX_CIPHER_BLOCK_SIZE DES3_BLOCK_SIZE
#elif (DES_SUPPORT == ENABLED)
   #define MAX_CIPHER_BLOCK_SIZE DES_BLOCK_SIZE
#endif

//Rotate left operation
#define ROL8(a, n) (((a) << (n)) | ((a) >> (8 - (n))))
#define ROL16(a, n) (((a) << (n)) | ((a) >> (16 - (n))))
#define ROL32(a, n) (((a) << (n)) | ((a) >> (32 - (n))))
#define ROL64(a, n) (((a) << (n)) | ((a) >> (64 - (n))))

//Rotate right operation
#define ROR8(a, n) (((a) >> (n)) | ((a) << (8 - (n))))
#define ROR16(a, n) (((a) >> (n)) | ((a) << (16 - (n))))
#define ROR32(a, n) (((a) >> (n)) | ((a) << (32 - (n))))
#define ROR64(a, n) (((a) >> (n)) | ((a) << (64 - (n))))

//Shift left operation
#define SHL8(a, n) ((a) << (n))
#define SHL16(a, n) ((a) << (n))
#define SHL32(a, n) ((a) << (n))
#define SHL64(a, n) ((a) << (n))

//Shift right operation
#define SHR8(a, n) ((a) >> (n))
#define SHR16(a, n) ((a) >> (n))
#define SHR32(a, n) ((a) >> (n))
#define SHR64(a, n) ((a) >> (n))

//Micellaneous macros
#define _U8(x) ((uint8_t) (x))
#define _U16(x) ((uint16_t) (x))
#define _U32(x) ((uint32_t) (x))
#define _U64(x) ((uint64_t) (x))

//Test if a 8-bit integer is zero
#define CRYPTO_TEST_Z_8(a) \
   _U8((_U8((_U8(a) | (~_U8(a) + 1U))) >> 7U) ^ 1U)

//Test if a 8-bit integer is nonzero
#define CRYPTO_TEST_NZ_8(a) \
   _U8(_U8((_U8(a) | (~_U8(a) + 1U))) >> 7U)

//Test if two 8-bit integers are equal
#define CRYPTO_TEST_EQ_8(a, b) \
   _U8((_U8(((_U8(a) ^ _U8(b)) | (~(_U8(a) ^ _U8(b)) + 1U))) >> 7U) ^ 1U)

//Test if two 8-bit integers are not equal
#define CRYPTO_TEST_NEQ_8(a, b) \
   _U8(_U8(((_U8(a) ^ _U8(b)) | (~(_U8(a) ^ _U8(b)) + 1U))) >> 7U)

//Test if a 8-bit integer is lower than another 8-bit integer
#define CRYPTO_TEST_LT_8(a, b) \
   _U8(_U8((((_U8(a) - _U8(b)) ^ _U8(b)) | (_U8(a) ^ _U8(b))) ^ _U8(a)) >> 7U)

//Test if a 8-bit integer is lower or equal than another 8-bit integer
#define CRYPTO_TEST_LTE_8(a, b) \
   _U8((_U8((((_U8(b) - _U8(a)) ^ _U8(a)) | (_U8(a) ^ _U8(b))) ^ _U8(b)) >> 7U) ^ 1U)

//Test if a 8-bit integer is greater than another 8-bit integer
#define CRYPTO_TEST_GT_8(a, b) \
   _U8(_U8((((_U8(b) - _U8(a)) ^ _U8(a)) | (_U8(a) ^ _U8(b))) ^ _U8(b)) >> 7U)

//Test if a 8-bit integer is greater or equal than another 8-bit integer
#define CRYPTO_TEST_GTE_8(a, b) \
   _U8((_U8((((_U8(a) - _U8(b)) ^ _U8(b)) | (_U8(a) ^ _U8(b))) ^ _U8(a)) >> 7U) ^ 1U)

//Select between two 8-bit integers
#define CRYPTO_SELECT_8(a, b, c) \
   _U8((_U8(a) & (_U8(c) - 1U)) | (_U8(b) & ~(_U8(c) - 1U)))

//Test if a 16-bit integer is zero
#define CRYPTO_TEST_Z_16(a) \
   _U16((_U16((_U16(a) | (~_U16(a) + 1U))) >> 15U) ^ 1U)

//Test if a 16-bit integer is nonzero
#define CRYPTO_TEST_NZ_16(a) \
   _U16(_U16((_U16(a) | (~_U16(a) + 1U))) >> 15U)

//Test if two 16-bit integers are equal
#define CRYPTO_TEST_EQ_16(a, b) \
   _U16((_U16(((_U16(a) ^ _U16(b)) | (~(_U16(a) ^ _U16(b)) + 1U))) >> 15U) ^ 1U)

//Test if two 16-bit integers are not equal
#define CRYPTO_TEST_NEQ_16(a, b) \
   _U16(_U16(((_U16(a) ^ _U16(b)) | (~(_U16(a) ^ _U16(b)) + 1U))) >> 15U)

//Test if a 16-bit integer is lower than another 16-bit integer
#define CRYPTO_TEST_LT_16(a, b) \
   _U16(_U16((((_U16(a) - _U16(b)) ^ _U16(b)) | (_U16(a) ^ _U16(b))) ^ _U16(a)) >> 15U)

//Test if a 16-bit integer is lower or equal than another 16-bit integer
#define CRYPTO_TEST_LTE_16(a, b) \
   _U16((_U16((((_U16(b) - _U16(a)) ^ _U16(a)) | (_U16(a) ^ _U16(b))) ^ _U16(b)) >> 15U) ^ 1U)

//Test if a 16-bit integer is greater than another 16-bit integer
#define CRYPTO_TEST_GT_16(a, b) \
   _U16(_U16((((_U16(b) - _U16(a)) ^ _U16(a)) | (_U16(a) ^ _U16(b))) ^ _U16(b)) >> 15U)

//Test if a 16-bit integer is greater or equal than another 16-bit integer
#define CRYPTO_TEST_GTE_16(a, b) \
   _U16((_U16((((_U16(a) - _U16(b)) ^ _U16(b)) | (_U16(a) ^ _U16(b))) ^ _U16(a)) >> 15U) ^ 1U)

//Select between two 16-bit integers
#define CRYPTO_SELECT_16(a, b, c) \
   _U16((_U16(a) & (_U16(c) - 1U)) | (_U16(b) & ~(_U16(c) - 1U)))

//Test if a 32-bit integer is zero
#define CRYPTO_TEST_Z_32(a) \
   _U32((_U32((_U32(a) | (~_U32(a) + 1U))) >> 31U) ^ 1U)

//Test if a 32-bit integer is nonzero
#define CRYPTO_TEST_NZ_32(a) \
   _U32(_U32((_U32(a) | (~_U32(a) + 1U))) >> 31U)

//Test if two 32-bit integers are equal
#define CRYPTO_TEST_EQ_32(a, b) \
   _U32((_U32(((_U32(a) ^ _U32(b)) | (~(_U32(a) ^ _U32(b)) + 1U))) >> 31U) ^ 1U)

//Test if two 32-bit integers are not equal
#define CRYPTO_TEST_NEQ_32(a, b) \
   _U32(_U32(((_U32(a) ^ _U32(b)) | (~(_U32(a) ^ _U32(b)) + 1U))) >> 31U)

//Test if a 32-bit integer is lower than another 32-bit integer
#define CRYPTO_TEST_LT_32(a, b) \
   _U32(_U32((((_U32(a) - _U32(b)) ^ _U32(b)) | (_U32(a) ^ _U32(b))) ^ _U32(a)) >> 31U)

//Test if a 32-bit integer is lower or equal than another 32-bit integer
#define CRYPTO_TEST_LTE_32(a, b) \
   _U32((_U32((((_U32(b) - _U32(a)) ^ _U32(a)) | (_U32(a) ^ _U32(b))) ^ _U32(b)) >> 31U) ^ 1U)

//Test if a 32-bit integer is greater than another 32-bit integer
#define CRYPTO_TEST_GT_32(a, b) \
   _U32(_U32((((_U32(b) - _U32(a)) ^ _U32(a)) | (_U32(a) ^ _U32(b))) ^ _U32(b)) >> 31U)

//Test if a 32-bit integer is greater or equal than another 32-bit integer
#define CRYPTO_TEST_GTE_32(a, b) \
   _U32((_U32((((_U32(a) - _U32(b)) ^ _U32(b)) | (_U32(a) ^ _U32(b))) ^ _U32(a)) >> 31U) ^ 1U)

//Select between two 32-bit integers
#define CRYPTO_SELECT_32(a, b, c) \
   _U32((_U32(a) & (_U32(c) - 1U)) | (_U32(b) & ~(_U32(c) - 1U)))

//Select between two 64-bit integers
#define CRYPTO_SELECT_64(a, b, c) \
   _U64((_U64(a) & (_U64(c) - 1U)) | (_U64(b) & ~(_U64(c) - 1U)))

/**
 * @brief Encryption algorithm type
 **/

typedef enum
{
   CIPHER_ALGO_TYPE_STREAM = 0,
   CIPHER_ALGO_TYPE_BLOCK  = 1
} CipherAlgoType;


/**
 * @brief Cipher operation modes
 **/

typedef enum
{
   CIPHER_MODE_NULL              = 0,
   CIPHER_MODE_CBC               = 1,
} CipherMode;

typedef enum
{
	ENCRYPT = 1,
	DECRYPT = 2
} CipherDirection;


//Common API for encryption algorithms
typedef int (*CipherAlgoInit)(void *context, const unsigned char *key, int keyLen);
typedef void (*CipherAlgoEncryptStream)(void *context, const unsigned char *input, unsigned char *output, int length);
typedef void (*CipherAlgoDecryptStream)(void *context, const unsigned char *input, unsigned char *output, int length);
typedef void (*CipherAlgoEncryptBlock)(void *context, const unsigned char *input, unsigned char *output);
typedef void (*CipherAlgoDecryptBlock)(void *context, const unsigned char*input, unsigned char *output);


/**
 * @brief Common interface for encryption algorithms
 **/

typedef struct
{
   const char *name;
   int contextSize;
   CipherAlgoType type;
   int blockSize;
   CipherAlgoInit init;
   CipherAlgoEncryptStream encryptStream;
   CipherAlgoDecryptStream decryptStream;
   CipherAlgoEncryptBlock encryptBlock;
   CipherAlgoDecryptBlock decryptBlock;
} CipherAlgo;

#endif
