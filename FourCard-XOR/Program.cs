using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using Fractions;
using Microsoft.Win32.SafeHandles;
using QuikGraph;
using QuikGraph.Graphviz;
using QuikGraph.Graphviz.Dot;

// Record that stores node information
public record Node(string Id, string Caption, GraphvizColor Color);

class FourCardXOR
{
    const int NUM_OF_CARDS = 4;

    public static readonly List<List<List<List<int>>>> SHUFFLES = new List<List<List<List<int>>>>(){
    //    new List<List<List<int>>>(){new List<List<int>>(){}},
       new List<List<List<int>>>(){new List<List<int>>(){}, new List<List<int>>(){new List<int>(){1,2}}},
       new List<List<List<int>>>(){new List<List<int>>(){}, new List<List<int>>(){new List<int>(){1,3}}},
       new List<List<List<int>>>(){new List<List<int>>(){}, new List<List<int>>(){new List<int>(){1,4}}},
       new List<List<List<int>>>(){new List<List<int>>(){}, new List<List<int>>(){new List<int>(){2,3}}},
       new List<List<List<int>>>(){new List<List<int>>(){}, new List<List<int>>(){new List<int>(){2,4}}},
       new List<List<List<int>>>(){new List<List<int>>(){}, new List<List<int>>(){new List<int>(){3,4}}},
       new List<List<List<int>>>(){new List<List<int>>(){}, new List<List<int>>(){new List<int>(){1,2,3}}, new List<List<int>>(){new List<int>(){1,3,2}}},
       new List<List<List<int>>>(){new List<List<int>>(){}, new List<List<int>>(){new List<int>(){1,2,4}}, new List<List<int>>(){new List<int>(){1,4,2}}},
       new List<List<List<int>>>(){new List<List<int>>(){}, new List<List<int>>(){new List<int>(){1,3,4}}, new List<List<int>>(){new List<int>(){1,4,3}}},
       new List<List<List<int>>>(){new List<List<int>>(){}, new List<List<int>>(){new List<int>(){2,3,4}}, new List<List<int>>(){new List<int>(){2,4,3}}},
       new List<List<List<int>>>(){new List<List<int>>(){}, new List<List<int>>(){new List<int>(){1,2,3,4}}, new List<List<int>>(){new List<int>(){1,3}, new List<int>(){2,4}}, new List<List<int>>(){new List<int>(){1,4,3,2}}},
       new List<List<List<int>>>(){new List<List<int>>(){}, new List<List<int>>(){new List<int>(){1,2,4,3}}, new List<List<int>>(){new List<int>(){1,4}, new List<int>(){2,3}}, new List<List<int>>(){new List<int>(){1,3,4,2}}},
       new List<List<List<int>>>(){new List<List<int>>(){}, new List<List<int>>(){new List<int>(){1,3,2,4}}, new List<List<int>>(){new List<int>(){1,2}, new List<int>(){3,4}}, new List<List<int>>(){new List<int>(){1,4,2,3}}},
    };

    public static readonly List<string> SHUFFLES_CAPTION = new List<string>()
    {
        // "shuf {id}",
        "shuf {id, (1 2)}",
        "shuf {id, (1 3)}",
        "shuf {id, (1 4)}",
        "shuf {id, (2 3)}",
        "shuf {id, (2 4)}",
        "shuf {id, (3 4)}",
        "shuf {id, (1 2 3), (1 3 2)}",
        "shuf {id, (1 2 4), (1 4 2)}",
        "shuf {id, (1 3 4), (1 4 3)}",
        "shuf {id, (2 3 4), (2 4 3)}",
        "shuf {id, (1 2 3 4), (1 3)(2 4), (1 4 3 2)}",
        "shuf {id, (1 2 4 3), (1 4)(2 3), (1 3 4 2)}",
        "shuf {id, (1 3 2 4), (1 2)(3 4), (1 4 2 3)}",
    };

    public enum Symbols
    {
        Club,
        Heart,
    }

    public static string ToStringFromEnum(Symbols value)
    {
        switch (value)
        {
            case Symbols.Club: return "♣";
            case Symbols.Heart: return "♥";
            default: throw new InvalidOperationException();
        }
    }

    public static void CopySymbols(Symbols[] to, Symbols[] org)
    {
        for (int i = 0; i < NUM_OF_CARDS; i++)
        {
            if (org[i].Equals(Symbols.Club))
            {
                to[i] = Symbols.Club;
            }
            else
            {
                to[i] = Symbols.Heart;
            }
        }
    }

