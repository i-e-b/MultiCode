// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable LoopCanBeConvertedToQuery
namespace MultiCode;

/// <summary>
/// 16-entry Galois field math for Reed-Solomon.
/// (4-bit per symbol)
/// </summary>
internal static class Galois16
{
    private static readonly int[] _exp = new int[32];
    private static readonly int[] _log = new int[16];

    private const int Prime = 19; // must be fixed across implementations!

    static Galois16()
    {
        var x = 1;

        for (var i = 0; i < 16; i++) {
            _exp[i] = x & 0x0f;
            _log[x] = i & 0x0f;
            x <<= 1;
            if ((x & 0x110) != 0) x ^= Prime;
            x &= 0x0f;
        }
        for (var i = 15; i < 32; i++) {
            _exp[i] = _exp[i - 15] & 0x0f;
        }
    }

    /// <summary>
    /// Add or Subtract: a +/- b
    /// </summary>
    public static int AddSub(int a, int b)
    {
        return (a ^ b) & 0x0f;
    }

    /// <summary>
    /// Multiply a and b
    /// </summary>
    public static int Mul(int a, int b)
    {
        if (a == 0 || b == 0) return 0;
        return _exp[(_log[a] + _log[b]) % 15];
    }

    /// <summary>
    /// Divide a by b
    /// </summary>
    public static int Div(int a, int b) {
        if (a == 0 || b == 0) return 0;
        return _exp[(_log[a] + 15 - _log[b]) % 15];
    }

    /// <summary>
    /// Raise n to power of p
    /// </summary>
    public static int Pow(int n, int p) {
        return _exp[(_log[n] * p) % 15];
    }

    /// <summary>
    /// Get multiplicative inverse of n
    /// </summary>
    public static int Inverse(int n) {
        return _exp[15 - _log[n]];
    }

    /// <summary>
    /// Multiply a polynomial 'p' by a scalar 'sc'
    /// </summary>
    public static FlexArray PolyMulScalar(FlexArray p, int sc) {
        var res = FlexArray.BySize(p.Length());
        for (var i = 0; i < p.Length(); i++) {
            res.Set(i, Mul(p.Get(i), sc));
        }
        return res;
    }

    /// <summary>
    /// Add two polynomials
    /// </summary>
    public static FlexArray AddPoly(FlexArray p, FlexArray q) {
        var len = p.Length() >= q.Length() ? p.Length() : q.Length();
        var res = FlexArray.BySize(len);
        for (var i = 0; i < p.Length(); i++)
        {
            var idx = i + len - p.Length();
            res.Set(idx, p.Get(i));
        }
        for (var i = 0; i < q.Length(); i++)
        {
            var idx = i + len - q.Length();
            res.Set(idx, res.Get(idx) ^ q.Get(i));
        }
        return res;
    }

    /// <summary>
    /// Multiply two polynomials
    /// </summary>
    public static FlexArray MulPoly(FlexArray p, FlexArray q) {
        var res = FlexArray.BySize(p.Length() + q.Length() - 1);
        for (var j = 0; j < q.Length(); j++) {
            for (var i = 0; i < p.Length(); i++) {
                var val = AddSub(res.Get(i + j), Mul(p.Get(i), q.Get(j)));
                res.Set(i + j, val);
            }
        }
        return res;
    }

    /// <summary>
    /// Evaluate polynomial 'p' for value 'x',
    /// resulting in a scalar
    /// </summary>
    public static int EvalPoly(FlexArray p, int x) {
        var y = p.Get(0);
        for (var i = 1; i < p.Length(); i++) {
            y = Mul(y, x) ^ p.Get(i);
        }
        return y & 0x0f;
    }

    /// <summary>
    /// Generate an irreducible polynomial for use
    /// in Reed-Solomon codes
    /// </summary>
    public static FlexArray IrreduciblePoly(int symCount)
    {
        var gen = FlexArray.SingleOne();

        var next = FlexArray.Pair(1, 1);
        for (var i = 0; i < symCount; i++)
        {
            next.Set(1, Pow(2, i));
            var nextGen = MulPoly(gen, next);
            gen.Release();
            gen = nextGen;
        }
        next.Release();
        return gen;
    }
}