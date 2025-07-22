// ReSharper disable CppParameterMayBeConst
// ReSharper disable CppLocalVariableMayBeConst
#include "MultiCode.h"

#include <stdlib.h>

#pragma region FlexArray

/** A helper for cross-language variable-length integer arrays */
typedef struct FlexArrayObj {
    int* _storage; //!< Allocated storage, if any
    int _storeSize; //!< length of storage
    int _length; //!< length of data (not storage)
    int _offset; //!< offset into storage of first item
} FlexArrayObj, *FlexArray;

int* fa_ZeroArray(int size) {
    if (size < 1) return NULL;
    int* result = ALLOCATE(size, sizeof(int));
    if (result == NULL) return NULL;

    return result;
}

FlexArray fa_Create(int length, const int storeSize) {
    FlexArray result = ALLOCATE(1, sizeof(FlexArrayObj));
    if (result == NULL) {
        return NULL;
    }
    int* store = fa_ZeroArray(storeSize);
    if (store == NULL) {
        FREE(result);
        return NULL;
    }

    result->_storage    = store;
    result->_length     = length;
    const int halfSpare = (storeSize - length) / 2;
    result->_offset     = halfSpare > 0 ? halfSpare : 0;
    result->_storeSize  = storeSize;

    return result;
}

/**
 * Remove all values and set length to zero.
 * Does not remove storage.
 */
void fa_Clear(FlexArray this) {
    if (this == NULL) return;
    this->_offset = 0;
    this->_length = 0;
}

/**
 * Remove any leading zeroes.
 * This can result in an empty array
 */
void fa_TrimLeadingZero(FlexArray this) {
    if (this == NULL) return;

    while (this->_length > 0) {
        if (this->_storage[this->_offset] != 0) return;
        this->_offset++;
        this->_length--;
    }
}

/**
 * Return <c>true</c> if all values are zero.
 * Returns <c>true</c> for an empty array.
 */
int fa_AllZero(FlexArray this) {
    if (this == NULL) return 1;

    for (int i = 0; i < this->_length; i++) {
        if (this->_storage[this->_offset + i] != 0) return 0;
    }

    return 1;
}

/** Data length of array */
int fa_Length(FlexArray this) {
    if (this == NULL) return 0;
    return this->_length;
}

/** Get from index */
int fa_Get(FlexArray this, int i) {
    if (this == NULL) return 0;
    return this->_storage[i + this->_offset];
}

/** Set value at index */
void fa_Set(FlexArray this, int index, int value) {
    if (this == NULL) return;
    this->_storage[index + this->_offset] = value;
}

/** Release flex array */
void fa_Release(FlexArray* reference) {
    if (reference == NULL) return;
    FlexArray this = *reference;
    if (this == NULL) return;
    if (this->_storage != NULL)
        FREE(this->_storage);
    this->_storage = NULL;
    FREE(this);
    *reference = NULL;
}

/** Increase storage space by copying to new array. */
void fa_Grow(FlexArray* reference) {
    if (reference == NULL) return;
    FlexArray this = *reference;

    if (this == NULL || this->_storage == NULL) return;

    this->_storeSize *= 2;
    int* newStore = fa_ZeroArray(this->_storeSize);

    if (newStore == NULL) {
        fa_Release(reference);
        return;
    }

    for (int i = 0; i < this->_length; i++) {
        newStore[i] = this->_storage[i];
    }

    FREE(this->_storage);
    this->_storage = newStore;
}

/** Push a new value to the end of this array */
void fa_Push(FlexArray this, int v) {
    if (this == NULL) return;

    // If we have space left, just add
    if (this->_offset + this->_length < this->_storeSize - 1) {
        this->_storage[this->_offset + this->_length] = v;
        this->_length++;
        return;
    }

    // Run out of space. See if we can recover from offset?
    if (this->_offset > 0) {
        for (int i = 0; i < this->_length; i++) {
            this->_storage[i] = this->_storage[i + this->_offset];
        }
        this->_storage[this->_length] = v;
        this->_length++;

        this->_offset = 0;
        return;
    }

    // Still out of space. Let's size-up.
    fa_Grow(&this);
    if (this == NULL) return;

    // Do the add
    this->_storage[this->_length] = v;
    this->_length++;
}

/** Add a new value at the start of this array, pushing other values forward */
void fa_AddStart(FlexArray this, int v) {
    if (this == NULL) return;

    if (this->_offset > 0) {
        this->_offset--;
        this->_length++;
        this->_storage[this->_offset] = v;
        return;
    }

    // If we're out of space, grow
    if (this->_offset + this->_length >= this->_storeSize) {
        fa_Grow(&this);
        if (this == NULL) return;
    }

    // Shift everything forward (we could try shifting extra for optimisation)
    for (int i = this->_length - 1; i >= 0; i--) {
        this->_storage[i + 1] = this->_storage[i];
    }

    // Write at front
    this->_storage[0] = v;
    this->_length++;
}