    public static void CopySymbol(ref Symbols to, Symbols org)
    {
        if (org.Equals(Symbols.Club))
        {
            to = Symbols.Club;
        }
        else
        {
            to = Symbols.Heart;
        }
    }

    public class CardRow
    {
        public Symbols[] symbols { get; set; }
        public Fraction X00 { get; set; }
        public Fraction X01 { get; set; }
        public Fraction X10 { get; set; }
        public Fraction X11 { get; set; }
        public CardRow()
        {
            this.symbols = new Symbols[NUM_OF_CARDS];
            this.X00 = new Fraction();
            this.X01 = new Fraction();
            this.X10 = new Fraction();
            this.X11 = new Fraction();
        }
    }

    public static int stateCounter = 0;

    public class OrgState
    {
        public State state { get; set; }
        public string prevAction { get; set; }
        public OrgState(State state, string prevAction)
        {
            this.state = state;
            this.prevAction = prevAction;
        }
    }

    public class State
    {
        public List<OrgState> orgStates { get; set; }
        public int stateNumber { get; set; }
        public bool isFinal { get; set; }
        public string stateDescription
        {
            get
            {
                string returnStr = $"☆state{stateNumber}\n";
                foreach (var orgState in orgStates)
                {
                    returnStr += $"orgState:state{orgState.state.stateNumber}, prevAction:{orgState.prevAction}\n";
                }
                if (isFinal)
                {
                    returnStr += $"FINAL\n";
                }
                returnStr += "---\n";
                foreach (CardRow row in this.rows)
                {
                    foreach (var symbol in row.symbols)
                    {
                        returnStr += ToStringFromEnum(symbol);
                    }
                    returnStr += " ";
                    returnStr += $"({row.X00})*X00 + ";
                    returnStr += $"({row.X01})*X01 + ";
                    returnStr += $"({row.X10})*X10 + ";
                    returnStr += $"({row.X11})*X11";
                    returnStr += "\n";
                }
                returnStr += "\n";
                return returnStr;
            }
        }
        public List<CardRow> rows { get; set; }
        public State()
        {
            this.rows = new List<CardRow>();
            stateNumber = stateCounter++;
            isFinal = false;
            this.orgStates = new List<OrgState>();
        }
        public State(State orgState, string prevAction) : this()
        {
            this.orgStates.Add(new OrgState(orgState, prevAction));
        }
    }

    public static State getStartState()
    {
        State startState = new State();
        CardRow x00 = new CardRow();
        x00.symbols[0] = Symbols.Club;
        x00.symbols[1] = Symbols.Heart;
        x00.symbols[2] = Symbols.Club;
        x00.symbols[3] = Symbols.Heart;
        x00.X00 = new Fraction(1);
        CardRow x01 = new CardRow();
        x01.symbols[0] = Symbols.Club;
        x01.symbols[1] = Symbols.Heart;
        x01.symbols[2] = Symbols.Heart;
        x01.symbols[3] = Symbols.Club;
        x01.X01 = new Fraction(1);
        CardRow x10 = new CardRow();
        x10.symbols[0] = Symbols.Heart;
        x10.symbols[1] = Symbols.Club;
        x10.symbols[2] = Symbols.Club;
        x10.symbols[3] = Symbols.Heart;
        x10.X10 = new Fraction(1);
        CardRow x11 = new CardRow();
        x11.symbols[0] = Symbols.Heart;
        x11.symbols[1] = Symbols.Club;
        x11.symbols[2] = Symbols.Heart;
        x11.symbols[3] = Symbols.Club;
        x11.X11 = new Fraction(1);
        startState.rows.Add(x00);
        startState.rows.Add(x01);
        startState.rows.Add(x10);
        startState.rows.Add(x11);
        return startState;
    }

    public static bool checkTurnability(State state, int index)
    {
        Fraction club_x00 = new Fraction(0);
        Fraction club_x01 = new Fraction(0);
        Fraction club_x10 = new Fraction(0);
        Fraction club_x11 = new Fraction(0);
        Fraction heart_x00 = new Fraction(0);
        Fraction heart_x01 = new Fraction(0);
        Fraction heart_x10 = new Fraction(0);
        Fraction heart_x11 = new Fraction(0);
        if (state.rows.Count(x => x.symbols[index] == Symbols.Club) == state.rows.Count()
        || state.rows.Count(x => x.symbols[index] == Symbols.Heart) == state.rows.Count())
        {
            return false;
        }
        foreach (var row in state.rows)
        {
            // Club
            if (row.symbols[index] == Symbols.Club)
            {
                club_x00 += row.X00;
                club_x01 += row.X01;
                club_x10 += row.X10;
                club_x11 += row.X11;
            }
            // Heart
            else
            {
                heart_x00 += row.X00;
                heart_x01 += row.X01;
                heart_x10 += row.X10;
                heart_x11 += row.X11;
            }
        }
        if (!(club_x00 == club_x01 && club_x00 == club_x10 && club_x00 == club_x11)
        || !(heart_x00 == heart_x01 && heart_x00 == heart_x10 && heart_x00 == heart_x11))
        {
            return false;
        }
        return true;
    }

