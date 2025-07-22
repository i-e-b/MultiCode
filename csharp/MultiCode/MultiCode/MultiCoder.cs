using System;
using System.Text;

// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable LoopCanBeConvertedToQuery

namespace MultiCode;

/// <summary>
/// Encode and Decode multi-code strings
/// </summary>
public static class MultiCoder
{
    /// <summary>
    /// Encode binary data to a multi-code string
    /// </summary>
    /// <param name="data">original data to encode</param>
    /// <param name="correctionSymbols">number of extra bytes to add for error recovery</param>
    public static string Encode(byte[] data, int correctionSymbols)
    {
        // Convert from bytes to nybbles
        var src = FlexArray.Fixed(data.Length * 2);
        int j   = 0;
        for (int i = 0; i < data.Length; i++)
        {
            var upper = (data[i] >> 4) & 0x0F;
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

    /// <summary>
    /// Decode a multi-code string to binary data
    /// </summary>
    /// <param name="code">user input: encoded data to restore</param>
    /// <param name="dataLength">expected length of data in bytes, excluding correction symbols</param>
    /// <param name="correctionSymbols">number of extra bytes added to original data</param>
    public static byte[] Decode(string code, int dataLength, int correctionSymbols)
    {
        var transposes = FlexArray.BySize(0);

        var expectedCodeLength = (dataLength * 2) + correctionSymbols;
        var cleanInput         = DecodeDisplay(expectedCodeLength, code, transposes);

        if (cleanInput.Length() < expectedCodeLength) // Input too short
        {
            return [];
        }

        if (cleanInput.Length() > expectedCodeLength) // Input too long
        {
            return [];
        }

        transposes.Release();

        var decoded = TryHardDecode(cleanInput, correctionSymbols, cleanInput.Length());

        if (decoded.ok)
        {
            // remove recovery data
            for (var i = 0; i < correctionSymbols; i++) decoded.result?.Pop();

            // decoded data is nybbles, convert back to bytes
            var final = new byte[(decoded.result?.Length()??0) / 2];
            for (int i = 0; i < final.Length; i++)
            {
                var upper = (decoded.result?.PopFirst()??0) << 4;
                var lower = decoded.result?.PopFirst()??0;
                final[i] = (byte)(upper + lower);
            }
            cleanInput.Release();
            if (decoded.result != cleanInput) decoded.result?.Release();
            return final;
        }

        cleanInput.Release();
        if (decoded.result != cleanInput) decoded.result?.Release();
        return [];
    }

    /// <summary>
    /// Decode a multi-code string to binary data.
    /// </summary>
    /// <param name="code">encoded data to restore</param>
    /// <param name="target">byte array to fill, excluding error recovery bytes</param>
    /// <returns><c>true</c> if decode was successful,
    /// <c>false</c> if input is not recoverable</returns>
    public static bool TryDecode(string code, byte[] target)
    {
        throw new NotImplementedException();
    }

    #region Code parameters

    // Note: '~' is for error.
    // Q and S are lower cased to look less like 0 and 5.

    /// <summary> Characters for odd-positioned output codes </summary>
    private static readonly char[] OddSet = ['0', '1', '2', '3', '6', '7', '8', '9', 'b', 'G', 'J', 'N', 'q', 'X', 'Y', 'Z', '~'];

    /// <summary> Characters for even-positioned output codes  </summary>
    private static readonly char[] EvenSet = ['4', '5', 'A', 'C', 'D', 'E', 'F', 'H', 'K', 'M', 'P', 'R', 's', 'T', 'V', 'W', '~'];

    /// <summary>
    /// Look up characters likely to be entered as spaces. These will be trimmed from input
    /// </summary>
    private static bool IsSpace(char c)
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

    /// <summary> Likely mistakes. Mapped to characters we guess are correct </summary>
    private static char Correction(char inp)
    {
        switch (inp)
        {
            case 'O': return '0';
            case 'L': return '1';
            case 'I': return '1';
            case 'U': return 'V';
            default: return inp;
        }
    }

    /// <summary> Case changes to improve letter/number distinction </summary>
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

    #endregion Code parameters

    #region Internal

    /// <summary>
    /// Find index in char array, or -1 if not found
    /// </summary>
    private static int IndexOf(char[] src, char target)
    {
        for (int i = 0; i < src.Length; i++)
        {
            if (src[i] == target) return i;
        }

        return -1;
    }

    /// <summary>
    /// Message value, and message output position to encoded character
    /// </summary>
    private static char EncodeDisplay(int number, int position)
    {
        if (number < 0 || number > 15) return '~';
        if ((position & 1) == 0) return OddSet[number];
        return EvenSet[number];
    }

    /// <summary>
    /// Create an output string for message data
    /// </summary>
    private static string Display(FlexArray message)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < message.Length(); i++)
        {
            if (i > 0)
            {
                if (i % 4 == 0) sb.Append('-');
                else if (i % 2 == 0) sb.Append(' ');
            }

            sb.Append(EncodeDisplay(message.Get(i), i));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Find first position where chirality is incorrect
    /// </summary>
    private static int FindFirstChiralityError(FlexArray chirality)
    {
        for (var position = 0; position < chirality.Length(); position++)
        {
            var expected = position & 1;
            if (chirality.Get(position) != expected) return position;
        }

        return -1;
    }

    private static bool RepairCodesAndChirality(int expectedCodeLength,
        FlexArray codes, FlexArray chirality, FlexArray transposes)
    {
        const bool tryAgain  = false;
        const bool completed = true;

        if (codes.Length() != chirality.Length())
        {
            //log("ERROR in code/chirality code: length mismatch");
            return completed; // can't do much here
        }

        var currentLength = codes.Length();
        var minLength     = (2 * expectedCodeLength) / 3;

        if (currentLength < minLength)
        {
            //log('Code is too short to recover accurately');
            return completed;
        }

        // Note: splice(start, deleteCount, insertItems...);

        var firstErrPos = FindFirstChiralityError(chirality);
        if (currentLength == expectedCodeLength && firstErrPos < 0)
        {
            //log('Input codes seem correct');
            return completed;
        }

        // If input is shorter than expected, guess where a deletion occured, and insert a zero-value.
        if (currentLength < expectedCodeLength)
        {
            //log('Too short: ' + currentLength);

            // Insert a zero value at error position, and inject correct chirality
            if (firstErrPos < 0)
            {
                // error is at the end
                var chi  = currentLength & 1;
                var diff = expectedCodeLength - currentLength;
                if (diff == 1 && chi != 1)
                {
                    // don't add a wrong chi at the end if we're off-by-one
                    //log('adding 0 at start');
                    codes.AddStart(0);
                    chirality.AddStart(0);
                    transposes.Push(0);
                }
                else
                {
                    //log('adding ' + chi + ' at end');
                    codes.Push(0);
                    chirality.Push(chi);
                    transposes.Push(currentLength);
                }
            }
            else
            {
                var chi = firstErrPos & 1;
                //log('adding ' + chi + ' before ' + firstErrPos);
                codes.InsertAt(firstErrPos, 0);
                chirality.InsertAt(firstErrPos, chi);
                transposes.Push(firstErrPos);
            }

            return tryAgain;
        }

        // If input is longer than expected, guess where the problem is and delete
        if (currentLength > expectedCodeLength)
        {
            //log('Too long: ' + currentLength);

            // First, if the last code is bad chirality, delete that before anything else
            var expectedLastChi = (1 + expectedCodeLength) & 1;
            if (chirality.Get(currentLength - 1) != expectedLastChi)
            {
                //log(`Duplicated last character? Expected ${ expectedLastChi }, got ${ chirality[currentLength - 1] }`);
                codes.Pop();
                chirality.Pop();
                return tryAgain;
            }

            // value and chirality at error position
            if (firstErrPos < 0) firstErrPos = currentLength - 1;
            //log('deleting at ' + firstErrPos);
            codes.DeleteAt(firstErrPos);
            chirality.DeleteAt(firstErrPos);

            transposes.Push(firstErrPos);
            return tryAgain;
        }

        // Input is correct length, but we have swapped characters.
        // Try swapping at first error, unless it is at the end.
        if (firstErrPos >= expectedCodeLength - 1)
        {
            //log('Dangling chirality error');
            return completed;
        }

        if (chirality.Get(firstErrPos) == chirality.Get(firstErrPos + 1))
        {
            // A simple swap won't fix this. Either a totally wrong code, or repeated insertions and deletions.
            // For now, we will flip the chirality without changing anything so the checks can continue.

            //log('Invalid chirality pattern at ' + firstErrPos);
            chirality.Set(firstErrPos, 1 - chirality.Get(firstErrPos));

            transposes.Push(firstErrPos);
            return tryAgain;
        }

        // swapping characters might fix the problem
        //log('Chirality error at ' + firstErrPos);
        codes.Swap(firstErrPos, firstErrPos + 1);
        chirality.Swap(firstErrPos, firstErrPos + 1);

        transposes.Push(firstErrPos);
        return tryAgain;
    }

    private static FlexArray DecodeDisplay(int expectedCodeLength, string input, FlexArray transposes)
    {
        var codes          = FlexArray.BySize(0);
        var chirality      = FlexArray.BySize(0);
        var validCharCount = 0;

        // Run filters first, to get the number of 'correct' characters.
        // We could extend this to store the location of unexpected chars to improve the next loop.
        for (var i = 0; i < input.Length; i++)
        {
            var src = char.ToUpperInvariant(input[i]);
            if (IsSpace(src)) continue; // skip spaces
            src = CaseChanges(src); // Q->q, S->s, B->b

            var oddIdx  = IndexOf(OddSet, src);
            var evenIdx = IndexOf(EvenSet, src);
            if (oddIdx >= 0 || evenIdx >= 0) validCharCount++;
        }

        // negative = too many chars. Positive = too few.
        var charCountMismatch = expectedCodeLength - validCharCount;

        var nextChir = 0;
        for (var i = 0; i < input.Length; i++)
        {
            var src = char.ToUpperInvariant(input[i]); // make upper-case

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

    /// <summary>
    /// Main RS encode.
    /// </summary>
    /// <param name="msg">array of ints in 0..15</param>
    /// <param name="sym">number of additional symbols</param>
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
        var result = new DecodeResult
        {
            ok = false,
            errs = false,
            result = msg.Copy(),
            info = ""
        };

        var erases = expectedLength - msg.Length();
        var synd   = ReedSolomon.CalcSyndromes(msg, sym);
        if (synd.AllZero())
        {
            //log('no errors found');
            result.ok = true;
            synd.Release();
            return result;
        }

        var errPoly = ReedSolomon.ErrorLocatorPoly(synd, sym, erases);
        if (errPoly.Length() - 1 - erases > sym)
        {
            result.errs = true;
            result.info = "too many errors (A)";
            errPoly.Release();
            synd.Release();
            return result;
        }

        errPoly.Reverse();
        var errorPositions = ReedSolomon.FindErrors(errPoly, msg.Length());
        if (errorPositions.Length() < 1)
        {
            result.errs = true;
            result.info = "too many errors (B)";
            errorPositions.Release();
            errPoly.Release();
            synd.Release();
            return result;
        }

        errorPositions.Reverse();
        result.result?.Release();
        result.result = ReedSolomon.CorrectErrors(msg, synd, errorPositions);

        errorPositions.Release();
        errPoly.Release();
        synd.Release();

        // recheck result
        var synd2 = ReedSolomon.CalcSyndromes(result.result, sym);
        if (synd2.AllZero())
        {
            //log('all errors corrected');
            synd2.Release();
            result.ok = true;
            result.errs = true;
            result.info = "all errors corrected";
            return result;
        }

        synd2.Release();

        result.ok = false;
        result.errs = true;
        result.info = "too many errors (C)";
        return result;
    }

    private static DecodeResult TryHardDecode(FlexArray msg, int sym, int expectedLength)
    {
        var basicDecode = RsDecode(msg, sym, expectedLength);
        if (basicDecode.ok) return basicDecode;

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

            basicDecode.result?.Release();
            basicDecode = RsDecode(msg, sym, expectedLength);
            if (basicDecode.ok) return basicDecode;
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

            basicDecode.result?.Release();
            basicDecode = RsDecode(msg, sym, expectedLength);
            if (basicDecode.ok) return basicDecode;
        }

        return basicDecode;
    }

    #endregion Internal
}