/** Remove a given number of elements from the end of the array */
void fa_TrimEnd(FlexArray this, int len) {
    if (this == NULL) return;
    this->_length -= len;
    if (this->_length < 0) this->_length = 0;
}

/** In-place reverse of an array */
void fa_Reverse(FlexArray this) {
    if (this == NULL) return;
    if (this->_length < 2) return;

    int left  = this->_offset;
    int right = this->_offset + this->_length - 1;
    while (left < right) {
        int t                 = this->_storage[left];
        this->_storage[left]  = this->_storage[right];
        this->_storage[right] = t;
        left++;
        right--;
    }
}

/** Remove last item, returning the removed value */
int fa_Pop(FlexArray this) {
    if (this == NULL) return 0;
    if (this->_length <= 0) return 0;

    this->_length--;
    return this->_storage[this->_offset + this->_length];
}

/** Remove first item, returning the removed value */
int fa_PopFirst(FlexArray this) {
    if (this == NULL) return 0;
    if (this->_length > 0) {
        int r = this->_storage[this->_offset];
        this->_offset++;
        this->_length--;
        return r;
    }

    return 0;
}

/** Swap values at two indices, in-place */
void fa_Swap(FlexArray this, int i1, int i2) {
    if (this == NULL) return;
    int t1                             = this->_storage[this->_offset + i1];
    this->_storage[this->_offset + i1] = this->_storage[this->_offset + i2];
    this->_storage[this->_offset + i2] = t1;
}

/** Insert a new value at the given index, shifting later values forward */
void fa_InsertAt(FlexArray this, int index, int insertValue) {
    if (this == NULL) return;
    // [a,b,c,d,e].InsertAfter(2, x);
    // --> [a,b,x,c,d,e]

    // If we can remove offset, do that
    if (this->_offset > 0) {
        for (int i = 0; i < index; i++) {
            this->_storage[i + this->_offset - 1] = this->_storage[i + this->_offset];
        }
        this->_offset--;
        this->_storage[this->_offset + index] = insertValue;
        this->_length++;
        return;
    }

    // If we're out of space, grow
    if (this->_offset + this->_length >= this->_storeSize) {
        fa_Grow(&this);
        if (this == NULL) return;
    }

    // Shift values forward
    for (int i = this->_length - 1; i >= index; i--) {
        this->_storage[i + 1] = this->_storage[i];
    }

    this->_storage[index] = insertValue;
    this->_length++;
}

/** Remove a value at the given index, shifting later values back */
void fa_DeleteAt(FlexArray this, int index) {
    if (this == NULL) return;
    // [a,b,c,d,e].DeleteAt(2);
    // --> [a,b,  d,e]

    // If at start, move offset forward
    if (index <= 0) {
        this->_offset++;
        this->_length--;
        return;
    }

    // If at end, pull length back
    if (index >= this->_length - 1) {
        this->_length--;
        return;
    }

    // If nearer start, push forward and increase offset
    int half = this->_length / 2;
    if (index <= half) {
        for (int i = index; i > 0; i--) {
            this->_storage[this->_offset + i] = this->_storage[this->_offset + i - 1];
        }

        this->_offset++;
        this->_length--;
        return;
    }

    // If nearer end, pull back
    for (int i = index; i < this->_length - 1; i++) {
        this->_storage[this->_offset + i] = this->_storage[this->_offset + i + 1];
    }

    this->_length--;
}

/** Create a duplicate of this array */
FlexArray fa_Copy(FlexArray this) {
    FlexArray result = fa_Create(this->_length, this->_length);
    if (result == NULL) return NULL;

    for (int i = 0; i < this->_length; i++) {
        result->_storage[i + result->_offset] = this->_storage[i + this->_offset];
    }

    return result;
}

/**
 * Create a zero-filled array of a given length
 * @param length number of zero-value elements in the array
 * @return a flex array or NULL
 */
FlexArray fa_BySize(const int length) {
    return fa_Create(length, length + 16);
}

/**
 * Create a FlexArray with no pre-allocated growth space
 */
FlexArray fa_Fixed(const int length) {
    return fa_Create(length, length);
}

/**
 * Create a new FlexArray with a single `1` value stored
 */
FlexArray fa_SingleOne() {
    FlexArray output = fa_BySize(1);
    if (output == NULL) return NULL;
    fa_Set(output, 0, 1);
    return output;
}

/**
 * Create a new FlexArray with two values stored
 */
