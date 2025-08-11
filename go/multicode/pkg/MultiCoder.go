package pkg

import (
	"strings"
	"unicode"
)

// Encode converts binary data to a multi-code string
func Encode(data []byte, correctionSymbols int) string {
	// Convert from bytes to nybbles
	var src = flexArrayFixed(len(data) * 2)
	j := 0
	for i := 0; i < len(data); i++ {
		var upper = (data[i] >> 4) & 0x0F
		var lower = data[i] & 0x0F

		src.set(j, int(upper))
		j++
		src.set(j, int(lower))
		j++
	}

	var encoded = rsEncode(src, correctionSymbols)

	var output = display(encoded)

	encoded.release()
	src.release()

	return output
}

// Decode a multi-code string to binary data.
func Decode(code string, dataLength, correctionSymbols int) []byte {
	var expectedCodeLength = (dataLength * 2) + correctionSymbols
	var cleanInput = decodeDisplay(expectedCodeLength, code)

	// Input too short
	if cleanInput.length() < expectedCodeLength {
		cleanInput.release()
		return make([]byte, 0)
	}

	// Input too long
	if cleanInput.length() > expectedCodeLength {
		cleanInput.release()
		return make([]byte, 0)
	}

	var decoded = tryHardDecode(cleanInput, correctionSymbols, cleanInput.length())

	if decoded.length() > 0 {
		// remove error correction symbols
		for i := 0; i < correctionSymbols; i++ {
			decoded.pop()
		}

		// decoded data is nybbles, convert back to bytes
		final := make([]byte, decoded.length()/2)
		for i := 0; i < len(final); i++ {
			var upper = (decoded.popFirst()) << 4
			var lower = decoded.popFirst()
			final[i] = (byte)(upper + lower)
		}
		cleanInput.release()
		if decoded != cleanInput {
			decoded.release()
		}
		return final
	}

	cleanInput.release()
	if decoded != cleanInput {
		decoded.release()
	}
	return make([]byte, 0)
}

// _oddSet is characters for odd positions in the multi-code
var _oddSet = []rune{'0', '1', '2', '3', '6', '7', '8', '9', 'b', 'G', 'J', 'N', 'q', 'X', 'Y', 'Z', '~'}

// _evenSet is characters for even positions in the multi-code
var _evenSet = []rune{'4', '5', 'A', 'C', 'D', 'E', 'F', 'H', 'K', 'M', 'P', 'R', 's', 'T', 'V', 'W', '~'}

// isSpace looks up characters likely to be entered as spaces. These will be trimmed from input
func isSpace(c rune) bool {
	switch c {
	case ' ':
		return true
	case '-':
		return true
	case '.':
		return true
	case '_':
		return true
	case '+':
		return true
	case '*':
		return true
	case '#':
		return true
	}
	return false
}

// correction maps likely input mistakes to guessed corrections
func correction(inp rune) rune {
	switch inp {
	case 'O':
		return '0'
	case 'L':
		return '1'
	case 'I':
		return '1'
	case 'U':
		return 'V'
	default:
		return inp
	}
}

// caseChanges maps some code characters to lower case to improve letter/number distinction
func caseChanges(inp rune) rune {
	switch inp {
	case 'B':
		return 'b'
	case 'Q':
		return 'q'
	case 'S':
		return 's'
	default:
		return inp
	}
}

// indexOf finds index in char array, or -1 if not found
func indexOf(src []rune, target rune) int {
	for i := 0; i < len(src); i++ {
		if src[i] == target {
			return i
		}
	}

	return -1
}

// encodeDisplay returns the multi-code character for a symbol value and position in output
func encodeDisplay(symbol int, position int) rune {
	if symbol < 0 || symbol > 15 {
		return '~'
	}
	if (position & 1) == 0 {
		return _oddSet[symbol]
	}
	return _evenSet[symbol]
}

// display creates an output string for message data
func display(message *flexArray) string {
	var sb = strings.Builder{}
	for i := 0; i < message.length(); i++ {
		if i > 0 {
			if i%4 == 0 {
				sb.WriteRune('-')
			} else if i%2 == 0 {
				sb.WriteRune(' ')
			}
		}

		sb.WriteRune(encodeDisplay(message.get(i), i))
	}

	return sb.String()
}

// findFirstChiralityError finds the first position where chirality is incorrect
func findFirstChiralityError(chirality *flexArray) int {
	for position := 0; position < chirality.length(); position++ {
		var expected = position & 1
		if chirality.get(position) != expected {
			return position
		}
	}

	return -1
}

