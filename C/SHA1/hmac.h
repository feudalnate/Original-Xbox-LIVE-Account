#include "sha.h"

void hmac_sha1(const uint8_t *key, size_t keyLen, const uint8_t *data, size_t dataLen, uint8_t *out_hash);