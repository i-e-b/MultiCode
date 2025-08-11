# region FlexArray

def fa_zero_array(size: int):
    """ Create an array with 'size' number of zeros """
    if size < 1:
        return list()
    return [0] * size


def fa_create(length: int, store_size: int):
    """ Create a FlexArray with given length and store size """
    half_space = (store_size - length) // 2
    offset = half_space if (half_space > 0) else 0
    result = FlexArray()
    result._storage = fa_zero_array(store_size)
    result._storeSize = store_size
    result._length = length
    result._offset = offset
    return result


def fa_by_size(length: int):
    """ Create a zero-filled array of a given length """
    return fa_create(length, length + 16)


def fa_fixed(length: int):
    """ Create a FlexArray with no pre-allocated growth space """
    return fa_create(length, length)


def fa_single_one():
    """ Create a new FlexArray with a single `1` value stored """
    result = fa_by_size(1)
    result.set(0, 1)
    return result


def fa_pair(a: int, b: int):
    """ Create a new FlexArray with two values stored """
    result = fa_fixed(2)
    result.set(0, a)
    result.set(1, b)
    return result


class FlexArray:
    """ Cross-language variable length integer array """
    _storage: list[int]
    _storeSize: int
    _length: int
    _offset: int

    def clear(self):
        """ Remove all values and set length to zero. Does not remove storage. """
        self._offset = 0
        self._length = 0

    def trim_leading_zero(self):
        """ Remove any leading zeroes. This can result in an empty array """
        while self._length > 0:
            if self._storage[self._offset] != 0: return
            self._offset += 1
            self._length -= 1

    def all_zero(self):
        """ Return `true` if all values are zero or array is empty """
        for i in range(0, self._length):
            if self._storage[self._offset + i] != 0: return False
        return True

    def length(self):
        """ Length of data in array """
        return self._length

    def get(self, i: int):
        """ Get value at index """
        return self._storage[i + self._offset]

    def set(self, index: int, value: int):
        """ Set value at index """
        self._storage[index + self._offset] = value

    def release(self):
        """ Release flex array """
        return  # No-op in garbage collected environments

    def grow(self):
        """ Increase storage space by copying to new array """
        old_store_size = self._storeSize
        self._storeSize *= 2
        new_store = fa_zero_array(self._storeSize)

        for i in range(0, old_store_size):
            new_store[i] = self._storage[i]

        self._storage = new_store

    def push(self, v: int):
        """ Push a new value to the end of this array """

        # If we have space left, just add
        if (self._offset + self._length) < (self._storeSize - 1):
            self._storage[self._offset + self._length] = v
            self._length += 1
            return

        # Run out of space. Try removing offset
        if self._offset > 0:
            for i in range(0, self._length):
                self._storage[i] = self._storage[i + self._offset]
            self._storage[self._length] = v
            self._length += 1
            self._offset = 0
            return

        # Still out of space. Grow the array and add normally
        self.grow()
        self._storage[self._length] = v
        self._length += 1
        return

    def add_start(self, v: int):
        """ Add a new value at the start of this array, pushing other values forward """

        # Use offset if we can
        if self._offset > 0:
            self._offset -= 1
            self._length += 1
            self._storage[self._offset] = v
            return

        # If we're out of space, grow
        if (self._offset + self._length) >= self._storeSize: self.grow()

        # Shift everything forwards 1 space
        for i in range(self._length - 1, -1, -1):
            self._storage[i + 1] = self._storage[i]

        # Write at front
        self._storage[0] = v
        self._length += 1

    def trim_end(self, count: int):
        """ Remove a given number of elements from the end of the array  """
        self._length -= count
        if self._length < 0: self._length = 0

    def reverse(self):
        """ In-place reverse of an array """
        if self._length < 2: return

        left = self._offset
        right = self._offset + self._length - 1

        while left < right:
            self._storage[left], self._storage[right] = self._storage[right], self._storage[left]

            left += 1
            right -= 1

    def pop(self):
        """ Remove last item, returning the removed value """
        if self._length < 1: return 0

        self._length -= 1
        return self._storage[self._offset + self._length]

    def pop_first(self):
        """ Remove first item, returning the removed value """
        if self._length < 1: return 0

        result = self._storage[self._offset]
        self._offset += 1
        self._length -= 1
        return result

    def swap(self, i1: int, i2: int):
        """ Swap values at two indices, in-place """
        self._storage[i1], self._storage[i2] = self._storage[i2], self._storage[i1]

    def insert_at(self, index: int, value: int):
        """ Insert a new value at the given index, shifting later values forward """
        # [a, b, c, d, e].InsertAfter(2, x);
        # --> [a, b, x, c, d, e]

        # Use up offset if available
        if self._offset > 0:
            # Shift everything before the insert back one place
            for i in range(0, index):
                self._storage[i + self._offset - 1] = self._storage[i + self._offset]
            self._offset -= 1
            self._storage[self._offset + index] = value
            self._length += 1
            return

        # If we're out of space, grow the array
        if (self._offset + self._length) >= self._storeSize:
            self.grow()

        # Shift values forward
        for i in range(self._length - 1, index - 1, -1):
            self._storage[i + 1] = self._storage[i]

        self._storage[index] = value
        self._length += 1

    def delete_at(self, index: int):
        """ Remove a value at the given index, shifting later values back """
        # [a,b,c,d,e].DeleteAt(2);
        # --> [a,b,  d,e]

        # If delete at start, move offset forward
        if index <= 0:
            self._offset += 1
            self._length -= 1
            return

        # If at end, pull length back
        if index >= (self._length - 1):
            self._length -= 1
            return

        # Deleting from inside the array
        # If nearer start, push forward and increase offset
        half = self._length // 2
        if index <= half:
            for i in range(index, 0, -1):
                self._storage[self._offset + i] = self._storage[self._offset + i - 1]

            self._offset += 1
            self._length -= 1
            return

        # Otherwise (nearer end), pull values back
        for i in range(index, self._length):
            self._storage[self._offset + i] = self._storage[self._offset + i + 1]

        self._length -= 1
        return

    def copy(self):
        """ Create a duplicate of this array """
        result = fa_create(self._length, self._length)
        for i in range(0, self._length):
            result._storage[i + result._offset] = self._storage[i + self._offset]
        return result

