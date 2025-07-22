package com.ieb.multicode_demo.core;

/**
 * A helper for cross-language variable-length integer arrays
 */
class FlexArray {
    private int[] _storage;   // allocated storage
    private int   _length;    // length of data (not storage)
    private int   _offset;    // offset of first item
    private int   _storeSize; // size of storage

    private FlexArray(int length, int storeSize)
    {
        _length = length;
        var halfSpare = (storeSize - length) / 2;
        _offset = Math.max(halfSpare, 0);
        _storeSize = storeSize;
        _storage = ZeroArray(_storeSize);
    }

    /**
     * Create a zero-filled array of a given length
     * @param length number of zero-value entries in the result
     * @return new flex array
     */
    public static FlexArray BySize(int length)
    {
        return new FlexArray(length, length + 16);
    }

    /**
     * Create a new FlexArray with a single '1' value stored
     * @return new flex array
     */
    public static FlexArray SingleOne()
    {
        var outp = BySize(1);
        outp.Set(0, 1);
        return outp;
    }

    /**
     * Create a new FlexArray with two values stored
     * @param a value at index 0
     * @param b value at index 1
     * @return new flex array
     */
    public static FlexArray Pair(int a, int b)
    {
        var outp = Fixed(2);
        outp.Set(0, a);
        outp.Set(1, b);
        return outp;
    }

    /**
     * Create a FlexArray with no growth space
     * @param length number of zero-valued entries
     * @return new flex array
     */
    public static FlexArray Fixed(int length)
    {
        return new FlexArray(length, length);
    }

    /**
     * Remove all values and set length to zero.
     * Does not remove storage.
     */
    public void Clear()
    {
        _offset = 0;
        _length = 0;
    }

    /**
     * Remove any leading zeroes.
     * This can result in an empty array
     */
    public void TrimLeadingZero()
    {
        while (_length > 0)
        {
            if (_storage[_offset] != 0) return;
            _offset++;
            _length--;
        }
    }

    /**
     * Return <c>true</c> if all values are zero, or the array is empty
     */
    public boolean AllZero()
    {
        for (int i = 0; i < _length; i++)
        {
            if (_storage[_offset + i] != 0) return false;
        }

        return true;
    }

    /**
     * Data length of array
     */
    public int Length()
    {
        return _length;
    }

    /**
     * Get from index
     * @param i index in the array
     * @return value at index
     */
    public int Get(int i)
    {
        return _storage[i + _offset];
    }

    /**
     * Set value at index
     * @param i index in the array
     * @param value value to set
     */
    public void Set(int i, int value)
    {
        _storage[i + _offset] = value;
    }

    /**
     * Release flex array
     */
    public void Release()
    {
        // Not required in garbage collected environments
        _storage = null;
    }

    /**
     * Push a new value to the end of this array
     * @param v new value to add
     */
    public void Push(int v)
    {
        // If we have space left, just add
        if (_offset + _length < _storeSize - 1)
        {
            _storage[_offset + _length] = v;
            _length++;
            return;
        }

        // Run out of space. See if we can recover from offset?
        if (_offset > 0)
        {
            for (int i = 0; i < _length; i++)
            {
                _storage[i] = _storage[i + _offset];
            }
            _storage[_length] = v;
            _length++;

            _offset = 0;
            return;
        }

        // Still out of space. Let's size-up.
        Grow();

        // Do the add
        _storage[_length] = v;
        _length++;
    }

    /**
     * Add a new value at the start of this array, pushing other values forward
     * @param v new value to add
     */
    public void AddStart(int v)
    {
        if (_offset > 0)
        {
            _offset--;
            _length++;
            _storage[_offset] = v;
            return;
        }

        // If we're out of space, grow
        if (_offset + _length >= _storeSize) { Grow(); }

        // Shift everything forward (we could try shifting extra for optimisation)
        for (int i = _length - 1; i >= 0; i--)
        {
            _storage[i + 1] = _storage[i];
        }

        // Write at front
        _storage[0] = v;
        _length++;
    }

