using System.Text;
using MultiCode;
using NUnit.Framework;

namespace MultiCodeTests;

[TestFixture]
public class MultiCodeTests
{

    [Test]
    public void stable_output_for_input()
    {
        var original = "Hello, world!\0";
        var data     = Encoding.UTF8.GetBytes(original);
        var length   = data.Length;

        Console.WriteLine(Convert.ToHexString(data));

        var result   = MultiCoder.Encode(data, 8);

        Console.WriteLine(result);

        var recovered = MultiCoder.Decode(result, length, 8);

        Assert.That(recovered, Is.EqualTo(data).AsCollection);
    }

    [Test]
    [TestCase("BC7DE6FD","Ns 9T-YF ZT-14 JP-Js")]
    [TestCase("DA6C1DF2","XP 8s-1T ZA-1R 0W-XD")]
    [TestCase("E137E76B","Y5 3H-YH 8R-Gs 6W-Xs")]
    [TestCase("BE006D89","NV 04-8T bM-qD 1A-YP")]
    [TestCase("00000000","04 04-04 04-04 04-04")]
    [TestCase("FFFFFFFF","ZW ZW-ZW ZW-8s YE-JR")]
    public void generate_stable_outputs_from_input(string input, string expected)
    {
        FlexArray.Unreleased = 0;

        var data   = Convert.FromHexString(input);
        var addSym = 6;

        var result   = MultiCoder.Encode(data, addSym);

        Console.WriteLine(result);

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(FlexArray.Unreleased, Is.Zero, "Not all flex arrays released!");
    }

    [Test]
    [TestCase("BC7DE6FD","Ns 9T-YF ZT-14 JP-Js")]
    [TestCase("DA6C1DF2","XP 8s-1T ZA-1R 0W-XD")]
    [TestCase("E137E76B","Y5 3H-YH 8R-Gs 6W-Xs")]
    [TestCase("BE006D89","NV 04-8T bM-qD 1A-YP")]
    [TestCase("00000000","04 04-04 04-04 04-04")]
    [TestCase("FFffFffF","ZW ZW-ZW ZW-8s YE-JR")]
    public void decode_inputs_with_no_errors(string expected, string input)
    {
        FlexArray.Unreleased = 0;

        var addSym       = 6;
        var expectedData = Convert.FromHexString(expected);
        var output       = MultiCoder.Decode(input, expectedData.Length, addSym);

        var result = Convert.ToHexString(output);

        Console.WriteLine(result);

        Assert.That(output, Is.EqualTo(expectedData).AsCollection);
        Assert.That(FlexArray.Unreleased, Is.Zero, "Not all flex arrays released!");
    }

    [Test]
    [TestCase("BC7DE6FD","nS9tyfzt14jpjS")]
    [TestCase("DA6C1DF2","xp8S1tzA1R0WXD")]
    [TestCase("E137E76B","Y53HYH8rgS6wxs")]
    [TestCase("BE006D89","  n v 0. 4 8 t b m q d. 1 a - y p")]
    [TestCase("BE006D89"," N-- V0...4 8 T B M Q.D..1 A -Y P")]
    [TestCase("00000000","04 04-04 04-04 04-04")]
    [TestCase("FFffFffF","ZW ZW-ZW ZW-8s YE-JR")]
    public void decode_inputs_with_mixed_case_and_spaces(string expected, string input)
    {
        FlexArray.Unreleased = 0;

        var addSym       = 6;
        var expectedData = Convert.FromHexString(expected);
        var output       = MultiCoder.Decode(input, expectedData.Length, addSym);

        var result = Convert.ToHexString(output);

        Console.WriteLine(result);

        Assert.That(output, Is.EqualTo(expectedData).AsCollection);
        Assert.That(FlexArray.Unreleased, Is.Zero, "Not all flex arrays released!");
    }

