using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Trax
{

    public class ScnEvent {
        #region Fields

        private static IFormatProvider FP = System.Globalization.CultureInfo.InvariantCulture; // needed to have C default decimal separator
        private static System.Globalization.NumberStyles NS = System.Globalization.NumberStyles.Float; // needed for default float numbers format
        private float Delay;
        string MemCellName = "none";
        #endregion

        #region Properties
        public EventTypes Type { get; internal set; }
        public string Name { get; internal set; }
        public ScnMemCell MemCell { get; internal set; }
        public List<object> Values { get; internal set; }

       #endregion

        #region Methods


        public ScnEvent(string source, List<string> parameters) {
            //initialize
            Values = new List<object>();
            //scana source
            int sourceLength = source.Length;
            int sourceLastIndex = sourceLength - 1;
            int noValues = 0;
            int block = 0;
            EventStates state = EventStates.Name;
            Stack<EventStates> states = new Stack<EventStates>();
            string fragment = "";
            char c;
            bool isEndLine = false;
            bool isWhiteSpace;
            bool isCommentStart, lastCommentStart = false, isComment = false;
            bool isParamStart = false, isParamEnd = false;
            bool isEnd = false;
            // analysis:
            for (int i = 0; i < sourceLength; i++) {
                c = source[i];
                isEndLine = c == '\n';
                isWhiteSpace = c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == ';'; // all whitespace and separators defined in SCN format
                isCommentStart = c == '/';
                isParamStart = c == '(';
                isParamEnd = c == ')';
                isComment = isCommentStart && lastCommentStart; // true on second '/' in a row
                lastCommentStart = isCommentStart;
                isEnd = isWhiteSpace || isComment || i == sourceLastIndex; // identifier is considered ended after whitespace character, comment start or end of data
                switch (state) {
                    case EventStates.Name:
                        if (!isWhiteSpace) fragment += c;
                        if (isParamStart) {
                            states.Push(state);
                            state = EventStates.Parameter;
                        }
                        else if (isEnd) {
                            Name = fragment;
                            state = EventStates.Type;
                            fragment = "";
                        }
                        break;
                    case EventStates.Type:
                        if (isWhiteSpace) {
                            if (Type == EventTypes.UpdateValues) {
                                Type = Tools.GetEventTypeFromString(fragment);
                                state = EventStates.Value;
                                block = 1;
                                noValues = 2;
                            }
                            else {
                                Type = Tools.GetEventTypeFromString(fragment);
                                state = EventStates.Timing;
                            }
                            fragment = "";
                        }
                        else fragment += c;
                        break;
                    case EventStates.Comment:
                        if (isEndLine) { state = states.Pop(); fragment = ""; }
                        break;
                    case EventStates.Parameter:
                        if (isParamEnd) {
                            fragment += c;
                            string p = fragment.Substring(fragment.IndexOf('('));
                            string n = "";
                            foreach (char c1 in p) {
                                if (c1 >= '0' && c1 <= '9') { n += c1; }
                            }
                            int cyf = int.Parse(n);
                            fragment = fragment.Replace(p, parameters[cyf - 1]);
                            state = states.Pop();
                        }
                        else fragment += c;
                        break;
                    case EventStates.Timing:
                        if (c == '-' || c == '.' || (c >= '0' && c <= '9')) { fragment += c; continue; }
                        else if (isEnd) {
                            Delay = float.Parse(fragment,FP);
                            if (Type == EventTypes.GetValues || Type == EventTypes.UpdateValues
                                || Type == EventTypes.Multiple) state = EventStates.MemCell;
                            else if (Type == EventTypes.Switch) {
                                state = EventStates.Value;
                                block = 1;
                                noValues = 2;
                            }
                            else if (Type == EventTypes.PutValues) {
                                state = EventStates.NonNecessaryValue;
                                noValues = 4;
                                block = 1;
                            }
                            else state = EventStates.Scan;
                            fragment = "";
                        }
                        break;
                    case EventStates.MemCell:
                        if (!isWhiteSpace) fragment += c;
                        if (isEnd) {
                            MemCellName = Tools.ChangeParamsToText(fragment.TrimEnd(new[] { '/' }),parameters);
                            if (Type == EventTypes.UpdateValues)
                                state = EventStates.Type;
                            else if (Type == EventTypes.Multiple) state = EventStates.MultipleValue;
                            else
                                state = EventStates.Scan;
                            fragment = "";
                        }
                        break;
                    case EventStates.NonNecessaryValue:
                        if (isEnd && block < noValues) {
                            block++;
                        }
                        else if (isEnd) {
                            state = EventStates.Type;
                        }
                        break;
                    case EventStates.Value:
                        if (!isWhiteSpace) fragment += c;
                        if (isEnd) {
                            double n;
                            if (Double.TryParse(fragment, NS, FP, out n)) Values.Add(n);
                            else Values.Add(Tools.ChangeParamsToText(fragment.TrimEnd(new[] { '/' }), parameters));
                            fragment = "";
                            block++;
                        }
                        if (block > noValues) state = EventStates.Scan;
                        break;
                    case EventStates.MultipleValue:
                        if (!isWhiteSpace) fragment += c;
                        if (fragment == "endevent" || fragment == "condition") {
                            state = EventStates.Scan;
                            fragment = "";
                            break;
                        }
                        if (isEnd) {
                            Values.Add(Tools.ChangeParamsToText(fragment.TrimEnd(new[] { '/' }), parameters));
                            fragment = "";
                        }
                        break;

                }
                if (isComment && state != EventStates.Comment) { states.Push(state); state = EventStates.Comment; }
            }

        }

        public void ConnectEventToMemCell(Dictionary<string,ScnMemCell> memcell_dict) {
            if (MemCellName != "none" && memcell_dict.ContainsKey(MemCellName)) {
                memcell_dict[MemCellName].EventCollection.Add(this);
                MemCell = memcell_dict[MemCellName];
            }

        }

        #endregion
    }

 }
