package pkg

type flexArray struct {
	_length    int
	_offset    int
	_storeSize int
	_storage   []int
}

// flexArrayEmptyWithStorage creates an empty array with given storage space
func flexArrayEmptyWithStorage(space int) *flexArray {
	return newFlexArray(0, space)
}

// flexArrayBySize creates a zero-filled array of a given length
func flexArrayBySize(length int) *flexArray {
	return newFlexArray(length, length+16)
}

// flexArrayFixed creates a flexArray with no growth space
func flexArrayFixed(length int) *flexArray {
	return newFlexArray(length, length)
}

// flexArraySingleOne creates a new flexArray with a single `1` value stored
func flexArraySingleOne() *flexArray {
	output := flexArrayBySize(1)
	output.set(0, 1)
	return output
}

// flexArrayPair creates a new flexArray with two values stored
func flexArrayPair(a int, b int) *flexArray {
	var output = flexArrayFixed(2)
	output.set(0, a)
	output.set(1, b)
	return output
}

// clear removes all values and set length to zero. Does not remove storage
func (a *flexArray) clear() {
	a._offset = 0
	a._length = 0
}

// trimLeadingZero removes any leading zeroes. This can result in an empty array
func (a *flexArray) trimLeadingZero() {
	for a._length > 0 {
		if a._storage[a._offset] != 0 {
			return
		}
		a._offset++
		a._length--
	}
}

// allZero returns true if all values are zero
func (a *flexArray) allZero() bool {
	for i := 0; i < a._length; i++ {
		if a._storage[a._offset+i] != 0 {
			return false
		}
	}
	return true
}

// length returns length of data in the flexArray
func (a *flexArray) length() int {
	return a._length
}

// get returns the value at an index
func (a *flexArray) get(i int) int {
	return a._storage[i+a._offset]
}

// set changes the value at an index
func (a *flexArray) set(i int, value int) {
	a._storage[i+a._offset] = value
}

// release releases storage
func (a *flexArray) release() {
	// Not required in garbage collected environments
}

// push adds a new value to the end of the array
func (a *flexArray) push(v int) {
	// If space left, just add
	if a._offset+a._length < a._storeSize-1 {
		a._storage[a._offset+a._length] = v
		a._length++
		return
	}

	// See if we can use offset
	if a._offset > 0 {
		for i := 0; i < a._length; i++ {
			a._storage[i] = a._storage[i+a._offset]
		}
		a._storage[a._length] = v
		a._length++
		a._offset = 0
		return
	}

	// Out of space. Grow and add
	grow(a)
	a._storage[a._length] = v
	a._length++
}

// addStart adds a value at the start of the array, pushing other values forward
func (a *flexArray) addStart(v int) {
	// If we have offset, use that first
	if a._offset > 0 {
		a._offset--
		a._length++
		a._storage[a._offset] = v
		return
	}

	// If out of space, grow
	if a._offset+a._length >= a._storeSize {
		grow(a)
	}

	// Shift data forward
	for i := a._length - 1; i >= 0; i-- {
		a._storage[i+1] = a._storage[i]
	}

	// Write at front
	a._storage[0] = v
	a._length++
}

// trimEnd removes the given number of elements from the end of the array
func (a *flexArray) trimEnd(length int) {
	a._length -= length
	if a._length < 0 {
		a._length = 0
	}
}

// reverse does an in-place reversal of the array
func (a *flexArray) reverse() {
	if a._length < 2 {
		return
	}

	left := a._offset
	right := a._offset + a._length - 1

	for left < right {
		t := a._storage[left]
		a._storage[left] = a._storage[right]
		a._storage[right] = t

		left++
		right--
	}
}

// pop removes the last item, returning the removed value
func (a *flexArray) pop() int {
	if a._length <= 0 {
		return 0
	}

	a._length--
	return a._storage[a._offset+a._length]
}

// popFirst removes the first item, returning the removed value
func (a *flexArray) popFirst() int {
	if a._length <= 0 {
		return 0
	}

	r := a._storage[a._offset]
	a._offset++
	a._length--
	return r
}

// swap exchanges values at two indices
func (a *flexArray) swap(i1 int, i2 int) {
	t1 := a._storage[a._offset+i1]
	a._storage[a._offset+i1] = a._storage[a._offset+i2]
	a._storage[a._offset+i2] = t1
}

// insertAt adds a new values at the given index, shifting later values forward
func (a *flexArray) insertAt(index int, insertValue int) {
	// [a,b,c,d,e].InsertAfter(2, x);
	// --> [a,b,x,c,d,e]

	// If we can remove offset, do that
	if a._offset > 0 {

		for i := 0; i < index; i++ {
			a._storage[i+a._offset-1] = a._storage[i+a._offset]
		}
		a._offset--
		a._storage[a._offset+index] = insertValue
		a._length++
		return
	}

	// If we're out of space, grow
	if a._offset+a._length >= a._storeSize {
		grow(a)
	}

	// Shift values forward
	for i := a._length - 1; i >= index; i-- {
		a._storage[i+1] = a._storage[i]
	}

	a._storage[index] = insertValue
	a._length++
}

// deleteAt removes a value at the given index, shifting later values back
func (a *flexArray) deleteAt(index int) {
	// [a,b,c,d,e].deleteAt(2);
	// --> [a,b,  d,e]

	// If at start, move offset forward
	if index <= 0 {
		a._offset++
		a._length--
		return
	}

	// If at end, pull length back
	if index >= a._length-1 {
		a._length--
		return
	}

	// If nearer start, push forward and increase offset
	half := a._length / 2
	if index <= half {
		for i := index; i > 0; i-- {
			a._storage[a._offset+i] = a._storage[a._offset+i-1]
		}

		a._offset++
		a._length--
		return
	}

	// If nearer end, pull back
	for i := index; i < a._length-1; i++ {
		a._storage[a._offset+i] = a._storage[a._offset+i+1]
	}

	a._length--
}

// increase storage space by copying to a new array
func grow(a *flexArray) {
	oldSize := a._storeSize
	a._storeSize *= 2
	newStore := zeroArray(a._storeSize)
	for i := 0; i < oldSize; i++ {
		newStore[i] = a._storage[i]
	}
	a.release()
	a._storage = newStore
}

func zeroArray(length int) []int {
	return make([]int, length)
}

func newFlexArray(length int, storeSize int) *flexArray {
	halfSpare := (storeSize - length) / 2
	offset := 0
	if halfSpare > 0 {
		offset = halfSpare
	}
	result := flexArray{
		_length:    length,
		_offset:    offset,
		_storeSize: storeSize,
		_storage:   zeroArray(storeSize),
	}
	return &result
}
