package pkg

// calcSyndromes finds locations of symbols that do not match the Reed-Solomon polynomial
func calcSyndromes(msg *flexArray, sym int) *flexArray {
	var syndromes = flexArrayBySize(sym + 1)
	for i := 0; i < sym; i++ {
		syndromes.set(i+1, evalPoly(msg, pow(2, i)))
	}
	return syndromes
}

// errorLocatorPoly builds a polynomial to location errors in the message
func errorLocatorPoly(syndrome *flexArray, sym int, erases int) *flexArray {
	var errLoc = flexArraySingleOne()
	var oldLoc = flexArraySingleOne()

	var syndromeShift = 0
	if syndrome.length() > sym {
		syndromeShift = syndrome.length() - sym
	}

	for i := 0; i < sym-erases; i++ {
		var kappa = i + syndromeShift
		var delta = syndrome.get(kappa)
		for j := 1; j < errLoc.length(); j++ {
			delta ^= mul(errLoc.get(errLoc.length()-(j+1)), syndrome.get(kappa-j))
		}
		oldLoc.push(0)
		if delta != 0 {
			if oldLoc.length() > errLoc.length() {
				var newLoc = polyMulScalar(oldLoc, delta)
				oldLoc.release()
				oldLoc = polyMulScalar(errLoc, inverse(delta))
				errLoc.release()
				errLoc = newLoc
			}
			var scale = polyMulScalar(oldLoc, delta)
			var nextErrLoc = addPoly(errLoc, scale)
			errLoc.release()
			errLoc = nextErrLoc
			scale.release()
		}
	}
	oldLoc.release()

	errLoc.trimLeadingZero()
	return errLoc
}

// findErrors finds error locations
func findErrors(locPoly *flexArray, length int) *flexArray {
	var errs = locPoly.length() - 1
	var pos = flexArrayBySize(0)

	for i := 0; i < length; i++ {
		test := evalPoly(locPoly, pow(2, i)) & 0x0f
		if test == 0 {
			pos.push(length - 1 - i)
		}
	}

	if pos.length() != errs {
		pos.clear()
	}

	return pos
}

// dataErrorLocatorPoly builds a polynomial to find data errors
func dataErrorLocatorPoly(pos *flexArray) *flexArray {
	var eLoc = flexArraySingleOne()
	var s1 = flexArraySingleOne()
	var pair = flexArrayBySize(2)

	for i := 0; i < pos.length(); i++ {
		pair.clear()
		pair.push(pow(2, pos.get(i)))
		pair.push(0)

		var add = addPoly(s1, pair)
		var nextELoc = mulPoly(eLoc, add)
		eLoc.release()
		eLoc = nextELoc
		add.release()
	}

	pair.release()
	s1.release()

	return eLoc
}

// errorEvaluator tries to evaluate a data error
func errorEvaluator(syndrome *flexArray, errLoc *flexArray, n int) *flexArray {
	var poly = mulPoly(syndrome, errLoc)
	var length = poly.length() - (n + 1)
	for i := 0; i < length; i++ {
		poly.set(i, poly.get(i+length))
	}

	poly.trimEnd(length)
	return poly
}

// correctErrors tries to correct errors in the message using the Forney algorithm
func correctErrors(msg, syndrome, pos *flexArray) *flexArray {
	var length = msg.length()

	var coeffPos = flexArrayBySize(0)
	var chi = flexArrayBySize(0)
	var tmp = flexArrayBySize(0)
	var e = flexArrayBySize(length)

	syndrome.reverse()
	for i := 0; i < pos.length(); i++ {
		coeffPos.push(length - 1 - pos.get(i))
	}

	var errLoc = dataErrorLocatorPoly(coeffPos)
	var errEval = errorEvaluator(syndrome, errLoc, errLoc.length()-1)

	for i := 0; i < coeffPos.length(); i++ {
		chi.push(pow(2, coeffPos.get(i)))
	}

	for i := 0; i < chi.length(); i++ {
		tmp.clear()
		var iChi = inverse(chi.get(i))
		for j := 0; j < chi.length(); j++ {
			if i == j {
				continue
			}
			tmp.push(addSub(1, mul(iChi, chi.get(j))))
		}

		var prime = 1
		for k := 0; k < tmp.length(); k++ {
			prime = mul(prime, tmp.get(k))
		}

		var y = evalPoly(errEval, iChi)
		y = mul(pow(chi.get(i), 1), y) // pow?
		e.set(pos.get(i), div(y, prime))
	}

	var final = addPoly(msg, e)

	errEval.release()
	errLoc.release()
	e.release()
	tmp.release()
	chi.release()
	coeffPos.release()

	return final
}

// rsEncode adds check symbols to a message
func rsEncode(msg *flexArray, sym int) *flexArray {
	var gen = irreduciblePoly(sym)
	var mix = flexArrayBySize(msg.length() + gen.length() - 1)
	for i := 0; i < msg.length(); i++ {
		mix.set(i, msg.get(i))
	}

	for i := 0; i < msg.length(); i++ {
		var coeff = mix.get(i)
		if coeff == 0 {
			continue
		}
		for j := 1; j < gen.length(); j++ {
			var next = mix.get(i+j) ^ mul(gen.get(j), coeff)
			mix.set(i+j, next)
		}
	}

	var output = flexArrayBySize(0)
	var length = msg.length() + gen.length() - 1
	for i := 0; i < length; i++ {
		output.push(mix.get(i))
	}

	for i := 0; i < msg.length(); i++ {
		output.set(i, msg.get(i))
	}

	gen.release()
	mix.release()
	return output
}

// rsDecode is the main decode and correct function for message symbols
func rsDecode(msg *flexArray, sym, expectedLength int) *flexArray {
	var erases = expectedLength - msg.length()
	var syndrome = calcSyndromes(msg, sym)
	if syndrome.allZero() {
		syndrome.release()
		return msg
	}

	var errPoly = errorLocatorPoly(syndrome, sym, erases)
	if errPoly.length()-1-erases > sym {
		errPoly.release()
		syndrome.release()
		return flexArrayFixed(0)
	}

	errPoly.reverse()
	var errorPositions = findErrors(errPoly, msg.length())
	if errorPositions.length() < 1 {
		errorPositions.release()
		errPoly.release()
		syndrome.release()
		return flexArrayFixed(0)
	}

	errorPositions.reverse()
	result := correctErrors(msg, syndrome, errorPositions)

	errorPositions.release()
	errPoly.release()
	syndrome.release()

	// recheck result
	var syndrome2 = calcSyndromes(result, sym)
	if syndrome2.allZero() {
		syndrome2.release()
		return result
	}

	syndrome2.release()
	result.release()
	return flexArrayFixed(0)
}
