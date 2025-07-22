package com.ieb.multicode_demo.core;

/** 16-entry Galois field math for Reed-Solomon (4-bit per symbol) */
class Galois16 {
    private static boolean _created = false;
    private static final int[] _exp = new int[32];
    private static final int[] _log = new int[16];

    private static final int Prime = 19; // must be fixed across implementations!

    private static synchronized void BuildTables()
    {
        var x = 1;
        _created = true;

        for (var i = 0; i < 16; i++) {
            _exp[i] = x & 0x0f;
            _log[x] = i & 0x0f;
            x <<= 1;
            if ((x & 0x110) != 0) x ^= Prime;
        }
        for (var i = 15; i < 32; i++) {
            _exp[i] = _exp[i - 15] & 0x0f;
        }
    }

    /**
     * Add or Subtract: a +/- b
     */
    public static int AddSub(int a, int b)
    {
        if (!_created) BuildTables();
        return (a ^ b) & 0x0f;
    }

    /**
     * Multiply a and b
     */
    public static int Mul(int a, int b)
    {
        if (!_created) BuildTables();
        if (a == 0 || b == 0) return 0;
        return _exp[(_log[a] + _log[b]) % 15];
    }

    /**
     * Divide a by b
     */
    public static int Div(int a, int b) {
        if (!_created) BuildTables();
        if (a == 0 || b == 0) return 0;
        return _exp[(_log[a] + 15 - _log[b]) % 15];
    }

    /**
     * Raise n to power of p
     */
    public static int Pow(int n, int p) {
        if (!_created) BuildTables();
        return _exp[(_log[n] * p) % 15];
    }

    /**
     * Get multiplicative inverse of n
     */
    public static int Inverse(int n) {
        if (!_created) BuildTables();
        return _exp[15 - _log[n]];
    }

    /**
     * Multiply a polynomial 'p' by a scalar 'sc'
     */
    public static FlexArray PolyMulScalar(FlexArray p, int sc) {
        if (!_created) BuildTables();
        var res = FlexArray.BySize(p.Length());
        for (var i = 0; i < p.Length(); i++) {
            res.Set(i, Mul(p.Get(i), sc));
        }
        return res;
    }

    /**
     * Add two polynomials
     */
    public static FlexArray AddPoly(FlexArray p, FlexArray q) {
        if (!_created) BuildTables();
        var len = Math.max(p.Length(), q.Length());
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

    /**
     * Multiply two polynomials
     */
    public static FlexArray MulPoly(FlexArray p, FlexArray q) {
        if (!_created) BuildTables();
        var res = FlexArray.BySize(p.Length() + q.Length() - 1);
        for (var j = 0; j < q.Length(); j++) {
            for (var i = 0; i < p.Length(); i++) {
                var val = AddSub(res.Get(i + j), Mul(p.Get(i), q.Get(j)));
                res.Set(i + j, val);
            }
        }
        return res;
    }

    /**
     * Evaluate polynomial 'p' for value 'x',
     * resulting in a scalar
     */
    public static int EvalPoly(FlexArray p, int x) {
        if (!_created) BuildTables();
        var y = p.Get(0);
        for (var i = 1; i < p.Length(); i++) {
            y = Mul(y, x) ^ p.Get(i);
        }
        return y & 0x0f;
    }

    /**
     * Generate an irreducible polynomial for use
     * in Reed-Solomon codes
     */
    public static FlexArray IrreduciblePoly(int symCount)
    {
        if (!_created) BuildTables();
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