# endregion FlexArray

# region Galois16

_g16_created = False
_g16_exp = [0]*32
_g16_log = [0]*16
_g16_prime = 19 # must be fixed across implementations!

def g16_create_tables():
    """ Setup look-up tables """
    if _g16_created: return
    x = 1
    for i in range(0,16):
        _g16_exp[i] = x & 0x0F
        _g16_log[x] = i & 0x0F
        x <<= 1
        if (x&0x110) != 0: x ^= _g16_prime

    for i in range(15,32):
        _g16_exp[i] = _g16_exp[i - 15] & 0x0F

def g16_add_sub(a:int, b:int):
    """ Add or Subtract: a +/- b """
    if not _g16_created: g16_create_tables()
    return (a ^ b) & 0x0F

def g16_mul(a:int, b:int):
    """ Multiply: a * b """
    if not _g16_created: g16_create_tables()
    if a == 0 or b == 0: return 0
    return _g16_exp[(_g16_log[a] + _g16_log[b]) % 15]

def g16_div(a:int, b:int):
    """ Divide: a / b """
    if not _g16_created: g16_create_tables()
    if a == 0 or b == 0: return 0
    return _g16_exp[(_g16_log[a] + 15 - _g16_log[b]) % 15]

def g16_pow(n:int, p:int):
    """ Power: n**p """
    if not _g16_created: g16_create_tables()
    return _g16_exp[(_g16_log[n] * p) % 15]

def g16_inverse(n:int):
    """ Get multiplicative inverse: 1/n """
    if not _g16_created: g16_create_tables()
    return _g16_exp[15 - _g16_log[n]]

def g16_poly_mul_scalar(p:FlexArray, sc:int):
    """ Multiply a polynomial 'p' by a scalar 'sc' """
    result = fa_by_size(p.length())
    for i in range(0, p.length()):
        result.set(i, g16_mul(p.get(i),sc))
    return result

def g16_add_poly(p:FlexArray, q:FlexArray):
    """ Add two polynomials """
    length = max(p.length(), q.length())
    result = fa_by_size(length)
    for i in range(0, p.length()):
        idx = i + length - p.length()
        result.set(idx, p.get(i))
    for i in range(0, q.length()):
        idx = i + length - q.length()
        result.set(idx, result.get(idx) ^ q.get(i))
    return result

def g16_mul_poly(p:FlexArray, q:FlexArray):
    """ Multiply two polynomials """
    result = fa_by_size(p.length() + q.length() - 1)
    for j in range(0, q.length()):
        for i in range(0, p.length()):
            val = g16_add_sub(result.get(i+j), g16_mul(p.get(i), q.get(j)))
            result.set(i+j, val)
    return result

