# MultiCode

Data encoding for human input

A combination of Reed-Solomon forward-error-correction codes,
and a specific binary-to-text encoding that allows common human-input errors
to be detected and possibly corrected.

This results in a highly resilient code which is very likely to work.

## Design

Before passing to a FEC (in this case, Reed-Solomon), we look for 
patterns in the input, and try to correct for them, increasing the chance
of the FEC successfully correcting the input.


Start with 32 characters, from the ASCII alpha-numeric set with indistict glyphs <code>OLIU</code> removed,
then split into an 'odd' and 'even' set, resulting in 16 characters in each set (for 4 bit grouping)
```text
 0 1 2 3 6 7 8 9 b G J N q X Y Z
4 5 A C D E F H K M P R s T V W
```

`S`, `Q`, and `B` are presented as lower case
to prevent confusion with `5`, `0`, and `8`.
As no pair of even or odd characters will be next to each other, we can optimise population of these
sets to reduce the chance of accidental obscenity. The likelyhood of accidental word forming is already
quite low with this set.

Generated codes should be alternating between the two sets.
We know if an input has mistakes if it is not following this alternation.
This has a short-coming that we can't tell the difference between pairs of deleted characters
at the start or end of the input. We try rotating the input during the Reed-Solomon step,
to the limit of deleted characters.

We could try having fixed guard codes at the start and end, but this is not implemented in this project.

## Error Examples

<table>
  <tr>
    <td></td>
    <td><code></code></td>
    <td></td>
  </tr>
  <tr>
    <td></td>
    <td><code>oeo-eoe-oeo</code></td>
    <td>-- odd-even pattern is correct, length is correct</td>
  </tr>
  <tr>
    <td>Real input:</td>
    <td><code>7MQ-6DJ-S01</code></td>
    <td></td>
  </tr>
  <tr><td>&nbsp;</td></tr>
  <tr>
    <td></td>
    <td><code><span style="color:red;">_eo-eoe-oeo</span></code></td>
    <td>-- pattern is inverted. First char missing, put in placeholder for Reed-Solomon</td>
  </tr>
  <tr>
    <td>Deleted first char:</td>
    <td><code><span style="color:red;">_</span>MQ-6DJ-S01</code></td>
    <td></td>
  </tr>
  <tr><td>&nbsp;</td></tr>
  <tr>
    <td></td>
    <td><code>oe<span style="color:red;">o-_o</span>e-oeo</code></td>
    <td>-- "ee" or "oo" around deletion point</td>
  </tr>
  <tr>
    <td>Deleted middle char:</td>
    <td><code>7MQ-<span style="color:red;">_</span>DJ-S01</code></td>
    <td></td>
  </tr>
  <tr><td>&nbsp;</td></tr>
  <tr>
    <td></td>
    <td><code>oeo-eoe-oe<span style="color:red;">_</span></code></td>
    <td>-- all correct, but wrong length</td>
  </tr>
  <tr>
    <td>Deleted end char:</td>
    <td><code>7MQ-6DJ-S0<span style="color:red;">_</span></code></td>
    <td></td>
  </tr>
  <tr><td>&nbsp;</td></tr>
  <tr>
    <td></td>
    <td><code>oeo-<span style="color:red;">eeo-o</span>eo</code></td>
    <td>-- "eeoo" one before transposition</td>
  </tr>
  <tr>
    <td>Transposed char:</td>
    <td><code>7MQ-6<span style="color:red;">JD</span>-S01</code></td>
    <td></td>
  </tr>
  <tr>
    <td></td>
    <td><code>oe<span style="color:red;">o-oee</span>-oeo</code></td>
    <td>-- "ooee" one before transposition</td>
  </tr>
  <tr>
    <td></td>
    <td><code>7MQ-<span style="color:red;">D6</span>J-S01</code></td>
    <td></td>
  </tr>
  <tr><td>&nbsp;</td></tr>
  <tr>
    <td></td>
    <td><code>oeo-<span style="color:red;">eeo-eoo</span></code></td>
    <td>-- "ee" at start, "oo" at end</td>
  </tr>
  <tr>
    <td>Double transposed:</td>
    <td><code>7MQ-6<span style="color:red;">JD-0S</span>1</code></td>
    <td></td>
  </tr>
  <tr>
    <td></td>
    <td><code>oe<span style="color:red;">o-oeo-ee</span>o</code></td>
    <td>-- "oo" at start, "ee" at end</td>
  </tr>
  <tr>
    <td></td>
    <td><code>7MQ-<span style="color:red;">D6S-J</span>01</code></td>
    <td></td>
  </tr>
  <tr><td>&nbsp;</td></tr>
  <tr>
    <td></td>
    <td><code>oeo-eo<span style="color:red;">o</span>e-oeo</code></td>
    <td>-- too long. Error at first repeated o/e</td>
  </tr>
  <tr>
    <td>Insertion:</td>
    <td><code>7MQ-6D<span style="color:red;">D</span>J-S01</code></td>
    <td></td>
  </tr>
</table>

## The wierd implementation

The various implementations are not generally idiomatic for their language. They have been written to be portable with minimal effort -- so they basically only rely on being able to create arrays, and the rest comes packaged.

## JsFiddle

The prototype of this is at https://jsfiddle.net/i_e_b/x1vru8bc/  where you can play around with it.