    [Test]
    [TestCase("BC7DE6FD","Ns T9-YF ZT-14 JP-Js")] // one transpose
    [TestCase("DA6C1DF2","XP 8s-1T ZA-1R 0W-X")] // delete 1 at end
    [TestCase("E137E76B","5 3H-YH 8R-Gs 6W-Xs")] // delete 1 at start
    [TestCase("BE006D89","VN 40-T8 Mb-Dq A1-PY")] // all transpose

    [TestCase("BC7DE6FD","9T-YF ZT-14 JP-Js")] // delete 2 at start
    [TestCase("DA6C1DF2","XP 8s-1T ZA-1R 0W")] // delete 2 at end
    [TestCase("E137E76B","Y5 3H-YHh 8Rr-Gs 6W-Xs")] // duplications
    [TestCase("BE006D89","NV 04-8T Mmb-qD 1A-YP")] // duplication and transpose
    public void decode_inputs_with_simple_errors(string expected, string input)
    {
        FlexArray.Unreleased = 0;

        var addSym       = 6;
        var expectedData = Convert.FromHexString(expected);
        var output       = MultiCoder.Decode(input, expectedData.Length, addSym);

        var result = Convert.ToHexString(output);

        Console.WriteLine(result);

        Assert.That(output, Is.EqualTo(expectedData).AsCollection);
        Assert.That(FlexArray.Unreleased, Is.Zero, "Not all flex arrays released!");
    }

    [Test, Explicit("Stresses recovery until an invalid solution is found"), Repeat(1000, StopOnFailure = false)]
    public void fuzz_can_survive_transpositions_and_errors()
    {
        Console.WriteLine();
        FlexArray.Unreleased = 0;

        var original = new byte[Random.Shared.Next(4,32)];
        Random.Shared.NextBytes(original);

        var correctionSyms = original.Length / 2;

        var correctCode = MultiCoder.Encode(original, correctionSyms);
        var damagedCode = FlexArray.FromArray(correctCode.ToCharArray().Where(c=> c != '-' && c != ' ').Select(c=>(int)c).ToArray());


        Console.WriteLine($"Data = {Convert.ToHexString(original)};");
        Console.WriteLine($"Correction Symbol Count = {correctionSyms};");
        Console.WriteLine($"Output code = {correctCode};");
        Console.WriteLine($" Input code = {new string(damagedCode.ToArray())};");

        // Check that normal decode works
        var cleanOutput = MultiCoder.Decode(new string(damagedCode.ToArray()), original.Length, correctionSyms);
        Assert.That(cleanOutput, Is.EqualTo(original).AsCollection, "Clean data was not decoded!");

        // Introduce errors
        for (int i = 0; i < correctionSyms / 2; i++)
        {
            var type = Random.Shared.Next(0, 4);
            switch (type)
            {
                case 0: // transpose
                {
                    var j = Random.Shared.Next(1, damagedCode.Length());
                    damagedCode.Swap(j,j-1);
                    break;
                }

                case 1: // delete
                {
                    var j = Random.Shared.Next(1, damagedCode.Length());
                    damagedCode.DeleteAt(j);
                    break;
                }

                case 2: // duplicate
                {
                    var j = Random.Shared.Next(1, damagedCode.Length());
                    damagedCode.InsertAt(j, damagedCode.Get(j));
                    break;
                }

                case 3: // error
                {
                    var j = Random.Shared.Next(1, damagedCode.Length());
                    var k = Random.Shared.Next(-2, 3);
                    damagedCode.Set(j, damagedCode.Get(j) + k);
                    break;
                }
            }

            // Check that decode can recover

            var damagedInput = new string(damagedCode.ToArray());
            var output       = MultiCoder.Decode(damagedInput, original.Length, correctionSyms);

            if (output.Length < 1)
            {
                Console.WriteLine($"Rejected      {damagedInput}");
                return;
            }

            Console.WriteLine($"        -->   {damagedInput} --> {Convert.ToHexString(output)}");

            Assert.That(output, Is.EqualTo(original).AsCollection, "Invalid solution found");
        }

        damagedCode.Release();

        Assert.That(FlexArray.Unreleased, Is.Zero, "Not all flex arrays released!");
    }
}