using MultiCode;
using NUnit.Framework;

namespace MultiCodeTests;

[TestFixture]
public class FlexArrayTests
{

    [Test]
    public void can_create_and_release_an_empty_array()
    {
        var subject = FlexArray.BySize(0);

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.Zero);
    }

    [Test]
    public void default_values_are_zero()
    {
        var subject = FlexArray.BySize(3);

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.EqualTo(3));
        Assert.That(subject.Get(0), Is.EqualTo(0));
        Assert.That(subject.Get(1), Is.EqualTo(0));
        Assert.That(subject.Get(2), Is.EqualTo(0));
    }

    [Test]
    public void can_set_value_at_index()
    {
        var subject = FlexArray.BySize(3);

        subject.Set(2, 3);
        subject.Set(0, 1);
        subject.Set(1, 2);

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.EqualTo(3));
        Assert.That(subject.Get(0), Is.EqualTo(1));
        Assert.That(subject.Get(1), Is.EqualTo(2));
        Assert.That(subject.Get(2), Is.EqualTo(3));
    }

    [Test]
    public void can_add_to_an_empty_array()
    {
        var subject = FlexArray.BySize(0);

        subject.Push(1);
        subject.Push(2);
        subject.Push(3);
        subject.Push(4);

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.EqualTo(4));
        Assert.That(subject.Get(0), Is.EqualTo(1));
        Assert.That(subject.Get(1), Is.EqualTo(2));
        Assert.That(subject.Get(2), Is.EqualTo(3));
        Assert.That(subject.Get(3), Is.EqualTo(4));
    }

    [Test]
    public void can_grow_arrays_past_initial_bounds()
    {
        var subject = FlexArray.Fixed(3);

        subject.Push(1);
        subject.Push(2);
        subject.Push(3);
        subject.Push(4);
        subject.Push(5);

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.EqualTo(8));
        Assert.That(subject.Get(0), Is.EqualTo(0));
        Assert.That(subject.Get(1), Is.EqualTo(0));
        Assert.That(subject.Get(2), Is.EqualTo(0));
        Assert.That(subject.Get(3), Is.EqualTo(1));
        Assert.That(subject.Get(4), Is.EqualTo(2));
        Assert.That(subject.Get(5), Is.EqualTo(3));
        Assert.That(subject.Get(6), Is.EqualTo(4));
        Assert.That(subject.Get(7), Is.EqualTo(5));
    }

    [Test]
    public void push_pop_and_unshift()
    {
        var subject = FlexArray.Pair(1, 2);

        subject.AddStart(0);
        var popped = subject.Pop();
        subject.Push(3);

        Console.WriteLine(subject.ToString());

        Assert.That(popped, Is.EqualTo(2));
        Assert.That(subject.Length(), Is.EqualTo(3));
        Assert.That(subject.Get(0), Is.EqualTo(0));
        Assert.That(subject.Get(1), Is.EqualTo(1));
        Assert.That(subject.Get(2), Is.EqualTo(3));

        subject.AddStart(8);
        popped = subject.PopFirst();
        Assert.That(popped, Is.EqualTo(8));
        Assert.That(subject.Length(), Is.EqualTo(3));
        Assert.That(subject.Get(0), Is.EqualTo(0));
        Assert.That(subject.Get(1), Is.EqualTo(1));
        Assert.That(subject.Get(2), Is.EqualTo(3));
    }

    [Test]
    public void all_zero()
    {
        var subject = FlexArray.Pair(0, 1);

        Assert.That(subject.AllZero(), Is.False);

        subject.Pop();
        Assert.That(subject.AllZero(), Is.True);

        subject.Pop();
        Assert.That(subject.Length(), Is.Zero);
        Assert.That(subject.AllZero(), Is.True);

        subject.Push(0);
        subject.Push(0);
        subject.Push(0);
        Assert.That(subject.AllZero(), Is.True);

        subject.Push(-1);
        Assert.That(subject.AllZero(), Is.False);
    }

    [Test]
    public void clear_array()
    {
        var subject = FlexArray.BySize(5);

        Assert.That(subject.Length(), Is.EqualTo(5));

        subject.Clear();
        Assert.That(subject.Length(), Is.Zero);

        subject.Push(1);
        Assert.That(subject.Get(0), Is.EqualTo(1));
        Assert.That(subject.Length(), Is.EqualTo(1));
    }

    [Test]
    public void trim_leading_zero()
    {
        var subject = FlexArray.Pair(0, 1);

        Assert.That(subject.Length(), Is.EqualTo(2));

        subject.TrimLeadingZero();
        Assert.That(subject.Get(0), Is.EqualTo(1));
        Assert.That(subject.Length(), Is.EqualTo(1));

        subject.TrimLeadingZero();
        Assert.That(subject.Get(0), Is.EqualTo(1));
        Assert.That(subject.Length(), Is.EqualTo(1));

        subject.Push(0);
        subject.Push(0);
        subject.Push(0);
        subject.TrimLeadingZero();
        Assert.That(subject.Length(), Is.EqualTo(4));
        Assert.That(subject.Get(0), Is.EqualTo(1));
        Assert.That(subject.Get(1), Is.EqualTo(0));
    }

    [Test]
    public void add_values_to_start()
    {
        var subject = FlexArray.Fixed(3);

        subject.AddStart(3);
        subject.AddStart(2);
        subject.AddStart(1);
        subject.Pop();
        subject.Pop();
        subject.Pop();

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.EqualTo(3));
        Assert.That(subject.Get(0), Is.EqualTo(1));
        Assert.That(subject.Get(1), Is.EqualTo(2));
        Assert.That(subject.Get(2), Is.EqualTo(3));
    }

    [Test]
    public void trim_end_values()
    {
        var subject = FlexArray.Fixed(3);

        subject.AddStart(3);
        subject.AddStart(2);
        subject.AddStart(1);
        subject.TrimEnd(3);

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.EqualTo(3));
        Assert.That(subject.Get(0), Is.EqualTo(1));
        Assert.That(subject.Get(1), Is.EqualTo(2));
        Assert.That(subject.Get(2), Is.EqualTo(3));
    }

    [Test]
    public void reverse_in_place()
    {
        var subject = FlexArray.BySize(3);

        subject.Set(0,3);
        subject.Set(1,2);
        subject.Set(2,1);

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.EqualTo(3));
        Assert.That(subject.Get(0), Is.EqualTo(3));
        Assert.That(subject.Get(1), Is.EqualTo(2));
        Assert.That(subject.Get(2), Is.EqualTo(1));

        subject.Reverse();

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.EqualTo(3));
        Assert.That(subject.Get(0), Is.EqualTo(1));
        Assert.That(subject.Get(1), Is.EqualTo(2));
        Assert.That(subject.Get(2), Is.EqualTo(3));

        subject.Push(4);
        subject.Reverse();

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.EqualTo(4));
        Assert.That(subject.Get(0), Is.EqualTo(4));
        Assert.That(subject.Get(1), Is.EqualTo(3));
        Assert.That(subject.Get(2), Is.EqualTo(2));
        Assert.That(subject.Get(3), Is.EqualTo(1));

        subject.Clear();
        subject.Reverse(); // make sure it behaves with zero-length arrays
        Assert.That(subject.Length(), Is.Zero);
    }

    [Test]
    public void swap_in_place()
    {
        var subject = FlexArray.BySize(4);

        subject.Set(0,3);
        subject.Set(1,2);
        subject.Set(2,1);
        subject.Set(3,4);

        Console.WriteLine(subject.ToString());

        subject.Swap(1,2);
        subject.Swap(0,2);

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.EqualTo(4));
        Assert.That(subject.Get(0), Is.EqualTo(2));
        Assert.That(subject.Get(1), Is.EqualTo(1));
        Assert.That(subject.Get(2), Is.EqualTo(3));
        Assert.That(subject.Get(3), Is.EqualTo(4));
    }

    [Test]
    public void delete_at_index()
    {
        var subject = FlexArray.BySize(10);

        for (int i = 0; i < 10; i++)
        {
            subject.Set(i,i+1);
        }

        Console.WriteLine(subject.ToString());

        subject.DeleteAt(2); // del 3
        subject.DeleteAt(7); // del 9

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.EqualTo(8));
        Assert.That(subject.Get(0), Is.EqualTo(1));
        Assert.That(subject.Get(1), Is.EqualTo(2));
        Assert.That(subject.Get(2), Is.EqualTo(4));
        Assert.That(subject.Get(3), Is.EqualTo(5));
        Assert.That(subject.Get(4), Is.EqualTo(6));
        Assert.That(subject.Get(5), Is.EqualTo(7));
        Assert.That(subject.Get(6), Is.EqualTo(8));
        Assert.That(subject.Get(7), Is.EqualTo(10));

        subject.DeleteAt(0); // special case
        subject.DeleteAt(6); // special case

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.EqualTo(6));
        Assert.That(subject.Get(0), Is.EqualTo(2));
        Assert.That(subject.Get(1), Is.EqualTo(4));
        Assert.That(subject.Get(2), Is.EqualTo(5));
        Assert.That(subject.Get(3), Is.EqualTo(6));
        Assert.That(subject.Get(4), Is.EqualTo(7));
        Assert.That(subject.Get(5), Is.EqualTo(8));
    }

    [Test]
    public void insert_at_index()
    {
        var subject = FlexArray.BySize(5);

        for (int i = 0; i < 5; i++)
        {
            subject.Set(i,i+1);
        }

        Console.WriteLine(subject.ToString());

        //                          [1, 2, 3, 4, 5]
        subject.InsertAt(2, -3); // [1, 2, -3, 3, 4, 5]
        subject.InsertAt(5, -5); // [1, 2, -3, 3, 4, -5, 5]
        subject.InsertAt(0, -1); // [-1, 1, 2, -3, 3, 4, -5, 5]
        subject.InsertAt(8, 6);  // [-1, 1, 2, -3, 3, 4, -5, 5, 6]

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.EqualTo(9));
        Assert.That(subject.Get(0), Is.EqualTo(-1));
        Assert.That(subject.Get(1), Is.EqualTo(1));
        Assert.That(subject.Get(2), Is.EqualTo(2));
        Assert.That(subject.Get(3), Is.EqualTo(-3));
        Assert.That(subject.Get(4), Is.EqualTo(3));
        Assert.That(subject.Get(5), Is.EqualTo(4));
        Assert.That(subject.Get(6), Is.EqualTo(-5));
        Assert.That(subject.Get(7), Is.EqualTo(5));
        Assert.That(subject.Get(8), Is.EqualTo(6));
    }

    [Test]
    public void insert_at_index_with_grow()
    {
        var subject = FlexArray.Fixed(5);

        for (int i = 0; i < 5; i++)
        {
            subject.Set(i,i+1);
        }

        Console.WriteLine(subject.ToString());

        //                          [1, 2, 3, 4, 5]
        subject.InsertAt(2, -3); // [1, 2, -3, 3, 4, 5]
        subject.InsertAt(5, -5); // [1, 2, -3, 3, 4, -5, 5]
        subject.InsertAt(0, -1); // [-1, 1, 2, -3, 3, 4, -5, 5]
        subject.InsertAt(8, 6);  // [-1, 1, 2, -3, 3, 4, -5, 5, 6]

        Console.WriteLine(subject.ToString());

        Assert.That(subject.Length(), Is.EqualTo(9));
        Assert.That(subject.Get(0), Is.EqualTo(-1));
        Assert.That(subject.Get(1), Is.EqualTo(1));
        Assert.That(subject.Get(2), Is.EqualTo(2));
        Assert.That(subject.Get(3), Is.EqualTo(-3));
        Assert.That(subject.Get(4), Is.EqualTo(3));
        Assert.That(subject.Get(5), Is.EqualTo(4));
        Assert.That(subject.Get(6), Is.EqualTo(-5));
        Assert.That(subject.Get(7), Is.EqualTo(5));
        Assert.That(subject.Get(8), Is.EqualTo(6));
    }
}