package com.ieb.multicode_demo.core;

/**
 * Encode and Decode multi-code strings
 * @noinspection ForLoopReplaceableByForEach
 */
public class MultiCoder {

    /**
     * Encode binary data to a multi-code string
     * @param data original data to encode
     * @param correctionSymbols number of extra symbols to add for error recovery
     * @return String for end-user consumption
     */
    public static String Encode(byte[] data, int correctionSymbols)
    {
        // Convert from bytes to nybbles
        var src = FlexArray.Fixed(data.length * 2);
        int j   = 0;
        for (int i = 0; i < data.length; i++)
        {
            var upper = (data[i] >>> 4) & 0x0F;
            var lower = data[i] & 0x0F;

            src.Set(j++, upper);
            src.Set(j++, lower);
        }

        var encoded = RsEncode(src, correctionSymbols);

        var output  = Display(encoded);

        encoded.Release();
        src.Release();

        return output;
    }

    /**
     * Decode a multi-code string to binary data
     * @param code user input: encoded data to restore
     * @param dataLength expected length of data in bytes, excluding correction symbols
     * @param correctionSymbols number of extra bytes added to original data<
     * @return recovered data, or empty array
     */
    public static byte[] Decode(String code, int dataLength, int correctionSymbols)
    {
        var transposes = FlexArray.BySize(0);

        var expectedCodeLength = (dataLength * 2) + correctionSymbols;
        var cleanInput         = DecodeDisplay(expectedCodeLength, code, transposes);

        if (cleanInput.Length() < expectedCodeLength) // Input too short
        {
            return new byte[0];
        }

        if (cleanInput.Length() > expectedCodeLength) // Input too long
        {
            return new byte[0];
        }

        transposes.Release();

        var decoded = TryHardDecode(cleanInput, correctionSymbols, cleanInput.Length());

        if (decoded.Ok)
        {
            // remove recovery data
            for (var i = 0; i < correctionSymbols; i++) decoded.Result.Pop();

            // decoded data is nybbles, convert back to bytes
            var finalData = new byte[decoded.Result.Length() / 2];
            for (int i = 0; i < finalData.length; i++)
            {
                var upper = decoded.Result.PopFirst() << 4;
                var lower = decoded.Result.PopFirst();
                finalData[i] = (byte)(upper + lower);
            }
            cleanInput.Release();
            if (decoded.Result != cleanInput) decoded.Release();
            return finalData;
        }

        cleanInput.Release();
        if (decoded.Result != cleanInput) decoded.Release();
        return new byte[0];
    }


    // Note: '~' is for error.
    // Q and S are lower cased to look less like 0 and 5.

    /** Characters for odd-positioned output codes */
    private static final char[] OddSet = new char[] {'0', '1', '2', '3', '6', '7', '8', '9', 'b', 'G', 'J', 'N', 'q', 'X', 'Y', 'Z', '~'};

    /** <summary> Characters for even-positioned output codes */
    private static final char[] EvenSet = new char[] {'4', '5', 'A', 'C', 'D', 'E', 'F', 'H', 'K', 'M', 'P', 'R', 's', 'T', 'V', 'W', '~'};

    /**
     * Look up characters likely to be entered as spaces. These will be trimmed from input
     */
    private static boolean IsSpace(char c)
    {
        switch (c)
        {
            case ' ':
            case '-':
            case '.':
            case '_':
            case '+':
            case '*':
            case '#':
                return true;
            default: return false;
        }
    }

    /** Likely mistakes. Mapped to characters we guess are correct */
    private static char Correction(char inp)
    {
        switch (inp)
        {
            case 'O': return '0';
            case 'L':
            case 'I':
                return '1';
            case 'U': return 'V';
            default: return inp;
        }
    }

    /** Case changes to improve letter/number distinction */
    private static char CaseChanges(char inp)
    {
        switch (inp)
        {
            case 'B': return 'b';
            case 'Q': return 'q';
            case 'S': return 's';
            default: return inp;
        }
    }

    /**
     * Find index in char array, or -1 if not found
     */
    private static int IndexOf(char[] src, char target)
    {
        for (int i = 0; i < src.length; i++)
        {
            if (src[i] == target) return i;
        }

        return -1;
    }

    /**
     * Message value, and message output position to encoded character
     */
    private static char EncodeDisplay(int number, int position)
    {
        if (number < 0 || number > 15) return '~';
        if ((position & 1) == 0) return OddSet[number];
        return EvenSet[number];
    }

