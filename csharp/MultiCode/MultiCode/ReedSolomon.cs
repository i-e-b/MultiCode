// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable LoopCanBeConvertedToQuery

namespace MultiCode;

/// <summary>
/// Reed-Solomon FEC codes for multi-coder
/// </summary>
internal static class ReedSolomon
{
    /// <summary>
    /// Find locations of symbols that do not match the Reed-Solomon polynomial
    /// </summary>
    public static FlexArray CalcSyndromes(FlexArray msg, int sym) {
        var syndromes = FlexArray.BySize(sym + 1);
        for (var i = 0; i < sym; i++) {
            syndromes.Set(i + 1, Galois16.EvalPoly(msg, Galois16.Pow(2, i)));
        }
        return syndromes;
    }

    /// <summary>
    /// Build a polynomial to location errors in the message
    /// </summary>
    public static FlexArray ErrorLocatorPoly(FlexArray synd, int sym, int erases) {
        var errLoc = FlexArray.SingleOne();
        var oldLoc = FlexArray.SingleOne();

        var syndShift = 0;
        if (synd.Length() > sym) syndShift = synd.Length() - sym;

        for (var i = 0; i < sym - erases; i++) {
            var kappa = i + syndShift;
            var delta = synd.Get(kappa);
            for (var j = 1; j < errLoc.Length(); j++) {
                delta ^= Galois16.Mul(errLoc.Get(errLoc.Length() - (j + 1)), synd.Get(kappa - j));
            }
            oldLoc.Push(0);
            if (delta != 0) {
                if (oldLoc.Length() > errLoc.Length()) {
                    var newLoc = Galois16.PolyMulScalar(oldLoc, delta);
                    oldLoc.Release();
                    oldLoc = Galois16.PolyMulScalar(errLoc, Galois16.Inverse(delta));
                    errLoc.Release();
                    errLoc = newLoc;
                }
                var scale = Galois16.PolyMulScalar(oldLoc, delta);
                var nextErrLoc = Galois16.AddPoly(errLoc, scale);
                errLoc.Release();
                errLoc = nextErrLoc;
                scale.Release();
            }
        }
        oldLoc.Release();

        errLoc.TrimLeadingZero();
        return errLoc;
    }

    /// <summary>
    /// Find error locations
    /// </summary>
    public static FlexArray FindErrors(FlexArray locPoly, int len) {
        var errs = locPoly.Length() - 1;
        var pos  = FlexArray.BySize(0);

        for (int i = 0; i < len; i++) {
            int test = Galois16.EvalPoly(locPoly, Galois16.Pow(2, i)) & 0x0f;
            if (test == 0) {
                pos.Push(len - 1 - i);
            }
        }

        if (pos.Length() != errs)
        {
            pos.Clear();
        }

        return pos;
    }

    /// <summary>
    /// Build polynomial to find data errors
    /// </summary>
    private static FlexArray DataErrorLocatorPoly(FlexArray pos) {
        var eLoc = FlexArray.SingleOne();
        var s1   = FlexArray.SingleOne();
        var pair = FlexArray.BySize(2);

        for (var i = 0; i < pos.Length(); i++)
        {
            pair.Clear();
            pair.Push(Galois16.Pow(2, pos.Get(i)));
            pair.Push(0);

            var add = Galois16.AddPoly(s1, pair);
            var nextELoc = Galois16.MulPoly(eLoc, add);
            eLoc.Release();
            eLoc = nextELoc;
            add.Release();
        }

        pair.Release();
        s1.Release();

        return eLoc;
    }

    /// <summary>
    /// Try to evaluate a data error
    /// </summary>
    private static FlexArray ErrorEvaluator(FlexArray synd, FlexArray errLoc, int n) {
        var poly = Galois16.MulPoly(synd, errLoc);
        var len  = poly.Length() - (n + 1);
        for (var i = 0; i < len; i++) {
            poly.Set(i, poly.Get(i + len));
        }

        poly.TrimEnd(len);
        return poly;
    }

    /// <summary>
    /// Try to correct errors in the message using the Forney algorithm
    /// </summary>
    public static FlexArray CorrectErrors(FlexArray msg, FlexArray synd, FlexArray pos) {
        var len      = msg.Length();

        var coeffPos = FlexArray.BySize(0);
        var chi      = FlexArray.BySize(0);
        var tmp      = FlexArray.BySize(0);
        var e        = FlexArray.BySize(len);

        synd.Reverse();
        for (var i = 0; i < pos.Length(); i++) {
            coeffPos.Push(len - 1 - pos.Get(i));
        }

        var errLoc  = DataErrorLocatorPoly(coeffPos);
        var errEval = ErrorEvaluator(synd, errLoc, errLoc.Length() - 1);

        for (var i = 0; i < coeffPos.Length(); i++) {
            chi.Push(Galois16.Pow(2, coeffPos.Get(i)));
        }

        for (var i = 0; i < chi.Length(); i++) {
            tmp.Clear();
            var ichi = Galois16.Inverse(chi.Get(i));
            for (var j = 0; j < chi.Length(); j++) {
                if (i == j) continue;
                tmp.Push(Galois16.AddSub(1, Galois16.Mul(ichi, chi.Get(j))));
            }

            var prime = 1;
            for (var k = 0; k < tmp.Length(); k++) {
                prime = Galois16.Mul(prime, tmp.Get(k));
            }

            var y = Galois16.EvalPoly(errEval, ichi);
            y = Galois16.Mul(Galois16.Pow(chi.Get(i), 1), y); // pow?
            e.Set(pos.Get(i), Galois16.Div(y, prime));
        }

        var final = Galois16.AddPoly(msg, e);

        errEval.Release();
        errLoc.Release();
        e.Release();
        tmp.Release();
        chi.Release();
        coeffPos.Release();

        return final;
    }
}