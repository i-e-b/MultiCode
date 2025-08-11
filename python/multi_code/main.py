from multi_code import multi_code_encode, multi_code_decode

# Press the green button in the gutter to run the script.
if __name__ == '__main__':
    original = bytes('Hello, world!\0', 'utf_8')
    print('Original data:\r\n    ', end='')

    for i in range(0, 14):
        print(f"{original[i]&0xFF:02X} ", end='')

    code = multi_code_encode(original, 8)

    print(f'\r\nEncoded:\r\n    {code}')

    recovered = multi_code_decode(code, 14, 8)

    if len(recovered) < 1:
        print(f"\r\nFailed to recover data", end='')
    else:
        print(f"\r\nRecovered:\r\n    ", end='')
        for i in range(0, 14):
            print(f"{recovered[i] & 0xFF:02X} ", end='')
        result = recovered.decode("utf-8")
        print(f"\r\n    {result}")



# See PyCharm help at https://www.jetbrains.com/help/pycharm/