    public static List<State> turn(State orgState, int index)
    {
        State state_club = new State(orgState, $"turn_{index + 1}_{ToStringFromEnum(Symbols.Club)}");
        State state_heart = new State(orgState, $"turn_{index + 1}_{ToStringFromEnum(Symbols.Heart)}");
        foreach (CardRow row in orgState.rows)
        {
            CardRow newRow = new CardRow();
            CopySymbols(newRow.symbols, row.symbols);
            newRow.X00 = new Fraction(row.X00.Numerator, row.X00.Denominator);
            newRow.X01 = new Fraction(row.X01.Numerator, row.X01.Denominator);
            newRow.X10 = new Fraction(row.X10.Numerator, row.X10.Denominator);
            newRow.X11 = new Fraction(row.X11.Numerator, row.X11.Denominator);
            if (row.symbols[index] == Symbols.Club)
            {
                state_club.rows.Add(newRow);
            }
            else
            {
                state_heart.rows.Add(newRow);
            }
        }
        // 正規化
        Fraction x00_club = new Fraction(0);
        Fraction x01_club = new Fraction(0);
        Fraction x10_club = new Fraction(0);
        Fraction x11_club = new Fraction(0);
        foreach (var row in state_club.rows)
        {
            x00_club += row.X00;
            x01_club += row.X01;
            x10_club += row.X10;
            x11_club += row.X11;
        }
        foreach (var row in state_club.rows)
        {
            if (x00_club != 0)
            {
                row.X00 /= x00_club;
            }
            if (x01_club != 0)
            {
                row.X01 /= x01_club;
            }
            if (x10_club != 0)
            {
                row.X10 /= x10_club;
            }
            if (x11_club != 0)
            {
                row.X11 /= x11_club;
            }
        }

        Fraction x00_heart = new Fraction(0);
        Fraction x01_heart = new Fraction(0);
        Fraction x10_heart = new Fraction(0);
        Fraction x11_heart = new Fraction(0);
        foreach (var row in state_heart.rows)
        {
            x00_heart += row.X00;
            x01_heart += row.X01;
            x10_heart += row.X10;
            x11_heart += row.X11;
        }
        foreach (var row in state_heart.rows)
        {
            if (x00_heart != 0)
            {
                row.X00 /= x00_heart;
            }
            if (x01_heart != 0)
            {
                row.X01 /= x01_heart;
            }
            if (x10_heart != 0)
            {
                row.X10 /= x10_heart;
            }
            if (x11_heart != 0)
            {
                row.X11 /= x11_heart;
            }
        }
        return new List<State>() { state_club, state_heart };
    }

    public static bool isFinalState(State state)
    {
        List<int> index = new List<int>() { };
        for (int i = 0; i < NUM_OF_CARDS; i++)
        {
            index.Add(i);
        }

        List<CardRow> output_0 = state.rows.FindAll(x => x.X00 > new Fraction(0) || x.X11 > new Fraction(0));
        List<CardRow> output_1 = state.rows.FindAll(x => x.X01 > new Fraction(0) || x.X10 > new Fraction(0));

        List<int> output_0_club = index.FindAll(i => output_0.Count(x => x.symbols[i] == Symbols.Club) == output_0.Count());
        List<int> output_0_heart = index.FindAll(i => output_0.Count(x => x.symbols[i] == Symbols.Heart) == output_0.Count());
        List<int> output_1_club = index.FindAll(i => output_1.Count(x => x.symbols[i] == Symbols.Club) == output_1.Count());
        List<int> output_1_heart = index.FindAll(i => output_1.Count(x => x.symbols[i] == Symbols.Heart) == output_1.Count());

        List<int> club_heart = output_0_club.FindAll(output_1_heart.Contains);
        List<int> heart_club = output_0_heart.FindAll(output_1_club.Contains);

        return club_heart.Count() != 0 && heart_club.Count() != 0;
    }

    public static bool hasBotSequence(State state)
    {
        foreach (var row in state.rows)
        {
            if (row.X00 + row.X11 > new Fraction(0) && row.X01 + row.X10 > new Fraction(0)) return true;
        }
        return false;
    }