FlexArray fa_Pair(int a, int b) {
    FlexArray output = fa_Fixed(2);
    if (output == NULL) return NULL;
    fa_Set(output, 0, a);
    fa_Set(output, 1, b);
    return output;
}

#pragma endregion FlexArray

#pragma region Galois16

// 16-entry Galois field math for Reed-Solomon (4-bit per symbol)
static int g16_created = 0;
static int g16_exp[32];
static int g16_log[16];
const int g16_prime = 19; // must be fixed across implementations!

/** Set up the look-up tables */
void g16_CreateTables() {
    g16_created = -1;
    int x       = 1;

    for (int i = 0; i < 16; i++) {
        g16_exp[i] = x & 0x0f;
        g16_log[x] = i & 0x0f;
        x <<= 1;
        if ((x & 0x110) != 0) x ^= g16_prime;
    }
    for (int i = 15; i < 32; i++) {
        g16_exp[i] = g16_exp[i - 15] & 0x0f;
    }
}

/** Add or Subtract: a +/- b */
int g16_AddSub(int a, int b) {
    if (!g16_created) g16_CreateTables();
    return (a ^ b) & 0x0f;
}

/** Multiply a and b */
int g16_Mul(int a, int b) {
    if (!g16_created) g16_CreateTables();
    if (a == 0 || b == 0) return 0;
    return g16_exp[(g16_log[a] + g16_log[b]) % 15];
}

/** Divide a by b */
int g16_Div(int a, int b) {
    if (!g16_created) g16_CreateTables();
    if (a == 0 || b == 0) return 0;
    return g16_exp[(g16_log[a] + 15 - g16_log[b]) % 15];
}

/** Raise n to power of p */
int g16_Pow(int n, int p) {
    if (!g16_created) g16_CreateTables();
    return g16_exp[(g16_log[n] * p) % 15];
}

/** Get multiplicative inverse of n */
int g16_Inverse(int n) {
    if (!g16_created) g16_CreateTables();
    return g16_exp[15 - g16_log[n]];
}

/** Multiply a polynomial 'p' by a scalar 'sc' */
FlexArray g16_PolyMulScalar(FlexArray p, int sc) {
    FlexArray res = fa_BySize(fa_Length(p));
    if (res == NULL) return NULL;
    for (int i = 0; i < fa_Length(p); i++) {
        fa_Set(res, i, g16_Mul(fa_Get(p, i), sc));
    }
    return res;
}

/** Add two polynomials */
FlexArray g16_AddPoly(FlexArray p, FlexArray q) {
    if (p == NULL || q == NULL) return NULL;
    int len       = fa_Length(p) >= fa_Length(q) ? fa_Length(p) : fa_Length(q);
    FlexArray res = fa_BySize(len);
    if (res == NULL) return NULL;
    for (int i = 0; i < fa_Length(p); i++) {
        int idx = i + len - fa_Length(p);
        fa_Set(res, idx, fa_Get(p, i));
    }
    for (int i = 0; i < fa_Length(q); i++) {
        int idx = i + len - fa_Length(q);
        fa_Set(res, idx, fa_Get(res, idx) ^ fa_Get(q, i));
    }
    return res;
}

/** Multiply two polynomials */
FlexArray g16_MulPoly(FlexArray p, FlexArray q) {
    if (p == NULL || q == NULL) return NULL;
    FlexArray res = fa_BySize(fa_Length(p) + fa_Length(q) - 1);
    if (res == NULL) return NULL;
    for (int j = 0; j < fa_Length(q); j++) {
        for (int i = 0; i < fa_Length(p); i++) {
            int val = g16_AddSub(fa_Get(res, i + j), g16_Mul(fa_Get(p, i), fa_Get(q, j)));
            fa_Set(res, i + j, val);
        }
    }
    return res;
}

/** Evaluate polynomial 'p' for value 'x', resulting in a scalar */
int g16_EvalPoly(FlexArray p, int x) {
    int y = fa_Get(p, 0);
    for (int i = 1; i < fa_Length(p); i++) {
        y = g16_Mul(y, x) ^ fa_Get(p, i);
    }
    return y & 0x0f;
}

/** Generate an irreducible polynomial for use in Reed-Solomon codes */
FlexArray g16_IrreduciblePoly(int symCount) {
    FlexArray gen = fa_SingleOne();
    if (gen == NULL) return NULL;

    FlexArray next = fa_Pair(1, 1);
    if (next == NULL) {
        fa_Release(&gen);
        return NULL;
    }

    for (int i = 0; i < symCount; i++) {
        fa_Set(next, 1, g16_Pow(2, i));
        FlexArray nextGen = g16_MulPoly(gen, next);
        fa_Release(&gen);
        if (nextGen == NULL) return NULL;
        gen = nextGen;
    }
    fa_Release(&next);
    return gen;
}