def g16_eval_poly(p:FlexArray, x:int):
    """ Evaluate polynomial 'p' for value 'x', resulting in a scalar """
    y = p.get(0)
    for i in range(1, p.length()):
        y = g16_mul(y,x) ^ p.get(i)
    return y & 0x0F

def g16_irreducible_poly(sym_count:int):
    """ Generate an irreducible polynomial for use in Reed-Solomon codes """
    gen = fa_single_one()
    next_pair = fa_pair(1,1)

    for i in range(0,sym_count):
        next_pair.set(1, g16_pow(2,i))
        gen = g16_mul_poly(gen, next_pair)


    next_pair.release()
    return gen

# endregion Galois16

# region ReedSolomon

def rs_calc_syndromes(msg:FlexArray, sym:int):
    """ Find locations of symbols that do not match the Reed-Solomon polynomial """
    syndromes = fa_by_size(sym + 1)
    for i in range(0,sym):
        syndromes.set(i+1, g16_eval_poly(msg, g16_pow(2, i)))
    return syndromes

def rs_error_locator_poly(synd:FlexArray, sym:int, erases:int):
    """ Build a polynomial to location errors in the message """
    err_loc = fa_single_one()
    old_loc = fa_single_one()

    synd_shift = 0
    if synd.length() > sym: synd_shift = synd.length() - sym

    for i in range(0, sym - erases):
        kappa = i + synd_shift
        delta = synd.get(kappa)

        for j in range(1, err_loc.length()):
            delta ^= g16_mul(err_loc.get(err_loc.length() - (j+1)), synd.get(kappa - j))
        old_loc.push(0)

        if delta != 0:
            if old_loc.length() > err_loc.length():
                new_loc = g16_poly_mul_scalar(old_loc, delta)
                old_loc.release()
                old_loc = g16_poly_mul_scalar(err_loc, g16_inverse(delta))
                err_loc.release()
                err_loc = new_loc

            scale = g16_poly_mul_scalar(old_loc, delta)
            next_err_loc = g16_add_poly(err_loc, scale)
            err_loc.release()
            err_loc = next_err_loc
            scale.release()

    old_loc.release()
    err_loc.trim_leading_zero()
    return err_loc

def rs_find_errors(loc_poly:FlexArray, length:int):
    """ Find error locations """
    errs = loc_poly.length() - 1
    pos = fa_by_size(0)

    for i in range(0, length):
        test = g16_eval_poly(loc_poly, g16_pow(2,i)) & 0x0F
        if test == 0: pos.push(length - 1 - i)

    if pos.length() != errs: pos.clear()

    return pos

def rs_data_error_locator_poly(pos:FlexArray):
    """ Build polynomial to find data errors """
    e_loc = fa_single_one()
    s1 = fa_single_one()
    pair = fa_by_size(2)

    for i in range(0, pos.length()):
        pair.clear()
        pair.push(g16_pow(2, pos.get(i)))
        pair.push(0)

        add = g16_add_poly(s1, pair)
        next_e_loc = g16_mul_poly(e_loc, add)
        e_loc.release()
        e_loc = next_e_loc
        add.release()

    pair.release()
    s1.release()

    return e_loc

def rs_error_evaluator(synd:FlexArray, err_loc:FlexArray, n:int):
    """ Try to evaluate a data error """
    poly = g16_mul_poly(synd, err_loc)
    length = poly.length() - (n + 1)

    for i in range(0, length):
        poly.set(i, poly.get(i + length))

    poly.trim_end(length)
    return poly

def rs_correct_errors(msg:FlexArray, synd:FlexArray, pos:FlexArray):
    """ Try to correct errors in the message using the Forney algorithm """
    length = msg.length()

    coeff_pos = fa_by_size(0)
    chi = fa_by_size(0)
    tmp = fa_by_size(0)
    e = fa_by_size(length)

    synd.reverse()
    for i in range(0, pos.length()):
        coeff_pos.push(length - 1 - pos.get(i))

    err_loc = rs_data_error_locator_poly(coeff_pos)
    err_eval = rs_error_evaluator(synd, err_loc, err_loc.length() - 1)

    for i in range(0, coeff_pos.length()):
        chi.push(g16_pow(2, coeff_pos.get(i)))

    for i in range(0, chi.length()):
        tmp.clear()
        i_chi = g16_inverse(chi.get(i))
        for j in range(0, chi.length()):
            if i == j: continue
            tmp.push(g16_add_sub(1, g16_mul(i_chi, chi.get(j))))

        prime = 1
        for k in range(0, tmp.length()):
            prime = g16_mul(prime, tmp.get(k))

        y = g16_eval_poly(err_eval, i_chi)
        y = g16_mul((g16_pow(chi.get(i), 1)), y)
        e.set(pos.get(i), g16_div(y, prime))

    final = g16_add_poly(msg, e)

    err_eval.release()
    err_loc.release()
    e.release()
    tmp.release()
    chi.release()
    coeff_pos.release()

    return final