    public static bool checkStateEquality(State a, State b)
    {
        if (a.rows.Count != b.rows.Count) return false;
        foreach (var row in a.rows)
        {
            if (b.rows.FirstOrDefault(x => x.symbols.SequenceEqual(row.symbols)) != null)
            {
                CardRow row_a = b.rows.First(x => x.symbols.SequenceEqual(row.symbols));
                if (row_a.X00 == row.X00 && row_a.X01 == row.X01 && row_a.X10 == row.X10 && row_a.X11 == row.X11)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    public static List<State> shufflesWithValidityCheck(State orgState)
    {
        List<State> newStateList = new List<State>();
        for (int i = 0; i < SHUFFLES.Count; i++)
        {
            State newState = new State(orgState, SHUFFLES_CAPTION[i]);
            foreach (CardRow orgRow in orgState.rows) //For each row of cards present in the state before the shuffle
            {
                foreach (var shuf in SHUFFLES[i]) //Create each of the shuffle transitions (calculation for each of (id, (1 2)) etc.)
                {
                    CardRow newRow = new CardRow();
                    CopySymbols(newRow.symbols, orgRow.symbols);
                    foreach (var perm in shuf) //For each of the card pairs to be permed (loops for cases such as (1 3)(2 4))
                    {
                        Symbols tmpSym = new Symbols();
                        CopySymbol(ref tmpSym, newRow.symbols[perm[(perm.Count - 1)] - 1]);
                        for (var j = 0; j < perm.Count; j++)
                        {
                            if (j == 0) continue;
                            CopySymbol(ref newRow.symbols[perm[j] - 1], orgRow.symbols[perm[j - 1] - 1]);
                            if (j == perm.Count - 1)
                            {
                                CopySymbol(ref newRow.symbols[perm[0] - 1], tmpSym);
                            }
                        }
                    }
                    // Calculate probabilities from inputs.
                    newRow.X00 = orgRow.X00 / SHUFFLES[i].Count;
                    newRow.X01 = orgRow.X01 / SHUFFLES[i].Count;
                    newRow.X10 = orgRow.X10 / SHUFFLES[i].Count;
                    newRow.X11 = orgRow.X11 / SHUFFLES[i].Count;
                    // Is there a cardRow with the same card row in the state being calculated?
                    if (newState.rows.FirstOrDefault(x => x.symbols.SequenceEqual(newRow.symbols)) == null)
                    {
                        //Add card column to state if not present
                        newState.rows.Add(newRow);
                    }
                    else
                    {
                        //If present, only the coefficients of the probabilistic variable are added
                        CardRow sameSymbolsState = newState.rows.First(x => x.symbols.SequenceEqual(newRow.symbols));
                        sameSymbolsState.X00 += newRow.X00;
                        sameSymbolsState.X01 += newRow.X01;
                        sameSymbolsState.X10 += newRow.X10;
                        sameSymbolsState.X11 += newRow.X11;
                    }
                }
            }
            // Does it contain a bot-sequence? or identical to the original state?
            if (hasBotSequence(newState) || checkStateEquality(newState, orgState)) continue;
            // Add as a new state
            newStateList.Add(newState);
        }
        return newStateList;
    }

    public static void Main(string[] args)
    {
        // Prepare initial state.
        State startState = getStartState();

        int phaseCounter = 0;
        List<State> existStates = new List<State>() { startState }; //existing state
        List<State> actionStates = new List<State>() { startState }; //operating state
        List<State> rcToTurnStates = new List<State>();
        List<State> turnToRcStates = new List<State>();
        DateTime start = DateTime.Now;
        while (true)
        {
            // states obtained from random cuts.
            List<State> rcStates = new List<State>();
            List<State> startStates = new List<State>(actionStates);
            foreach (var state in actionStates)
            {
                // Applying random cuts
                List<State> shuffledState = shufflesWithValidityCheck(state);
                foreach (var shuffledNewState in shuffledState)
                {
                    if (existStates.FindAll(x => checkStateEquality(x, shuffledNewState)).Count == 0)
                    {
                        existStates.Add(shuffledNewState);
                        rcStates.Add(shuffledNewState);
                    }
                    else
                    {
                        // Specify the destination of the transition.
                        State existedState = existStates.First(x => checkStateEquality(x, shuffledNewState));
                        existedState.orgStates.Add(new OrgState(shuffledNewState.orgStates[0].state, shuffledNewState.orgStates[0].prevAction));
                    }
                }
            }
            actionStates = new List<State>(rcStates).ToList();
            // state obtained from turn
            List<State> turnedStates = new List<State>();
            while (true)
            {
                List<State> turnedStatesInLoop = new List<State>();
                foreach (var state in actionStates)
                {
                    // Check turnability -> If possible, check whether turn + termination is possible -> If possible, operate termination flag; if not possible, save state only
                    for (int i = 0; i < NUM_OF_CARDS; i++)
                    {
                        if (checkTurnability(state, i))
                        {
                            List<State> turnStates = turn(state, i);
                            foreach (var turnedState in turnStates)
                            {
                                if (isFinalState(turnedState))
                                {
                                    turnedState.isFinal = true;
                                }
                                if (existStates.FindAll(x => checkStateEquality(turnedState, x)).Count == 0)
                                {
                                    existStates.Add(turnedState);
                                    turnedStates.Add(turnedState);
                                    turnedStatesInLoop.Add(turnedState);
                                }
                                else
                                {
                                    State existedState = existStates.First(x => checkStateEquality(x, turnedState));
                                    existedState.orgStates.Add(new OrgState(turnedState.orgStates[0].state, turnedState.orgStates[0].prevAction));
                                }
                            }
                        }
                    }
                }
                if (turnedStatesInLoop.Count() == 0)
                {
                    break;
                }
                else
                {
                    actionStates = new List<State>(turnedStatesInLoop);
                }
            }
            DateTime end_loop = DateTime.Now;
            // output
            using (StreamWriter writer = new StreamWriter($"log/phase{++phaseCounter}.log", true))
            {
                writer.Write($"phase{phaseCounter}: {(end_loop - start).TotalMilliseconds} ms\nTotal:{existStates.Count},+RC:{rcStates.Count},+Turn:{turnedStates.Count},Final?:{existStates.Count(x => x.isFinal) > 0}");
                writer.Write("\n\n");
                foreach (var state in existStates)
                {
                    writer.Write(state.stateDescription);
                }
            }

            var graph = new AdjacencyGraph<Node, TaggedEdge<Node, string>>();
            Dictionary<int, Node> existNodes = new Dictionary<int, Node>();
            var newStates = new List<State>(rcStates).Concat(turnedStates).ToList();
            foreach (var state in newStates)
            {
                var node = new Node($"state{state.stateNumber}", state.stateDescription, GraphvizColor.Orange);
                if (!existNodes.ContainsKey(state.stateNumber))
                {
                    existNodes.Add(state.stateNumber, node);
                }
                graph.AddVertex(node);
                foreach (var orgState in state.orgStates)
                {
                    if (!existNodes.ContainsKey(orgState.state.stateNumber))
                    {
                        var orgNode = new Node($"state{orgState.state.stateNumber}", orgState.state.stateDescription, GraphvizColor.Aqua);
                        existNodes.Add(orgState.state.stateNumber, orgNode);
                        graph.AddVertex(orgNode);
                    }
                }
            }
            foreach (var state in newStates)
            {
                foreach (var orgState in state.orgStates)
                {
                    graph.AddEdge(new TaggedEdge<Node, string>(existNodes[orgState.state.stateNumber], existNodes[state.stateNumber], $"{orgState.prevAction}"));
                }
            }
            var graphviz = new GraphvizAlgorithm<Node, TaggedEdge<Node, string>>(graph);
            graphviz.GraphFormat.RankSeparation = 1.5;
            graphviz.GraphFormat.NodeSeparation = 0.5;
            graphviz.GraphFormat.Font = new GraphvizFont("Yu Gothic", 12);
            graphviz.FormatVertex += (sender, e) =>
            {
                e.VertexFormat.Label = e.Vertex.Caption.Replace("\n", "\\l").Replace("{", "\\{").Replace("}", "\\}");
                e.VertexFormat.Shape = GraphvizVertexShape.Record;
                e.VertexFormat.Style = GraphvizVertexStyle.Filled;
                e.VertexFormat.FillColor = e.Vertex.Color;
            };
            graphviz.FormatEdge += (sender, e) =>
            {
                e.EdgeFormat.Label = new GraphvizEdgeLabel { Value = e.Edge.Tag };
            };
            string dotContent = graphviz.Generate();
            File.WriteAllText($"node_graph_phase{phaseCounter}.dot", dotContent);

            if (rcStates.Count == 0 && turnedStates.Count == 0)
            {
                Console.WriteLine("FINISHED");
                return;
            }
            if (phaseCounter >= 10)
            {
                return;
            }
            actionStates = new List<State>(rcStates).Concat(turnedStates).ToList();
        }
    }
}
