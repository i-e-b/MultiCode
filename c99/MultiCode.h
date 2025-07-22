#pragma once
#ifndef C99_MULTICODE_H
#define C99_MULTICODE_H

#ifndef ALLOCATE
#define ALLOCATE calloc
#endif
#ifndef FREE
#define FREE free
#endif

/**
 * Encode binary data to a multi-code string
 * @param data pointer to start of data
 * @param dataLength number of bytes in data
 * @param correctionSymbols count of correction symbols to add
 * @return pointer to null-terminated string. Free this after use.
 */
char* MultiCode_Encode(void* data, int dataLength, int correctionSymbols);

/**
 * Decode a multi-code string to binary data
 * @param code pointer to null-terminated string. This is the end-user input.
 * @param dataLength number of bytes in ORIGINAL data
 * @param correctionSymbols count of correction symbols added to code
 * @return pointer to recovered data, or NULL on failure. Length is 'dataLength'
 */
void* MultiCode_Decode(char* code, int dataLength, int correctionSymbols);

#endif //C99_MULTICODE_H