def rs_encode(msg:FlexArray, sym:int):
    """
    Main Reed-Solomon encode
    :param msg: array of ints in 0..15
    :param sym: number of additional symbols for error correction
    :return: array of ints in 0..15
    """
    gen = g16_irreducible_poly(sym)
    mix = fa_by_size(msg.length() + gen.length() - 1)

    for i in range(0, msg.length()):
        mix.set(i, msg.get(i))

    for i in range(0, msg.length()):
        coeff = mix.get(i)
        if coeff == 0: continue
        for j in range(1, gen.length()):
            next_val = mix.get(i + j) ^ g16_mul(gen.get(j), coeff)
            mix.set(i + j, next_val)

    output = fa_by_size(0)
    length = msg.length() + gen.length() - 1
    for i in range(0, length): output.push(mix.get(i))

    for i in range(0, msg.length()):
        output.set(i, msg.get(i))

    gen.release()
    mix.release()
    return output

def rs_decode(msg:FlexArray, sym:int, expected_length:int):
    """
    Main decode and correct function
    :param msg: input symbols
    :param sym: of additional check symbols in input
    :param expected_length: expected length of original input
    :return: decoded data, or empty if can't be decoded
    """
    erases = expected_length - msg.length()
    synd = rs_calc_syndromes(msg, sym)

    if synd.all_zero(): # No errors found
        synd.release()
        return msg

    err_poly = rs_error_locator_poly(synd, sym, erases)

    if (err_poly.length() - 1 - erases) > sym: # too many errors to decode
        err_poly.release()
        synd.release()
        return fa_fixed(0)

    err_poly.reverse()
    error_positions = rs_find_errors(err_poly, msg.length())
    if error_positions.length() < 1: # too many errors to decode
        error_positions.release()
        err_poly.release()
        synd.release()
        return fa_fixed(0)

    error_positions.reverse()
    result = rs_correct_errors(msg, synd, error_positions)

    error_positions.release()
    err_poly.release()
    synd.release()

    # Recheck result
    synd2 = rs_calc_syndromes(result, sym)
    if synd2.all_zero(): # all errors corrected
        synd2.release()
        return result

    # correction failed
    synd2.release()
    return fa_fixed(0)

# endregion ReedSolomon

# region MultiCoder

# region CodeParameters

## Note: '~' is for error.
## Q and S are lower cased to look less like 0 and 5.

# noinspection SpellCheckingInspection
_odd_set  = "01236789bGJNqXYZ~"
# noinspection SpellCheckingInspection
_even_set = "45ACDEFHKMPRsTVW~"
# noinspection SpellCheckingInspection
_spaces = " -._+*#"

def mc_is_space(c: str):
    """ Look up characters likely to be entered as spaces. These will be trimmed from input """
    return _spaces.find(c) >= 0

def mc_correction(inp:str):
    """ Likely mistakes. Mapped to characters we guess are correct """
    if inp == 'O': return '0'
    if inp == 'L': return '1'
    if inp == 'I': return '1'
    if inp == 'U': return 'V'
    return inp

def mc_case_changes(inp:str):
    """ Case changes to improve letter/number distinction """
    if inp == 'B': return 'b'
    if inp == 'Q': return 'q'
    if inp == 'S': return 's'
    return inp

# endregion CodeParameters

def mc_index_of(src:str, target:str):
    """ Find index in char array, or -1 if not found """
    return src.find(target)

def mc_encode_display(number:int, position:int):
    """ Message value, and message output position to encoded character """
    if (number < 0) or (number > 15): return '~'
    if (position & 1) == 0: return _odd_set[number]
    return _even_set[number]

def mc_display(message:FlexArray):
    """ Create an output string for message data """
    result = ""
    for i in range(0, message.length()):
        if i > 0:
            if i % 4 == 0: result += '-'
            elif i % 2 == 0: result += ' '

        result += mc_encode_display(message.get(i), i)

    return result