#pragma endregion Galois16

#pragma region ReedSolomon

/** Find locations of symbols that do not match the Reed-Solomon polynomial */
FlexArray rs_CalcSyndromes(FlexArray msg, int sym) {
    if (msg == NULL) return NULL;
    FlexArray syndromes = fa_BySize(sym + 1);
    if (syndromes == NULL) return NULL;

    for (int i = 0; i < sym; i++) {
        fa_Set(syndromes, i + 1, g16_EvalPoly(msg, g16_Pow(2, i)));
    }
    return syndromes;
}

/** Build a polynomial to location errors in the message */
FlexArray rs_ErrorLocatorPoly(FlexArray synd, int sym, int erases) {
    if (synd == NULL) return NULL;
    FlexArray errLoc = fa_SingleOne();
    FlexArray oldLoc = fa_SingleOne();
    if (errLoc == NULL || oldLoc == NULL) {
        fa_Release(&errLoc);
        fa_Release(&oldLoc);
        return NULL;
    }

    int syndShift = 0;
    if (fa_Length(synd) > sym) syndShift = fa_Length(synd) - sym;

    for (int i = 0; i < sym - erases; i++) {
        int kappa = i + syndShift;
        int delta = fa_Get(synd, kappa);
        for (int j = 1; j < fa_Length(errLoc); j++) {
            delta ^= g16_Mul(fa_Get(errLoc, fa_Length(errLoc) - (j + 1)), fa_Get(synd, kappa - j));
        }
        fa_Push(oldLoc, 0);
        if (delta != 0) {
            if (fa_Length(oldLoc) > fa_Length(errLoc)) {
                FlexArray newLoc = g16_PolyMulScalar(oldLoc, delta);
                fa_Release(&oldLoc);
                oldLoc = g16_PolyMulScalar(errLoc, g16_Inverse(delta));
                fa_Release(&errLoc);
                errLoc = newLoc;
            }
            FlexArray scale      = g16_PolyMulScalar(oldLoc, delta);
            FlexArray nextErrLoc = g16_AddPoly(errLoc, scale);
            fa_Release(&errLoc);
            errLoc = nextErrLoc;
            fa_Release(&scale);
        }
    }
    fa_Release(&oldLoc);

    fa_TrimLeadingZero(errLoc);
    return errLoc;
}

/** Find error locations */
FlexArray rs_FindErrors(FlexArray locPoly, int len) {
    if (locPoly == NULL) return NULL;
    int errs      = fa_Length(locPoly) - 1;
    FlexArray pos = fa_BySize(0);
    if (pos == NULL) return NULL;

    for (int i = 0; i < len; i++) {
        int test = g16_EvalPoly(locPoly, g16_Pow(2, i)) & 0x0f;
        if (test == 0) {
            fa_Push(pos, len - 1 - i);
        }
    }

    if (fa_Length(pos) != errs) {
        fa_Clear(pos);
    }

    return pos;
}

/** Build polynomial to find data errors */
FlexArray rs_DataErrorLocatorPoly(FlexArray pos) {
    if (pos == NULL) return NULL;
    FlexArray eLoc = fa_SingleOne();
    FlexArray s1   = fa_SingleOne();
    FlexArray pair = fa_BySize(2);

    if (eLoc == NULL || s1 == NULL || pair == NULL) {
        fa_Release(&eLoc);
        fa_Release(&s1);
        fa_Release(&pair);
        return NULL;
    }

    for (int i = 0; i < fa_Length(pos); i++) {
        fa_Clear(pair);
        fa_Push(pair, g16_Pow(2, fa_Get(pos, i)));
        fa_Push(pair, 0);

        FlexArray add      = g16_AddPoly(s1, pair);
        FlexArray nextELoc = g16_MulPoly(eLoc, add);
        fa_Release(&eLoc);
        eLoc = nextELoc;
        fa_Release(&add);
    }

    fa_Release(&pair);
    fa_Release(&s1);

    return eLoc;
}

/** Try to evaluate a data error */
FlexArray rs_ErrorEvaluator(FlexArray synd, FlexArray errLoc, int n) {
    if (synd == NULL || errLoc == NULL) return NULL;

    FlexArray poly = g16_MulPoly(synd, errLoc);
    if (poly == NULL) return NULL;

    int len = fa_Length(poly) - (n + 1);
    for (int i = 0; i < len; i++) {
        fa_Set(poly, i, fa_Get(poly, i + len));
    }

    fa_TrimEnd(poly, len);
    return poly;
}

