#include <string.h>
#include <stdint.h>
#include "sha.h"

#define SHA_BLOCKSIZE   (64)

/**
* Function to compute the digest
*
* @param key   Secret key
* @param keyLen  Length of the key in bytes
* @param data   Data
* @param dataLen  Length of data in bytes
* @param out_hash Digest output
*/
void hmac_sha1(const uint8_t *key, size_t keyLen, const uint8_t *data, size_t dataLen, uint8_t *out_hash) {

    SHA_CTX ictx, octx;
    uint8_t isha[SHA_DIGEST_LENGTH], osha[SHA_DIGEST_LENGTH];
    uint8_t keybuf[SHA_DIGEST_LENGTH];
    uint8_t buf[SHA_BLOCKSIZE];
    size_t i;

    if (keyLen > SHA_BLOCKSIZE) {
        SHA_CTX tctx;
        SHA1_Init(&tctx);
        SHA1_Update(&tctx, key, keyLen);
        SHA1_Final(keybuf, &tctx);
        key = keybuf;
        keyLen = SHA_DIGEST_LENGTH;
    }

    /**** Inner Digest ****/

    SHA1_Init(&ictx);

    /* Pad the key for inner digest */
    for (i = 0; i < keyLen; ++i) buf[i] = key[i] ^ 0x36;
    for (i = keyLen; i < SHA_BLOCKSIZE; ++i) buf[i] = 0x36;

    SHA1_Update(&ictx, buf, SHA_BLOCKSIZE);
    SHA1_Update(&ictx, data, dataLen);
    SHA1_Final(isha, &ictx);

    /**** Outer Digest ****/

    SHA1_Init(&octx);

    /* Pad the key for outter digest */

    for (i = 0; i < keyLen; ++i) buf[i] = key[i] ^ 0x5c;
    for (i = keyLen; i < SHA_BLOCKSIZE; ++i) buf[i] = 0x5c;

    SHA1_Update(&octx, buf, SHA_BLOCKSIZE);
    SHA1_Update(&octx, isha, SHA_DIGEST_LENGTH);
    SHA1_Final(osha, &octx);

    /* truncate and print the results */
    memcpy(out_hash, osha, SHA_DIGEST_LENGTH);
}
