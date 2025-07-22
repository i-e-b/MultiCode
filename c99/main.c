#include <stdint.h>
#include <stdio.h>
#include "MultiCode.h"

int main(void) {
    uint64_t data = 0xBEEFfeedFACEf00dUL; // TODO: fix endian issue here
    printf("\r\nOriginal data: %llx", data);

    char* code = MultiCode_Encode(&data, 8, 6);

    printf("\r\nEncoded: %s", code);

    char* recovered = MultiCode_Decode(code, 8, 6);

    if (recovered == NULL) {
        printf("\r\nFailed to recover data");
    } else {
        printf("\r\nRecovered: ");
        for (int i = 0; i < 8; ++i) {
            printf("%.2x ", recovered[i] & 0xFF);
        }
    }

    return 0;
}