/** Try to correct errors in the message using the Forney algorithm */
FlexArray rs_CorrectErrors(FlexArray msg, FlexArray synd, FlexArray pos) {
    if (msg == NULL || synd == NULL || pos == NULL) return NULL;

    int len = fa_Length(msg);

    FlexArray coeffPos = fa_BySize(0);
    FlexArray chi      = fa_BySize(0);
    FlexArray tmp      = fa_BySize(0);
    FlexArray e        = fa_BySize(len);

    if (coeffPos == NULL || chi == NULL || tmp == NULL || e == NULL) {
        fa_Release(&coeffPos);
        fa_Release(&chi);
        fa_Release(&tmp);
        fa_Release(&e);
        return NULL;
    }

    fa_Reverse(synd);
    for (int i = 0; i < fa_Length(pos); i++) {
        fa_Push(coeffPos, len - 1 - fa_Get(pos, i));
    }

    FlexArray errLoc  = rs_DataErrorLocatorPoly(coeffPos);
    FlexArray errEval = rs_ErrorEvaluator(synd, errLoc, fa_Length(errLoc) - 1);

    if (errLoc == NULL || errEval == NULL) {
        fa_Release(&coeffPos);
        fa_Release(&chi);
        fa_Release(&tmp);
        fa_Release(&e);
        fa_Release(&errLoc);
        fa_Release(&errEval);
        return NULL;
    }

    for (int i = 0; i < fa_Length(coeffPos); i++) {
        fa_Push(chi, g16_Pow(2, fa_Get(coeffPos, i)));
    }

    for (int i = 0; i < fa_Length(chi); i++) {
        fa_Clear(tmp);
        int iChi = g16_Inverse(fa_Get(chi, i));
        for (int j = 0; j < fa_Length(chi); j++) {
            if (i == j) continue;
            fa_Push(tmp, g16_AddSub(1, g16_Mul(iChi, fa_Get(chi, j))));
        }

        int prime = 1;
        for (int k = 0; k < fa_Length(tmp); k++) {
            prime = g16_Mul(prime, fa_Get(tmp, k));
        }

        int y = g16_EvalPoly(errEval, iChi);
        y     = g16_Mul(g16_Pow(fa_Get(chi, i), 1), y); // pow?
        fa_Set(e, fa_Get(pos, i), g16_Div(y, prime));
    }

    FlexArray final = g16_AddPoly(msg, e);

    fa_Release(&errEval);
    fa_Release(&errLoc);
    fa_Release(&e);
    fa_Release(&tmp);
    fa_Release(&chi);
    fa_Release(&coeffPos);

    return final;
}

/**
 * Main Reed-Solomon encode
 * @param msg array of ints in 0..15
 * @param sym number of additional symbols
 * @return array of ints in 0..15
 */
FlexArray rs_Encode(FlexArray msg, int sym)
{
    if (msg == NULL) return NULL;

    FlexArray gen = g16_IrreduciblePoly(sym);
    FlexArray mix = fa_BySize(fa_Length(msg) + fa_Length(gen) - 1);
    if (gen == NULL || mix == NULL) {
        fa_Release(&gen);
        fa_Release(&mix);
        return NULL;
    }

    for (int i = 0; i < fa_Length(msg); i++)
    {
        fa_Set(mix, i, fa_Get(msg, i));
    }

    for (int i = 0; i < fa_Length(msg); i++)
    {
        int coeff = fa_Get(mix, i);
        if (coeff == 0) continue;
        for (int j = 1; j < fa_Length(gen); j++)
        {
            int next = fa_Get(mix, i + j) ^ g16_Mul(fa_Get(gen, j), coeff);
            fa_Set(mix, i + j, next);
        }
    }

    FlexArray output = fa_BySize(0);
    int len  = fa_Length(msg) + fa_Length(gen) - 1;
    for (int i = 0; i < len; i++) fa_Push(output, fa_Get(mix, i));

    for (int i = 0; i < fa_Length(msg); i++)
    {
        fa_Set(output, i, fa_Get(msg, i));
    }

    fa_Release(&gen);
    fa_Release(&mix);
    return output;
}

/**
 * Main decode and correct function
 * @param msg input symbols
 * @param sym count of additional check symbols in input
 * @param expectedLength expected length of original input
 * @return decoded data, or NULL if can't be decoded
 */