// repairCodesAndChirality tries to repair errors in the input code that can be detected with chirality
func repairCodesAndChirality(expectedCodeLength int, codes, chirality *flexArray) bool {
	const tryAgain = false
	const completed = true

	if codes.length() != chirality.length() {
		// ERROR in code/chirality code
		return completed // can't do much here
	}

	var currentLength = codes.length()
	var minLength = (2 * expectedCodeLength) / 3

	if currentLength < minLength {
		// Code is too short to recover accurately
		return completed
	}

	var firstErrPos = findFirstChiralityError(chirality)
	if currentLength == expectedCodeLength && firstErrPos < 0 {
		// Input codes seem correct
		return completed
	}

	// If input is shorter than expected, guess where a deletion occurred, and insert a zero-value.
	if currentLength < expectedCodeLength {
		// Insert a zero value at error position, and inject correct chirality
		if firstErrPos < 0 {
			// error is at the end
			var chi = currentLength & 1
			var endChi = expectedCodeLength & 1
			var diff = expectedCodeLength - currentLength
			if diff == 1 && chi == endChi {
				// don't add a wrong chi at the end if we're off-by-one
				codes.addStart(0)
				chirality.addStart(0)
			} else {
				codes.push(0)
				chirality.push(chi)
			}
		} else {
			var chi = firstErrPos & 1
			var chiNext = (firstErrPos + 1) & 1
			var chiAfter = (firstErrPos + 2) & 1

			// First, check if this is a transpose and not the first delete
			if firstErrPos < currentLength-3 && chirality.get(firstErrPos) != chi && chirality.get(firstErrPos+1) != chiNext && chirality.get(firstErrPos+2) == chiAfter {
				// swap these character
				codes.swap(firstErrPos, firstErrPos+1)
				chirality.swap(firstErrPos, firstErrPos+1)
				return tryAgain

			}

			// looks like a delete
			codes.insertAt(firstErrPos, 0)
			chirality.insertAt(firstErrPos, chi)
		}

		return tryAgain
	}

	// If input is longer than expected, guess where the problem is and delete
	if currentLength > expectedCodeLength {

		// First, if the last code is bad chirality, delete that before anything else
		var expectedLastChi = (1 + expectedCodeLength) & 1
		if chirality.get(currentLength-1) != expectedLastChi {
			codes.pop()
			chirality.pop()
			return tryAgain
		}

		// value and chirality at error position
		if firstErrPos < 0 {
			firstErrPos = currentLength - 1
		}
		codes.deleteAt(firstErrPos)
		chirality.deleteAt(firstErrPos)

		return tryAgain
	}

	// Input is correct length, but we have swapped characters.
	// Try swapping at first error, unless it is at the end.
	if firstErrPos >= expectedCodeLength-1 {
		return completed
	}

	if chirality.get(firstErrPos) == chirality.get(firstErrPos+1) {
		// A simple swap won't fix this. Either a totally wrong code, or repeated insertions and deletions.
		// For now, we will flip the chirality without changing anything so the checks can continue.

		chirality.set(firstErrPos, 1-chirality.get(firstErrPos))

		return tryAgain
	}

	// swapping characters might fix the problem
	codes.swap(firstErrPos, firstErrPos+1)
	chirality.swap(firstErrPos, firstErrPos+1)

	return tryAgain
}

// decodeDisplay tries to decode a string input into a symbol array
func decodeDisplay(expectedCodeLength int, input string) *flexArray {
	runeInput := []rune(input)
	var validCharCount = 0

	// Run filters first, to get the number of 'correct' characters.
	// We could extend this to store the location of unexpected chars to improve the next loop.
	for i := 0; i < len(runeInput); i++ {
		var src = unicode.ToUpper(runeInput[i])
		if isSpace(src) {
			continue
		} // skip spaces
		src = caseChanges(src) // Q->q, S->s, B->b
		src = correction(src)  // fix for anticipated transcription errors

		var oddIdx = indexOf(_oddSet, src)
		var evenIdx = indexOf(_evenSet, src)
		if oddIdx >= 0 || evenIdx >= 0 {
			validCharCount++
		}
	}

	// negative = too many chars. Positive = too few.
	var charCountMismatch = expectedCodeLength - validCharCount

	var codes = flexArrayEmptyWithStorage(validCharCount)
	var chirality = flexArrayEmptyWithStorage(validCharCount)

	var nextChir = 0
	for i := 0; i < len(runeInput); i++ {
		var src = unicode.ToUpper(runeInput[i])

		if isSpace(src) {
			continue
		} // skip spaces

		src = caseChanges(src) // Q->q, S->s, B->b
		src = correction(src)  // fix for anticipated transcription errors

		var oddIdx = indexOf(_oddSet, src)
		var evenIdx = indexOf(_evenSet, src)

		if oddIdx < 0 && evenIdx < 0 {
			// Broken character, maybe insert dummy.
			if charCountMismatch > 0 {
				codes.push(0)
				chirality.push(nextChir)
				nextChir = 1 - nextChir
				charCountMismatch--
			} else {
				charCountMismatch++
			}
		} else if oddIdx >= 0 && evenIdx >= 0 {
			// Should never happen!
			codes.release()
			chirality.release()
			return flexArrayFixed(0)
		} else if oddIdx >= 0 {
			codes.push(oddIdx)
			chirality.push(0)
			nextChir = 1
		} else {
			codes.push(evenIdx)
			chirality.push(1)
			nextChir = 0
		}
	}

	for tries := 0; tries < expectedCodeLength; tries++ {
		if repairCodesAndChirality(expectedCodeLength, codes, chirality) {
			chirality.release()
			return codes
		}
	}

	chirality.release()
	return codes
}

// tryHardDecode tries to find a reasonable solution for the message symbols
func tryHardDecode(msg *flexArray, sym, expectedLength int) *flexArray {
	var basicDecode = rsDecode(msg, sym, expectedLength)
	if basicDecode.length() > 0 {
		return basicDecode
	}

	// Normal decoding didn't work. Try rotations

	var end = msg.length()
	var half = end / 2
	i := 0
	for i = 0; i < half; i++ {
		// rotate left until we run out of zeros
		var r = msg.popFirst() // remove and return first element
		if r != 0 {
			msg.addStart(r)
			break
		}

		msg.push(r)

		basicDecode.release()
		basicDecode = rsDecode(msg, sym, expectedLength)
		if basicDecode.length() > 0 {
			return basicDecode
		}
	}

	// undo
	for i > 0 {
		i--
		var r = msg.pop()
		msg.addStart(r)
	}

	for i = 0; i < half; i++ {
		// rotate right until we run out of zeros
		var r = msg.pop()
		if r != 0 {
			msg.push(r)
			break
		}

		msg.addStart(r)

		basicDecode.release()
		basicDecode = rsDecode(msg, sym, expectedLength)
		if basicDecode.length() > 0 {
			return basicDecode
		}
	}

	return basicDecode
}