    /**
     * Create an output string for message data
     */
    private static String Display(FlexArray message)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < message.Length(); i++)
        {
            if (i > 0)
            {
                if (i % 4 == 0) sb.append('-');
                else if (i % 2 == 0) sb.append(' ');
            }

            sb.append(EncodeDisplay(message.Get(i), i));
        }

        return sb.toString();
    }

    /**
     * Find first position where chirality is incorrect
     */
    private static int FindFirstChiralityError(FlexArray chirality)
    {
        for (var position = 0; position < chirality.Length(); position++)
        {
            var expected = position & 1;
            if (chirality.Get(position) != expected) return position;
        }

        return -1;
    }

    private static boolean RepairCodesAndChirality(int expectedCodeLength,
                                                FlexArray codes, FlexArray chirality, FlexArray transposes)
    {
        final boolean tryAgain  = false;
        final boolean completed = true;

        if (codes.Length() != chirality.Length())
        {
            return completed; // can't do much here
        }

        var currentLength = codes.Length();
        var minLength     = (2 * expectedCodeLength) / 3;

        if (currentLength < minLength)
        {
            // Code is too short to recover accurately
            return completed;
        }

        var firstErrPos = FindFirstChiralityError(chirality);
        if (currentLength == expectedCodeLength && firstErrPos < 0)
        {
            // Input codes seem correct
            return completed;
        }

        // If input is shorter than expected, guess where a deletion occurred, and insert a zero-value.
        if (currentLength < expectedCodeLength)
        {
            // Insert a zero value at error position, and inject correct chirality
            if (firstErrPos < 0)
            {
                // error is at the end
                var chi  = currentLength & 1;
                var diff = expectedCodeLength - currentLength;
                if (diff == 1 && chi != 1)
                {
                    // don't add a wrong chi at the end if we're off-by-one
                    codes.AddStart(0);
                    chirality.AddStart(0);
                    transposes.Push(0);
                }
                else
                {
                    codes.Push(0);
                    chirality.Push(chi);
                    transposes.Push(currentLength);
                }
            }
            else
            {
                var chi = firstErrPos & 1;
                codes.InsertAt(firstErrPos, 0);
                chirality.InsertAt(firstErrPos, chi);
                transposes.Push(firstErrPos);
            }

            return tryAgain;
        }

        // If input is longer than expected, guess where the problem is and delete
        if (currentLength > expectedCodeLength)
        {
            // First, if the last code is bad chirality, delete that before anything else
            var expectedLastChi = (1 + expectedCodeLength) & 1;
            if (chirality.Get(currentLength - 1) != expectedLastChi)
            {
                codes.Pop();
                chirality.Pop();
                return tryAgain;
            }

            // value and chirality at error position
            if (firstErrPos < 0) firstErrPos = currentLength - 1;
            codes.DeleteAt(firstErrPos);
            chirality.DeleteAt(firstErrPos);

            transposes.Push(firstErrPos);
            return tryAgain;
        }

        // Input is correct length, but we have swapped characters.
        // Try swapping at first error, unless it is at the end.
        if (firstErrPos >= expectedCodeLength - 1)
        {
            return completed;
        }

        if (chirality.Get(firstErrPos) == chirality.Get(firstErrPos + 1))
        {
            // A simple swap won't fix this. Either a totally wrong code, or repeated insertions and deletions.
            // For now, we will flip the chirality without changing anything so the checks can continue.

            chirality.Set(firstErrPos, 1 - chirality.Get(firstErrPos));

            transposes.Push(firstErrPos);
            return tryAgain;
        }

        // swapping characters might fix the problem
        codes.Swap(firstErrPos, firstErrPos + 1);
        chirality.Swap(firstErrPos, firstErrPos + 1);

        transposes.Push(firstErrPos);
        return tryAgain;
    }

    private static FlexArray DecodeDisplay(int expectedCodeLength, String input, FlexArray transposes)
    {
        var codes          = FlexArray.BySize(0);
        var chirality      = FlexArray.BySize(0);
        var validCharCount = 0;

        // Run filters first, to get the number of 'correct' characters.
        // We could extend this to store the location of unexpected chars to improve the next loop.
        for (var i = 0; i < input.length(); i++)
        {
            var src = Character.toUpperCase(input.charAt(i));
            if (IsSpace(src)) continue; // skip spaces
            src = CaseChanges(src); // Q->q, S->s, B->b

            var oddIdx  = IndexOf(OddSet, src);
            var evenIdx = IndexOf(EvenSet, src);
            if (oddIdx >= 0 || evenIdx >= 0) validCharCount++;
        }

        // negative = too many chars. Positive = too few.
        var charCountMismatch = expectedCodeLength - validCharCount;

        var nextChir = 0;
        for (var i = 0; i < input.length(); i++)
        {
            var src = Character.toUpperCase(input.charAt(i)); // make upper-case

            if (IsSpace(src)) continue; // skip spaces

            src = CaseChanges(src); // Q->q, S->s, B->b
            src = Correction(src); // fix for anticipated transcription errors

            var oddIdx  = IndexOf(OddSet, src);
            var evenIdx = IndexOf(EvenSet, src);

            if (oddIdx < 0 && evenIdx < 0)
            {
                // Broken character, maybe insert dummy.
                if (charCountMismatch > 0)
                {
                    codes.Push(0);
                    chirality.Push(nextChir);
                    nextChir = 1 - nextChir;
                    charCountMismatch--;
                }
                else
                {
                    charCountMismatch++;
                }
            }
            else if (oddIdx >= 0 && evenIdx >= 0)
            {
                // Should never happen!
                codes.Release();
                chirality.Release();
                return FlexArray.Fixed(0);
            }
            else if (oddIdx >= 0)
            {
                codes.Push(oddIdx);
                chirality.Push(0);
                nextChir = 1;
            }
            else
            {
                codes.Push(evenIdx);
                chirality.Push(1);
                nextChir = 0;
            }
        }

        for (var tries = 0; tries < 12; tries++)
        {
            if (RepairCodesAndChirality(expectedCodeLength, codes, chirality, transposes))
            {
                chirality.Release();
                return codes;
            }
        }

        chirality.Release();
        return codes;
    }

    /**
     * Main RS encode
     * @param msg array of ints in 0..15
     * @param sym number of additional symbols
     * @return flex array of encoded symbols
     */
    private static FlexArray RsEncode(FlexArray msg, int sym)
    {
        var gen = Galois16.IrreduciblePoly(sym);
        var mix = FlexArray.BySize(msg.Length() + gen.Length() - 1);
        for (var i = 0; i < msg.Length(); i++)
        {
            mix.Set(i, msg.Get(i));
        }

        for (var i = 0; i < msg.Length(); i++)
        {
            var coeff = mix.Get(i);
            if (coeff == 0) continue;
            for (var j = 1; j < gen.Length(); j++)
            {
                var next = mix.Get(i + j) ^ Galois16.Mul(gen.Get(j), coeff);
                mix.Set(i + j, next);
            }
        }

        var outp = FlexArray.BySize(0);
        var len  = msg.Length() + gen.Length() - 1;
        for (var i = 0; i < len; i++) outp.Push(mix.Get(i));

        for (var i = 0; i < msg.Length(); i++)
        {
            outp.Set(i, msg.Get(i));
        }

        gen.Release();
        mix.Release();
        return outp;
    }

    /// <summary>
    /// Main decode and correct function
    /// </summary>
    /// <param name="msg">input symbols</param>
    /// <param name="sym">additional check symbols in input</param>
    /// <param name="expectedLength">expected length of input</param>
    private static DecodeResult RsDecode(FlexArray msg, int sym, int expectedLength)
    {
        var result = new DecodeResult();
        result.Ok = false;
        result.Errs = false;
        result.Result = msg.Copy();
        result.Info = "";

        var erases = expectedLength - msg.Length();
        var synd   = ReedSolomon.CalcSyndromes(msg, sym);
        if (synd.AllZero())
        {
            // no errors found
            result.Ok = true;
            synd.Release();
            return result;
        }

        var errPoly = ReedSolomon.ErrorLocatorPoly(synd, sym, erases);
        if (errPoly.Length() - 1 - erases > sym)
        {
            result.Errs = true;
            result.Info = "too many errors (A)";
            errPoly.Release();
            synd.Release();
            return result;
        }

        errPoly.Reverse();
        var errorPositions = ReedSolomon.FindErrors(errPoly, msg.Length());
        if (errorPositions.Length() < 1)
        {
            result.Errs = true;
            result.Info = "too many errors (B)";
            errorPositions.Release();
            errPoly.Release();
            synd.Release();
            return result;
        }

        errorPositions.Reverse();
        result.Release();
        result.Result = ReedSolomon.CorrectErrors(msg, synd, errorPositions);

        errorPositions.Release();
        errPoly.Release();
        synd.Release();

        // recheck result
        var synd2 = ReedSolomon.CalcSyndromes(result.Result, sym);
        if (synd2.AllZero())
        {
            // all errors corrected
            synd2.Release();
            result.Ok = true;
            result.Errs = true;
            result.Info = "all errors corrected";
            return result;
        }

        synd2.Release();

        result.Ok = false;
        result.Errs = true;
        result.Info = "too many errors (C)";
        return result;
    }

    private static DecodeResult TryHardDecode(FlexArray msg, int sym, int expectedLength)
    {
        var basicDecode = RsDecode(msg, sym, expectedLength);
        if (basicDecode.Ok) return basicDecode;

        // Normal decoding didn't work. Try rotations

        var end  = msg.Length();
        var half = end / 2;
        int i;
        for (i = 0; i < half; i++)
        {
            // rotate left until we run out of zeros
            var r = msg.PopFirst(); // remove and return first element
            if (r != 0)
            {
                msg.AddStart(r);
                break;
            }

            msg.Push(r);

            basicDecode.Release();
            basicDecode = RsDecode(msg, sym, expectedLength);
            if (basicDecode.Ok) return basicDecode;
        }

        // undo
        while (i > 0)
        {
            i--;
            var r = msg.Pop();
            msg.AddStart(r);
        }

        for (i = 0; i < half; i++)
        {
            // rotate right until we run out of zeros
            var r = msg.Pop();
            if (r != 0)
            {
                msg.Push(r);
                break;
            }

            msg.AddStart(r);

            basicDecode.Release();
            basicDecode = RsDecode(msg, sym, expectedLength);
            if (basicDecode.Ok) return basicDecode;
        }

        return basicDecode;
    }

    /**
     * Result from RsDecode
     */
    static class DecodeResult
    {
        public boolean Ok;
        public boolean Errs;
        public FlexArray Result;
        public String Info = "";

        /**
         * Release result if there is one
         */
        public void Release()
        {
            if (Result != null) Result.Release();
        }
    }

}
