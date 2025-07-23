#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

#include "MultiCode.h"

int main(void) {
    // Prepare data
    printf("\r\nOriginal data: ");
    char* data = "Hello, world!";
    for (int i = 0; i < 14; ++i) {
        printf("%.2x ", data[i] & 0xFF);
    }
    printf("\r\n       -> %s", data);

    // Encode and decode with no errors
    char* code = MultiCode_Encode(data, 14, 8);

    printf("\r\nEncoded: %s", code);

    char* recovered = MultiCode_Decode(code, 14, 8);


    // Print out result
    if (recovered == NULL) {
        printf("\r\nFailed to recover data");
    } else {
        printf("\r\nRecovered: ");
        for (int i = 0; i < 14; ++i) {
            printf("%.2x ", recovered[i] & 0xFF);
        }
        printf("\r\n       -> %s", recovered);
    }
    free(recovered);


    // Damage the code to simulate transcription errors
    char t = code[0]; // transpose
    code[0] = code[1];
    code[1] = t;

    t = code[18]; // transpose
    code[18] = code[19];
    code[19] = t;

    code[52] = ' '; // delete

    // Check we can still recover
    printf("\r\nDamaged: %s", code);
    recovered = MultiCode_Decode(code, 14, 8);

    // Print out result
    if (recovered == NULL) {
        printf("\r\nFailed to recover data");
    } else {
        printf("\r\nRecovered: ");
        for (int i = 0; i < 14; ++i) {
            printf("%.2x ", recovered[i] & 0xFF);
        }
        printf("\r\n       -> %s", recovered);
    }

    free(recovered);
    free(code);
    return 0;
}