using System;
using System.Text;

namespace MultiCode;

/// <summary>
/// A helper for cross-language variable-length integer arrays
/// </summary>
public class FlexArray
{
    private int[] _storage;   // allocated storage
    private int   _length;    // length of data (not storage)
    private int   _offset;    // offset of first item
    private int   _storeSize; // size of storage

    /// <summary>
    /// Debug: number of FlexArrays not released
    /// </summary>
    public static int Unreleased { get; set; } // don't port

    private FlexArray(int length, int storeSize)
    {
        _length = length;
        var halfSpare = (storeSize - length) / 2;
        _offset = halfSpare > 0 ? halfSpare : 0;
        _storeSize = storeSize;
        _storage = ZeroArray(_storeSize);
    }

    /// <summary>
    /// Create a zero-filled array of a given length
    /// </summary>
    public static FlexArray BySize(int length)
    {
        return new FlexArray(length, length + 16);
    }

    /// <summary>
    /// Create a new FlexArray with a single <c>1</c>
    /// value stored
    /// </summary>
    public static FlexArray SingleOne()
    {
        var outp = BySize(1);
        outp.Set(0, 1);
        return outp;
    }

    /// <summary>
    /// Create a new FlexArray with two values stored
    /// </summary>
    public static FlexArray Pair(int a, int b)
    {
        var outp = Fixed(2);
        outp.Set(0, a);
        outp.Set(1, b);
        return outp;
    }

    /// <summary>
    /// Create a FlexArray with no growth space
    /// </summary>
    public static FlexArray Fixed(int length)
    {
        return new FlexArray(length, length);
    }

    /// <summary>
    /// Remove all values and set length to zero.
    /// Does not remove storage.
    /// </summary>
    public void Clear()
    {
        _offset = 0;
        _length = 0;
    }

    /// <summary>
    /// Remove any leading zeroes.
    /// This can result in an empty array
    /// </summary>
    public void TrimLeadingZero()
    {
        while (_length > 0)
        {
            if (_storage[_offset] != 0) return;
            _offset++;
            _length--;
        }
    }

    /// <summary>
    /// Return <c>true</c> if all values are zero.
    /// Returns <c>true</c> for an empty array.
    /// </summary>
    public bool AllZero()
    {
        for (int i = 0; i < _length; i++)
        {
            if (_storage[_offset + i] != 0) return false;
        }

        return true;
    }

    /// <summary>
    /// Data length of array
    /// </summary>
    public int Length()
    {
        return _length;
    }

    /// <summary>
    /// Get from index
    /// </summary>
    public int Get(int i)
    {
        return _storage[i + _offset];
    }

    /// <summary>
    /// Set value at index
    /// </summary>
    public void Set(int i, int value)
    {
        _storage[i + _offset] = value;
    }

    /// <summary>
    /// Release flex array
    /// </summary>
    public void Release()
    {
        // Not required in garbage collected environments

        if (_storage.Length < 1) throw new Exception("Double release!");

        Unreleased--;
        _storage = Array.Empty<int>();
    }

    /// <summary>
    /// Push a new value to the end of this array
    /// </summary>
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

    /// <summary>
    /// Add a new value at the start of this array, pushing other values forward
    /// </summary>
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

    /// <summary>
    /// Remove a given number of elements from the end of the array
    /// </summary>
    public void TrimEnd(int len)
    {
        _length -= len;
        if (_length < 0) _length = 0;
    }

    /// <summary>
    /// In-place reverse of an array
    /// </summary>
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

    /// <summary>
    /// Remove last item, returning the removed value
    /// </summary>
    public int Pop()
    {
        if (_length <= 0) return 0;

        _length--;
        return _storage[_offset+_length];
    }

    /// <summary>
    /// Remove first item, returning the removed value
    /// </summary>
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

    /// <summary>
    /// Swap values at two indices
    /// </summary>
    public void Swap(int i1, int i2)
    {
        // ReSharper disable once SwapViaDeconstruction
        var t1 = _storage[_offset+i1];
        _storage[_offset+i1] = _storage[_offset+i2];
        _storage[_offset+i2] = t1;
    }

    /// <summary>
    /// Insert a new value at the given index, shifting later values forward
    /// </summary>
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

    /// <summary>
    /// Remove a value at the given index, shifting later values back
    /// </summary>
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


    /// <summary>
    /// Human readable string
    /// </summary>
    public override string ToString()
    {
        // This is for testing and debugging, it does not need to be ported
        var sb = new StringBuilder();

        sb.Append('[');
        for (int i = 0; i < _length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(_storage[_offset + i]);
        }
        sb.Append(']');

        return sb.ToString();
    }


    /// <summary>
    /// Increase storage space by copying to new array.
    /// </summary>
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

    /// <summary>
    /// Create a fixed size array filled with zeros.
    /// </summary>
    private int[] ZeroArray(int size)
    {
        // For environments with calloc, use that.
        // For raw data allocations, write zeros to at least 0.._length

        Unreleased++;
        return new int[size];
    }

    /// <summary>
    /// Create a duplicate of this array
    /// </summary>
    public FlexArray Copy()
    {
        var result = Fixed(_length);
        for (int i = 0; i < _length; i++)
        {
            result._storage[i + result._offset] = _storage[i + _offset];
        }

        return result;
    }

    /// <summary>
    /// Copy an int array into a new FlexArray
    /// </summary>
    public static FlexArray FromArray(int[] src) // Don't port -- this is for tests
    {
        var result = Fixed(src.Length);
        for (int i = 0; i < src.Length; i++)
        {
            result._storage[i] = src[i];
        }

        return result;
    }

    /// <summary>
    /// Copy to a new char array
    /// </summary>
    public char[] ToArray() // Don't port -- this is for tests
    {
        var result = new char[_length];
        for (int i = 0; i < _length; i++)
        {
            result[i] = (char)_storage[_offset+i];
        }

        return result;
    }
}