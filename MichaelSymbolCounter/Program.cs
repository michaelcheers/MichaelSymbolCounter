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
        static void Main(string[] args)
        {
            ChoiceState testState = new ChoiceState();

            while(true)
            {
                string report = "choice";
                testState.BeginSequence();
                if (testState.done)
                    break;

                int newOutcome = 10;
                while (newOutcome > 0)
                {
                    newOutcome = testState.NextChoice(-1, newOutcome - 1);
                    if (newOutcome >= 0)
                    {
                        report += " " + newOutcome;
                    }
                }
                Console.WriteLine(report);
            }
        }
    }

    class ChoiceState
    {
        struct Choice
        {
            public int min;
            public int max;
            public int cur;

            public Choice(int min, int max)
            {
                this.min = min;
                this.max = max;
                this.cur = min;
            }
        }

        List<Choice> choices = new List<Choice>();
        int nextChoiceIdx;
        public bool done { get; private set; }

        public int NextChoice(int min, int max)
        {
            Debug.Assert(!done);
            if (nextChoiceIdx >= choices.Count)
            {
                Choice c = new Choice(min, max);
                choices.Add(c);
                nextChoiceIdx++;
                return c.cur;
            }
            else
            {
                Choice c = choices[nextChoiceIdx];
                Debug.Assert(c.min == min && c.max == max);

                int result = c.cur;
                nextChoiceIdx++;
                return result;
            }
        }

        public void BeginSequence()
        {
            Debug.Assert(choices.Count == 0 || nextChoiceIdx == choices.Count);

            while (choices.Count > 0)
            {
                Choice foundChoice = choices.Last();
                if (foundChoice.cur == foundChoice.max)
                {
                    choices.RemoveAt(choices.Count - 1);
                    if (choices.Count == 0)
                        done = true;
                }
                else
                {
                    // found a thing we can increment
                    foundChoice.cur++;
                    choices[choices.Count - 1] = foundChoice;
                    break;
                }
            }

            nextChoiceIdx = 0;
        }

        void Restart()
        {
            done = false;
        }
    }
}