FlexArray rs_Decode(FlexArray msg, int sym, int expectedLength)
{
    if (msg == NULL) return NULL;

    int erases     = expectedLength - fa_Length(msg);
    FlexArray synd = rs_CalcSyndromes(msg, sym);

    if (fa_AllZero(synd))
    {
        // no errors found
        fa_Release(&synd);
        return fa_Copy(msg);
    }

    FlexArray errPoly = rs_ErrorLocatorPoly(synd, sym, erases);
    if (fa_Length(errPoly) - 1 - erases > sym)
    {
        // too many errors to decode
        fa_Release(&errPoly);
        fa_Release(&synd);
        return NULL;
    }

    fa_Reverse(errPoly);
    FlexArray errorPositions = rs_FindErrors(errPoly, fa_Length(msg));
    if (fa_Length(errorPositions) < 1)
    {
        // too many errors to decode
        fa_Release(&errorPositions);
        fa_Release(&errPoly);
        fa_Release(&synd);
        return NULL;
    }

    fa_Reverse(errorPositions);
    FlexArray result = rs_CorrectErrors(msg, synd, errorPositions);

    fa_Release(&errorPositions);
    fa_Release(&errPoly);
    fa_Release(&synd);

    // recheck result
    FlexArray synd2 = rs_CalcSyndromes(result, sym);
    if (fa_AllZero(synd2))
    {
        // all errors corrected
        fa_Release(&synd2);
        return result;
    }

    // Error correction failed
    fa_Release(&synd2);
    return NULL;
}
#pragma endregion ReedSolomon

#pragma region MultiCoder

#pragma region CodeParameters

// Note: '~' is for error.
// Q and S are lower cased to look less like 0 and 5.

/** Characters for odd-positioned output codes */
static const char OddSet[] = "01236789bGJNqXYZ~";

/** Characters for even-positioned output codes */
static const char EvenSet[] = "45ACD" "EFH" "KMPRsTVW~";

/** Look up characters likely to be entered as spaces. These will be trimmed from input */
int mc_IsSpace(char c) {
    switch (c) {
        case ' ':
        case '-':
        case '.':
        case '_':
        case '+':
        case '*':
        case '#':
            return -1;
        default: return 0;
    }
}

/** Likely mistakes. Mapped to characters we guess are correct */
char mc_Correction(char inp) {
    switch (inp) {
        case 'O': return '0';
        case 'L':
        case 'I': return '1';
        case 'U': return 'V';
        default: return inp;
    }
}

/** Case changes to improve letter/number distinction */
char mc_CaseChanges(char inp) {
    switch (inp) {
        case 'B': return 'b';
        case 'Q': return 'q';
        case 'S': return 's';
        default: return inp;
    }
}

#pragma endregion CodeParameters

/** Find index in char array, or -1 if not found */
int mc_IndexOf(const char src[18], char target) {
    if (src == NULL) return -1;
    for (int i = 0; i < 18; i++) {
        if (src[i] == target) return i;
    }

    return -1;
}

/** Message value, and message output position to encoded character */
char mc_EncodeDisplay(int number, int position) {
    if (number < 0 || number > 15) return '~';
    if ((position & 1) == 0) return OddSet[number];
    return EvenSet[number];
}

/** Create an output string for message data. Result must be free()'d */
char* mc_Display(FlexArray message) {
    int length = 1; // space for terminator
    for (int i = 0; i < fa_Length(message); i++) {
        length++;
        if (i > 0 && (i % 4 == 0 || i % 2 == 0)) length++;
    }

    char* result = ALLOCATE(length, 1);
    if (result == NULL) return NULL;

    int j = 0;
    for (int i = 0; i < fa_Length(message); i++) {
        if (i > 0) {
            if (i % 4 == 0) result[j++] = '-';
            else if (i % 2 == 0) result[j++] = ' ';
        }

        result[j++] = mc_EncodeDisplay(fa_Get(message, i), i);
    }

    result[j] = 0; // ensure terminator
    return result;
}

/** Find first position where chirality is incorrect */
int mc_FindFirstChiralityError(FlexArray chirality) {
    if (chirality == NULL) return -1;

    for (int position = 0; position < fa_Length(chirality); position++) {
        int expected = position & 1;
        if (fa_Get(chirality, position) != expected) return position;
    }

    return -1;
}