def mc_find_first_chirality_error(chirality: FlexArray):
    """ Find first position where chirality is incorrect """
    for position in range(0, chirality.length()):
        expected = position & 1
        if chirality.get(position) != expected: return position
    return -1

def mc_repair_codes_and_chirality(expected_code_length:int,
                                  codes:FlexArray, chirality:FlexArray):
    """ Try to find and repair a single chirality error.
        This is the core of the odd/even code repairs. """
    try_again = 0
    completed = -1

    # each code point must have a chirality
    if codes.length() != chirality.length():
        return completed

    current_length = codes.length()
    min_length = (2 * expected_code_length) / 3

    if current_length < min_length: # code is too short to recover
        return completed

    first_err_pos = mc_find_first_chirality_error(chirality)
    if (current_length == expected_code_length) and (first_err_pos < 0):
        # Input seems correct
        return completed

    # If input is shorter than expected, guess where a deletion occurred
    # and insert a zero value
    if current_length < expected_code_length:
        if first_err_pos < 0:
            # error is at end
            chi = current_length & 1
            end_chi = expected_code_length & 1
            diff = expected_code_length - current_length
            if diff == 1 and chi == end_chi:
                # don't add a wrong chi at the end if we're off by 1
                codes.add_start(0)
                codes.add_start(0)
            else:
                codes.push(0)
                chirality.push(chi)
            return try_again

        # error not at end
        chi = first_err_pos & 1
        chi_next = (first_err_pos + 1) & 1
        chi_3rd = (first_err_pos + 1) & 1

        # First, check if this is a transpose and not the first delete
        not_at_end = first_err_pos < (current_length - 3)
        this_pos_wrong = chirality.get(first_err_pos) != chi
        next_pos_wrong = chirality.get(first_err_pos + 1) != chi_next
        third_is_ok = chirality.get(first_err_pos + 2) == chi_3rd
        if not_at_end and this_pos_wrong and next_pos_wrong and third_is_ok:
            codes.swap(first_err_pos, first_err_pos + 1)
            chirality.swap(first_err_pos, first_err_pos + 1)
            return try_again

        # Probably a delete at chirality error
        codes.insert_at(first_err_pos, 0)
        chirality.insert_at(first_err_pos, chi)
        return try_again

    # If input is longer than expected, guess where the problem is and delete
    if current_length > expected_code_length:
        # If the last code has bad chirality, delete that first
        expected_last_chi = (1 + expected_code_length) & 1
        if chirality.get(current_length - 1) != expected_last_chi:
            codes.pop()
            chirality.pop()
            return try_again

        # Delete value and chirality at error position
        if first_err_pos < 0: first_err_pos = current_length - 1
        codes.delete_at(first_err_pos)
        chirality.delete_at(first_err_pos)
        return try_again

    # Input is correct length, but we have swapped characters.
    # Try swapping at the first error unless it is at the end
    if first_err_pos >= expected_code_length - 1:
        return completed

    if chirality.get(first_err_pos) == chirality.get(first_err_pos + 1):
        # A simple swap won't fix this. Either code is totally wrong,
        # or there are repeated insertions and deletions.
        # For now, 'fake' the chirality so other checks can go forward
        chirality.set(first_err_pos, 1 - chirality.get(first_err_pos))
        return try_again

    # Probably a normal transpose. Swap characters
    codes.swap(first_err_pos, first_err_pos + 1)
    chirality.swap(first_err_pos, first_err_pos + 1)
    return try_again

