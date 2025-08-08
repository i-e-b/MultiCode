package main

import (
	"fmt"
	"multicode/pkg"
)

func main() {
	original := "Hello, world!\x00"
	data := []byte(original)
	length := len(data)

	for i := 0; i < length; i++ {
		fmt.Printf("%.2x ", data[i]&0xFF)
	}

	// Encode and decode with no errors
	code := pkg.Encode(data, 8)

	fmt.Printf("\r\nEncoded: %s", code)

	recovered := pkg.Decode(code, length, 8)

	// Print out result
	if len(recovered) < 1 {
		fmt.Printf("\r\nFailed to recover data")
	} else {
		fmt.Printf("\r\nRecovered: ")
		for i := 0; i < length; i++ {
			fmt.Printf("%.2x ", recovered[i]&0xFF)
		}
		fmt.Printf("\r\n       -> %s", recovered)
	}

	// Damage the code to simulate transcription errors
	runes := []rune(code)
	t := runes[0] // transpose
	runes[0] = runes[1]
	runes[1] = t

	t = runes[18] // transpose
	runes[18] = runes[19]
	runes[19] = t

	runes[52] = ' ' // delete
	code = string(runes)

	// Check we can still recover
	fmt.Printf("\r\nDamaged: %s", code)

	recovered = pkg.Decode(code, length, 8)

	// Print out result
	if len(recovered) < 1 {
		fmt.Printf("\r\nFailed to recover data")
	} else {
		fmt.Printf("\r\nRecovered: ")
		for i := 0; i < length; i++ {
			fmt.Printf("%.2x ", recovered[i]&0xFF)
		}
		fmt.Printf("\r\n       -> %s", recovered)
	}
}