    /**
     * Remove a given number of elements from the end of the array
     * @param len number of entries to remove
     */
    public void TrimEnd(int len)
    {
        _length -= len;
        if (_length < 0) _length = 0;
    }

    /**
     * In-place reverse of an array
     */
    public void Reverse()
    {
        if (_length < 2) return;

        var left  = _offset;
        var right = _offset + _length - 1;
        while (left < right)
        {
            // ReSharper disable once SwapViaDeconstruction
            var t = _storage[left];
            _storage[left] = _storage[right];
            _storage[right] = t;
            left++;
            right--;
        }
    }

    /**
     * Remove last item, returning the removed value
     * @return value removed
     */
    public int Pop()
    {
        if (_length <= 0) return 0;

        _length--;
        return _storage[_offset+_length];
    }

    /**
     * Remove first item, returning the removed value
     * @return value removed
     */
    public int PopFirst()
    {
        if (_length > 0)
        {
            var r = _storage[_offset];
            _offset++;
            _length--;
            return r;
        }

        return 0;
    }

    /**
     * Swap values at two indices
     */
    public void Swap(int i1, int i2)
    {
        var t1 = _storage[_offset+i1];
        _storage[_offset+i1] = _storage[_offset+i2];
        _storage[_offset+i2] = t1;
    }

    /**
     * Insert a new value at the given index, shifting later values forward
     * @param index index for new value
     * @param insertValue value to insert
     */
    public void InsertAt(int index, int insertValue)
    {
        // [a,b,c,d,e].InsertAfter(2, x);
        // --> [a,b,x,c,d,e]

        // If we can remove offset, do that
        if (_offset > 0)
        {
            for (int i = 0; i < index; i++)
            {
                _storage[i + _offset - 1] = _storage[i + _offset];
            }
            _offset--;
            _storage[_offset + index] = insertValue;
            _length++;
            return;
        }

        // If we're out of space, grow
        if (_offset + _length >= _storeSize) { Grow(); }

        // Shift values forward
        for (int i = _length - 1; i >= index; i--)
        {
            _storage[i + 1] = _storage[i];
        }

        _storage[index] = insertValue;
        _length++;
    }

    /**
     * Remove a value at the given index, shifting later values back
     * @param index index of item to remove
     */
    public void DeleteAt(int index)
    {
        // [a,b,c,d,e].DeleteAt(2);
        // --> [a,b,  d,e]

        // If at start, move offset forward
        if (index <= 0)
        {
            _offset++;
            _length--;
            return;
        }

        // If at end, pull length back
        if (index >= _length - 1)
        {
            _length--;
            return;
        }

        // If nearer start, push forward and increase offset
        var half = _length / 2;
        if (index <= half)
        {
            for (int i = index; i > 0; i--)
            {
                _storage[_offset + i] = _storage[_offset + i - 1];
            }

            _offset++;
            _length--;
            return;
        }

        // If nearer end, pull back
        for (int i = index; i < _length - 1; i++)
        {
            _storage[_offset + i] = _storage[_offset + i + 1];
        }

        _length--;

    }

    /**
     * Increase storage space by copying to new array.
     */
    private void Grow()
    {
        _storeSize *= 2;
        var newStore = ZeroArray(_storeSize);
        for (int i = 0; i < _length; i++)
        {
            newStore[i] = _storage[i];
        }

        Release();
        _storage = newStore;
    }

    /**
     * Create a fixed size array filled with zeros.
     * @param size array size
     * @return new array
     */
    private int[] ZeroArray(int size)
    {
        // For environments with calloc, use that.
        // For raw data allocations, write zeros to at least 0.._length
        return new int[size];
    }

    /**
     * Create a duplicate of this array
     * @return new flex array
     */
    public FlexArray Copy()
    {
        var result = Fixed(_length);
        for (int i = 0; i < _length; i++)
        {
            result._storage[i + result._offset] = _storage[i + _offset];
        }

        return result;
    }
}
