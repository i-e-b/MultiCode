package pkg

var _exp = [32]int{}
var _log = [16]int{}

var _created = false

const _prime = 19 // must be fixed across implementations!

func setup() {
	x := 1

	for i := 0; i < 16; i++ {
		_exp[i] = x & 0x0f
		_log[x] = i & 0x0f
		x <<= 1
		if (x & 0x110) != 0 {
			x ^= _prime
		}
	}
	for i := 15; i < 32; i++ {
		_exp[i] = _exp[i-15] & 0x0f
	}
	_created = true
}

// addSub adds or subtracts (a +/- b)
func addSub(a int, b int) int {
	if !_created {
		setup()
	}
	return (a ^ b) & 0x0f
}

// mul multiplies a * b
func mul(a int, b int) int {
	if !_created {
		setup()
	}

	if a == 0 || b == 0 {
		return 0
	}
	return _exp[(_log[a]+_log[b])%15]
}

// div divides a / b
func div(a int, b int) int {
	if !_created {
		setup()
	}

	if a == 0 || b == 0 {
		return 0
	}
	return _exp[(_log[a]+15-_log[b])%15]
}

// pow raises n^p
func pow(n int, p int) int {
	if !_created {
		setup()
	}

	return _exp[(_log[n]*p)%15]
}

// inverse gives multiplicative inverse of n
func inverse(n int) int {
	if !_created {
		setup()
	}
	return _exp[15-_log[n]]
}

// polyMulScalar multiplies a polynomial 'p' by a scalar 'sc'
func polyMulScalar(p *flexArray, sc int) *flexArray {
	var res = flexArrayBySize(p.length())
	for i := 0; i < p.length(); i++ {
		res.set(i, mul(p.get(i), sc))
	}
	return res
}

// addPoly adds two polynomials
func addPoly(p *flexArray, q *flexArray) *flexArray {
	var length = max(p.length(), q.length())
	var res = flexArrayBySize(length)
	for i := 0; i < p.length(); i++ {
		var idx = i + length - p.length()
		res.set(idx, p.get(i))
	}
	for i := 0; i < q.length(); i++ {
		var idx = i + length - q.length()
		res.set(idx, res.get(idx)^q.get(i))
	}
	return res
}

// mulPoly multiplies two polynomials
func mulPoly(p *flexArray, q *flexArray) *flexArray {
	var res = flexArrayBySize(p.length() + q.length() - 1)
	for j := 0; j < q.length(); j++ {
		for i := 0; i < p.length(); i++ {
			var val = addSub(res.get(i+j), mul(p.get(i), q.get(j)))
			res.set(i+j, val)
		}
	}
	return res
}

// evalPoly evaluates polynomial 'p' for value 'x', resulting in a scalar
func evalPoly(p *flexArray, x int) int {
	var y = p.get(0)
	for i := 1; i < p.length(); i++ {
		y = mul(y, x) ^ p.get(i)
	}
	return y & 0x0f
}

// irreduciblePoly generates an irreducible polynomial for use in Reed-Solomon codes
func irreduciblePoly(symCount int) *flexArray {
	var gen = flexArraySingleOne()

	var next = flexArrayPair(1, 1)
	for i := 0; i < symCount; i++ {
		next.set(1, pow(2, i))
		var nextGen = mulPoly(gen, next)
		gen.release()
		gen = nextGen
	}
	next.release()
	return gen
}