/** */
int mc_RepairCodesAndChirality(int expectedCodeLength,
                               FlexArray codes, FlexArray chirality, FlexArray transposes) {
    const int tryAgain  = 0;
    const int completed = -1;

    if (codes == NULL || chirality == NULL || transposes == NULL) return completed;

    if (fa_Length(codes) != fa_Length(chirality)) {
        // ERROR in code/chirality code
        return completed; // can't do much here
    }

    int currentLength = fa_Length(codes);
    int minLength     = (2 * expectedCodeLength) / 3;

    if (currentLength < minLength) {
        // Code is too short to recover accurately
        return completed;
    }

    int firstErrPos = mc_FindFirstChiralityError(chirality);
    if (currentLength == expectedCodeLength && firstErrPos < 0) {
        // Input codes seem correct
        return completed;
    }

    // If input is shorter than expected, guess where a deletion occurred, and insert a zero-value.
    if (currentLength < expectedCodeLength) {
        // Insert a zero value at error position, and inject correct chirality
        if (firstErrPos < 0) {
            // error is at the end
            int chi  = currentLength & 1;
            int diff = expectedCodeLength - currentLength;
            if (diff == 1 && chi != 1) {
                // don't add a wrong chi at the end if we're off-by-one
                fa_AddStart(codes, 0);
                fa_AddStart(chirality, 0);
                fa_Push(transposes, 0);
            } else {
                fa_Push(codes, 0);
                fa_Push(chirality, chi);
                fa_Push(transposes, currentLength);
            }
        } else {
            int chi = firstErrPos & 1;
            fa_InsertAt(codes, firstErrPos, 0);
            fa_InsertAt(chirality, firstErrPos, chi);
            fa_Push(transposes, firstErrPos);
        }

        return tryAgain;
    }

    // If input is longer than expected, guess where the problem is and delete
    if (currentLength > expectedCodeLength) {
        // First, if the last code is bad chirality, delete that before anything else
        int expectedLastChi = (1 + expectedCodeLength) & 1;
        if (fa_Get(chirality, currentLength - 1) != expectedLastChi) {
            fa_Pop(codes);
            fa_Pop(chirality);
            return tryAgain;
        }

        // value and chirality at error position
        if (firstErrPos < 0) firstErrPos = currentLength - 1;
        fa_DeleteAt(codes, firstErrPos);
        fa_DeleteAt(chirality, firstErrPos);

        fa_Push(transposes, firstErrPos);
        return tryAgain;
    }

    // Input is correct length, but we have swapped characters.
    // Try swapping at first error, unless it is at the end.
    if (firstErrPos >= expectedCodeLength - 1) {
        return completed;
    }

    if (fa_Get(chirality, firstErrPos) == fa_Get(chirality, firstErrPos + 1)) {
        // A simple swap won't fix this. Either a totally wrong code, or repeated insertions and deletions.
        // For now, we will flip the chirality without changing anything so the checks can continue.

        fa_Set(chirality, firstErrPos, 1 - fa_Get(chirality, firstErrPos));

        fa_Push(transposes, firstErrPos);
        return tryAgain;
    }

    // swapping characters might fix the problem
    fa_Swap(codes, firstErrPos, firstErrPos + 1);
    fa_Swap(chirality, firstErrPos, firstErrPos + 1);

    fa_Push(transposes, firstErrPos);
    return tryAgain;
}

FlexArray mc_DecodeDisplay(int expectedCodeLength, const char* input, FlexArray transposes) {
    if (input == NULL || expectedCodeLength < 1) return NULL;
    int validCharCount = 0;
    int safetyLimit    = expectedCodeLength * 4;

    // Run filters first, to get the number of 'correct' characters.
    // We could extend this to store the location of unexpected chars to improve the next loop.
    int inputLength = 0;
    for (int i = 0; i < safetyLimit; i++) {
        if (input[i] == 0) {
            inputLength = i;
            break;
        }
        char src = (char) (input[i] & 0xDF);
        if (mc_IsSpace(src)) continue; // skip spaces
        src = mc_CaseChanges(src); // Q->q, S->s, B->b

        int oddIdx  = mc_IndexOf(OddSet, src);
        int evenIdx = mc_IndexOf(EvenSet, src);
        if (oddIdx >= 0 || evenIdx >= 0) validCharCount++;
    }
    if (inputLength < 1) return NULL;

    // negative = too many chars. Positive = too few.
    int charCountMismatch = expectedCodeLength - validCharCount;

    // set up arrays
    FlexArray codes     = fa_BySize(0);
    FlexArray chirality = fa_BySize(0);

    if (codes == NULL || chirality == NULL) {
        fa_Release(&codes);
        fa_Release(&chirality);
        return NULL;
    }

    int nextChir = 0;
    for (int i = 0; i < inputLength; i++) {
        char src = (char) (input[i] & 0xDF);

        if (mc_IsSpace(src)) continue; // skip spaces

        src = mc_CaseChanges(src); // Q->q, S->s, B->b
        src = mc_Correction(src); // fix for anticipated transcription errors

        int oddIdx  = mc_IndexOf(OddSet, src);
        int evenIdx = mc_IndexOf(EvenSet, src);

        if (oddIdx < 0 && evenIdx < 0) {
            // Broken character, maybe insert dummy.
            if (charCountMismatch > 0) {
                fa_Push(codes, 0);
                fa_Push(chirality, nextChir);
                nextChir = 1 - nextChir;
                charCountMismatch--;
            } else {
                charCountMismatch++;
            }
        } else if (oddIdx >= 0 && evenIdx >= 0) {
            // Should never happen!
            fa_Release(&codes);
            fa_Release(&chirality);
            return fa_Fixed(0);
        } else if (oddIdx >= 0) {
            fa_Push(codes, oddIdx);
            fa_Push(chirality, 0);
            nextChir = 1;
        } else {
            fa_Push(codes, evenIdx);
            fa_Push(chirality, 1);
            nextChir = 0;
        }
    }

    for (int tries = 0; tries < expectedCodeLength; tries++) {
        if (mc_RepairCodesAndChirality(expectedCodeLength, codes, chirality, transposes)) {
            fa_Release(&chirality);
            return codes;
        }
    }

    fa_Release(&chirality);
    return codes;
}

