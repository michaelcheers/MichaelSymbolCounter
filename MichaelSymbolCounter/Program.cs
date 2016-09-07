using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MichaelSymbolCounter
{
    class Program
    {
        struct LineSet
        {
            int a;
            int b;
            int c;
            int midBit;
            int bits;

            public LineSet(int b1, int b2, int b3)
            {
                int bit1 = (1 << b1);
                int bit2 = (1 << b2);
                int bit3 = (1 << b3);
                this.a = bit1 + bit2;
                this.b = bit1 + bit3;
                this.c = bit2 + bit3;
                this.midBit = bit2;
                this.bits = bit1+bit2+bit3;
            }

            public bool Match(int line, ref int other1, ref int other2, ref int bannedBit)
            {
                if ((bits & line) != line)
                    return false;

                if (a == line)
                {
                    other1 = b;
                    other2 = c;
                }
                else if (b == line)
                {
                    other1 = a;
                    other2 = c;
                    bannedBit = midBit;
                }
                else if (c == line)
                {
                    other1 = a;
                    other2 = b;
                }
                return true;
            }
        }

        static void Main(string[] args)
        {
            ChoiceState testState = new ChoiceState();

            Dictionary<int, int> bitIndex = new Dictionary<int, int>();
            int allBits = 0;
            for (int Idx = 0; Idx <= 9; Idx++)
            {
                bitIndex[1 << Idx] = Idx;
                allBits += 1 << Idx;
            }

            //            int firstTimeBits = allBits;
            // AB and E are the only interesting starting positions; everything else
            // is a reflection and/or rotation of those.
            int firstTimeBits = (1 << 1) + (1 << 2) + (1 << 5);
            ChoiceRange_Bits firstTimeRange = new ChoiceRange_Bits(firstTimeBits);

            // forbidden line combinations (because they continue the same line):
            // 123 -  456 -  789 -
            // 147 |  258 |  369 |
            // 159 \  357 /
            List<LineSet> forbiddenLines = new List<LineSet>
            {
                new LineSet(1,2,3), new LineSet(4,5,6), new LineSet(7,8,9),
                new LineSet(1,4,7), new LineSet(2,5,8), new LineSet(3,6,9),
                new LineSet(1,5,9), new LineSet(3,5,7)
            };

            int count = 0;
            int[] counts = new int[10];

            do
            {
                int remainingBits = allBits;
                List<int> shape = new List<int>(9);
                HashSet<int> banned = new HashSet<int>();

                while (true)
                {
                    int newOutcome;
                    if (shape.Count == 0)
                        newOutcome = testState.NextChoice(firstTimeRange);
                    else
                        newOutcome = testState.NextChoice(new ChoiceRange_Bits(remainingBits));
                    if(newOutcome == 1)
                    {
                        // bit 1 means stop and report the shape
                        
                        string report = "shape ";
                        foreach (int x in shape)
                        {
                            report += (char)('A'+bitIndex[x]-1);
                        }
                        Console.WriteLine(report);
                        count++;
                        if (shape.Count > 0)
                        {
                            counts[bitIndex[shape[0]]]++;
                        }
                        break;
                    }

                    if (shape.Count > 0)
                    {
                        int lastPair = newOutcome + shape.Last();
                        if (banned.Contains(lastPair))
                        {
                            // if we just made a line that's been banned, forget it and start a new sequence.
                            break;
                        }
                        else
                        {
                            foreach (LineSet linePair in forbiddenLines)
                            {
                                int bannedPair1 = 0;
                                int bannedPair2 = 0;
                                int bannedBit = 0;
                                if (linePair.Match(lastPair, ref bannedPair1, ref bannedPair2, ref bannedBit))
                                {
                                    banned.Add(bannedPair1);
                                    banned.Add(bannedPair2);
                                    remainingBits &= ~bannedBit;
                                    break;
                                }
                            }
                        }
                    }

                    shape.Add(newOutcome);
                    remainingBits &= ~newOutcome; // go around again, but we can't pick this position again
                }
            }
            while (testState.NextSequence());

            Console.WriteLine(counts[1]*2 + " valid shapes start at corners");
            Console.WriteLine(counts[2]*2 + " valid shapes start at sides");
            Console.WriteLine(counts[5]/2 + " valid shapes start in the middle");
            Console.WriteLine("Final count: " + (counts[1]*2 + counts[2]*2 + counts[5]/2) + " distinguishable shapes");

            Console.Read();
        }
    }

    class ChoiceState
    {
        struct ChoiceTracker
        {
            public int index;
            public ChoiceRange range;

            public ChoiceTracker(ChoiceRange range)
            {
                this.range = range;
                this.index = 0;
            }

            public ChoiceTracker(int bits)
            {
                this.range = new ChoiceRange_Bits(bits);
                this.index = 0;
            }

            public int value { get { return range.get(index); } }
        }

        List<ChoiceTracker> choices = new List<ChoiceTracker>();
        int nextChoiceIdx;
        public bool done { get; private set; }

        public int NextChoice(ChoiceRange range)
        {
            Debug.Assert(!done);
            ChoiceTracker c;
            if (nextChoiceIdx >= choices.Count)
            {
                c = new ChoiceTracker(range);
                choices.Add(c);
            }
            else
            {
                c = choices[nextChoiceIdx];
                //Debug.Assert(c.min == min && c.max == max);
            }

            nextChoiceIdx++;
            return c.value;
        }

        public bool NextSequence()
        {
            Debug.Assert(choices.Count == 0 || nextChoiceIdx == choices.Count);
            if (choices.Count > 0)
            {
                while (true)
                {
                    ChoiceTracker foundChoice = choices.Last();
                    if (foundChoice.index == foundChoice.range.maxIndex)
                    {
                        choices.RemoveAt(choices.Count - 1);
                        if (choices.Count == 0)
                            return false;
                    }
                    else
                    {
                        // found a thing we can increment, we're done
                        foundChoice.index++;
                        choices[choices.Count - 1] = foundChoice;
                        break;
                    }
                }
            }

            nextChoiceIdx = 0;
            return true;
        }

        void Restart()
        {
            done = false;
        }
    }

    public interface ChoiceRange
    {
        int get(int value);
        int maxIndex { get; }
    }

    class ChoiceRange_MinMax : ChoiceRange
    {
        int minValue;
        int _maxIndex;

        public int maxIndex { get { return _maxIndex; } }

        public ChoiceRange_MinMax(int min, int max)
        {
            this.minValue = min;
            this._maxIndex = max - min;
        }

        public int get(int index)
        {
            return index + minValue;
        }
    }

    class ChoiceRange_Bits : ChoiceRange
    {
        List<int> bitList;

        public int maxIndex { get { return bitList.Count - 1; } }

        public ChoiceRange_Bits(int bits)
        {
            bitList = new List<int>();
            int factor = 1;
            while (bits > 0)
            {
                if ((bits & 1) == 1)
                {
                    bitList.Add(factor);
                }
                bits >>= 1;
                factor <<= 1;
            }
        }

        public int get(int index)
        {
            return bitList[index];
        }
    }
}