def mc_decode_display(expected_code_length:int, input_str:str):
    """ Try to decode a string input, and correct transpositions """
    valid_char_count = 0

    input_length = len(input_str)
    for i in range(0, input_length):
        src = input_str[i]
        if src == '\0': # check for C end-of-string marker
            input_length = i
            break

        if mc_is_space(src): continue

        src = mc_case_changes(src.upper()) # all upper except Q->q, S->s, B->b
        src = mc_correction(src) # fix for anticipated transcription errors

        odd_idx = mc_index_of(_odd_set, src)
        even_idx = mc_index_of(_even_set, src)
        if (odd_idx >= 0) or (even_idx >= 0): valid_char_count += 1
        else: print(f"bad char: {src}")

    # negative = too many, positive = too few
    char_count_mismatch = expected_code_length - valid_char_count

    codes = fa_create(0, valid_char_count + 16)
    chirality = fa_create(0, valid_char_count + 16)

    next_chi = 0
    for i in range(0, input_length):
        src = input_str[i]
        if mc_is_space(src): continue

        src = mc_case_changes(src.upper()) # all upper except Q->q, S->s, B->b
        src = mc_correction(src) # fix for anticipated transcription errors

        odd_idx = mc_index_of(_odd_set, src)
        even_idx = mc_index_of(_even_set, src)

        if odd_idx < 0 and even_idx < 0:
            # Bad input char. Insert dummy if we think it's a mistype
            if char_count_mismatch > 0:
                codes.push(0)
                chirality.push(next_chi)
                next_chi = 1 - next_chi
                char_count_mismatch -= 1
            else:
                char_count_mismatch += 1
        elif odd_idx >= 0 and even_idx >= 0:
            # The character decoding is broken!
            codes.release()
            chirality.release()
            return fa_fixed(0)
        elif odd_idx >= 0:
            codes.push(odd_idx)
            chirality.push(0)
            next_chi = 1
        else:
            codes.push(even_idx)
            chirality.push(1)
            next_chi = 0

    # Try to fix chirality errors until there are none or we hit our fix limit
    for tries in range(0, expected_code_length):
        if mc_repair_codes_and_chirality(expected_code_length, codes, chirality) != 0:
            break

    chirality.release()
    return codes

def mc_try_hard_decode(msg: FlexArray, sym:int, expected_length:int):
    """ Try to decode input using Reed-Solomon """
    basic_decode = rs_decode(msg, sym, expected_length)
    if basic_decode.length() > 0: return basic_decode # success

    # Normal decoding didn't work.
    # Try rotations in case start/end deletions were guessed wrong

    end = msg.length()
    half = end // 2
    undo = 0

    # rotate left until we run out of zeros
    for i in range(0, half):
        r = msg.pop_first() # take off left

        if r != 0:
            msg.add_start(r) # put it back
            break

        undo += 1
        msg.push(r) # put on right

        # try decode again
        basic_decode = rs_decode(msg, sym, expected_length)
        if basic_decode.length() > 0: return basic_decode

    # Rotating left didn't work. Undo
    while undo > 0:
        undo -= 1
        r = msg.pop()
        msg.add_start(r)

    # rotate right until we run out of zeros
    for i in range(0, half):
        r = msg.pop() # take off right

        if r != 0:
            msg.push(r)
            break

        msg.add_start(r) # put on left

        basic_decode = rs_decode(msg, sym, expected_length)
        if basic_decode.length() > 0: return basic_decode

    return fa_fixed(0) # Did not find a solution

# endregion MultiCoder


def multi_code_encode(source:bytes, correction_symbols:int):
    """
    Encode binary data to a multi-code string
    :param source: source data to be encoded
    :param correction_symbols: count of correction symbols to add
    :return: multi-code for the source data
    """

    # convert bytes to nybbles
    data_len = len(source)
    src = fa_fixed(data_len * 2)
    j = 0
    for i in range(0, data_len):
        upper = (source[i] >> 4) & 0x0F
        lower = source[i] & 0x0F

        src.set(j, upper)
        j += 1
        src.set(j, lower)
        j += 1

    encoded = rs_encode(src, correction_symbols)
    output = mc_display(encoded)

    encoded.release()
    src.release()

    return output


def multi_code_decode(code:str, data_length:int, correction_symbols:int):
    """
    Decode a multi-code string to binary data
    :param code: the end-user input.
    :param data_length: number of bytes in ORIGINAL data
    :param correction_symbols: count of correction symbols added to code
    :return: recovered data or empty array on failure
    """
    expected_code_length = (data_length * 2) + correction_symbols
    clean_input = mc_decode_display(expected_code_length, code)

    if clean_input.length() < expected_code_length:
        # input too short
        clean_input.release()
        return []

    if clean_input.length() > expected_code_length:
        # input too long
        clean_input.release()
        return []

    decoded = mc_try_hard_decode(clean_input, correction_symbols, clean_input.length())

    if decoded.length() < 1: # failed to decode
        clean_input.release()
        return []

    # Decoded ok. Remove the error correction symbols
    for i in range(0, correction_symbols): decoded.pop()

    # Convert nybbles back to bytes
    length = decoded.length() // 2
    final = bytearray(length)

    for i in range(0, length):
        upper = (decoded.pop_first() << 4) & 0xF0
        lower = decoded.pop_first() & 0x0F
        final[i] = upper + lower

    return final