/** Try to decode input */
FlexArray mc_TryHardDecode(FlexArray msg, int sym, int expectedLength)
{
    FlexArray basicDecode = rs_Decode(msg, sym, expectedLength);
    if (basicDecode != NULL) return basicDecode;

    // Normal decoding didn't work. Try rotations

    int end  = fa_Length(msg);
    int half = end / 2;
    int i;
    for (i = 0; i < half; i++)
    {
        // rotate left until we run out of zeros
        int r = fa_PopFirst(msg); // remove and return first element
        if (r != 0)
        {
            fa_AddStart(msg, r);
            break;
        }

        fa_Push(msg, r);

        basicDecode = rs_Decode(msg, sym, expectedLength);
        if (basicDecode != NULL) return basicDecode;
    }

    // undo
    while (i > 0)
    {
        i--;
        int r = fa_Pop(msg);
        fa_AddStart(msg, r);
    }

    for (i = 0; i < half; i++)
    {
        // rotate right until we run out of zeros
        int r = fa_Pop(msg);
        if (r != 0)
        {
            fa_Push(msg, r);
            break;
        }

        fa_AddStart(msg, r);

        basicDecode = rs_Decode(msg, sym, expectedLength);
        if (basicDecode != NULL) return basicDecode;
    }

    return NULL;
}

#pragma endregion MultiCoder

/**
 * Encode binary data to a multi-code string
 * @param source pointer to start of data
 * @param sourceLength number of bytes in data
 * @param correctionSymbols count of correction symbols to add
 * @return pointer to null-terminated string. Free this after use.
 */
char* MultiCode_Encode(void* source, int sourceLength, int correctionSymbols) {
    if (source == NULL || sourceLength < 1) return NULL;

    unsigned char* data = source;
    int dataLength = sourceLength;

    // Convert from bytes to nybbles
    FlexArray src = fa_Fixed(dataLength * 2);
    int j   = 0;
    for (int i = 0; i < dataLength; i++)
    {
        int upper = (data[i] >> 4) & 0x0F;
        int lower = data[i] & 0x0F;

        fa_Set(src, j++, upper);
        fa_Set(src, j++, lower);
    }

    FlexArray encoded = rs_Encode(src, correctionSymbols);

    char* output  = mc_Display(encoded);

    fa_Release(&encoded);
    fa_Release(&src);

    return output;
}

/**
 * Decode a multi-code string to binary data
 * @param code pointer to null-terminated string. This is the end-user input.
 * @param dataLength number of bytes in ORIGINAL data
 * @param correctionSymbols count of correction symbols added to code
 * @return pointer to recovered data, or NULL on failure. Length is 'dataLength'. Free this after use
 */
void* MultiCode_Decode(char* code, int dataLength, int correctionSymbols) {
    FlexArray transposes = fa_BySize(0);

    int expectedCodeLength = (dataLength * 2) + correctionSymbols;
    FlexArray cleanInput   = mc_DecodeDisplay(expectedCodeLength, code, transposes);

    if (fa_Length(cleanInput) < expectedCodeLength) // Input too short
    {
        fa_Release(&cleanInput);
        return NULL;
    }

    if (fa_Length(cleanInput) > expectedCodeLength) // Input too long
    {
        return NULL;
    }

    fa_Release(&transposes);

    FlexArray decoded = mc_TryHardDecode(cleanInput, correctionSymbols, fa_Length(cleanInput));

    // Failed to recover
    if (decoded == NULL) {
        fa_Release(&cleanInput);
        return NULL;
    }

    // remove error correction symbols
    for (int i = 0; i < correctionSymbols; i++) fa_Pop(decoded);

    // decoded data is nybbles, convert back to bytes
    int length = fa_Length(decoded) / 2;
    char* final = ALLOCATE(length + 1, 1);
    for (int i = 0; i < length; i++)
    {
        int upper = (fa_PopFirst(decoded) << 4) & 0xF0;
        int lower = fa_PopFirst(decoded) & 0x0F;
        final[i]  = (char)(upper + lower);
    }
    if (decoded != cleanInput) fa_Release(&decoded);
    fa_Release(&cleanInput);
    return final;
}
