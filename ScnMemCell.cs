using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Trax {

    public class ScnMemCell {

        //public enum Types { Updatevalues, Multiple, PowerSource, Friction, GetValues, Semaphor, RoadVelocity, SectionVelocity, Lights };
        internal enum States { Scan, Parameter };
        private static IFormatProvider FP = System.Globalization.CultureInfo.InvariantCulture; // needed to have C default decimal separator

        #region Fields
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        #endregion

        #region Properties
        public EventTypes Type { get; internal set; }
        public string Name { get; internal set; }
        public V3D Point { get; internal set; }
        public List<ScnEvent> EventCollection { get; set; }

        public double Angle { get; internal set; }

        public Dictionary<double, Dictionary<double, string>> SpeedListNormal { get; internal set; }
        public Dictionary<double, string> SpeedListShunt { get; internal set; }

        #endregion

        public ScnMemCell(ScnNodeLexer.Node node, List<string> param_list, V3D rotation) {
            Point = new V3D();
            EventCollection = new List<ScnEvent>();
            SpeedListNormal = new Dictionary<double, Dictionary<double, string>>();
            SpeedListShunt = new Dictionary<double, string>();
            int block = 0;
            Angle = rotation.Y;
            Name = Tools.ChangeParamsToText(node.Name, param_list);
                foreach (var value in node.Values) {
                try {
                    switch (block++) {
                        case 0: Point.X = value.GetType() != typeof(double) ? double.Parse(Tools.ChangeParamsToText((string)value, param_list), FP) : (double)value; break;
                        case 1: Point.Y = value.GetType() != typeof(double) ? double.Parse(Tools.ChangeParamsToText((string)value, param_list), FP) : (double)value; break;
                        case 2: Point.Z = value.GetType() != typeof(double) ? double.Parse(Tools.ChangeParamsToText((string)value, param_list), FP) : (double)value; break;
                        case 3: Type = value != null? Tools.GetEventTypeFromString((string)value): EventTypes.Unknown; break;
                    }
                }
                catch (Exception x) {
                    log.Error(x.Message + "\r\n" + x.StackTrace + "\r\n");
                }
            }
        }

        public void GetSpeedListFromEvents(Dictionary<string, ScnEvent> event_dict) {
            if (Type == EventTypes.SetVelocity || Type == EventTypes.ShuntVelocity) { //tylko dla semaforów
                var list_here = new Dictionary<string, double>();
                var list_next = new Dictionary<string, double>();
                foreach (var e in EventCollection) {
                    switch (e.Type) {
                        case EventTypes.SetVelocity: //setvelocity
                            if (typeof(double) == e.Values[0].GetType())
                                list_here.Add(e.Name, (double)e.Values[0]);
                            if (typeof(double) == e.Values[1].GetType())
                                list_next.Add(e.Name, (double)e.Values[1]);
                            break;
                        case EventTypes.ShuntVelocity: //shuntvelocity
                            if (typeof(double) == e.Values[0].GetType())
                                SpeedListShunt.Add((double)e.Values[0], e.Name);
                            break;
                    }
                }
                //połączyć eventy ze soba
                foreach (var speed in list_here) {
                    if (!list_next.ContainsKey(speed.Key)) {
                        //będzie w paru różnych miejscach użyte w zwykłej tabelce eventów
                        foreach (var e in event_dict) {
                            if (e.Value.Values.Contains(speed.Key)) {
                                foreach (var es in e.Value.Values) {
                                    if (list_next.TryGetValue((string)es, out double val_next)) {
                                        if (!SpeedListNormal.ContainsKey(speed.Value))
                                            SpeedListNormal.Add(speed.Value, new Dictionary<double, string>());
                                        SpeedListNormal[speed.Value].Add(val_next, e.Value.Name);
                                    }
                                }
                            }
                        }
                    }
                    else {
                        //jest jeden event ustawiający obie wartości - zapewne zastępczy lub s1
                        SpeedListNormal.Add(speed.Value, new Dictionary<double, string>());
                        var e_name = speed.Key;
                        foreach (var e in event_dict)
                            if (e.Value.Values.Contains(speed.Key)) {
                                e_name = e.Value.Name;
                                break;
                            }
                        SpeedListNormal[speed.Value].Add(list_next[speed.Key], e_name);
                    }
                }
            }
        }
    }

    public class ScnMemCellCollection : Dictionary<string, ScnMemCell> {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();


        public static ScnMemCellCollection Parse(ref string text, List<string> param_list, V3D rotation) {
            var cells = new ScnMemCellCollection();
            try {
                var lexer = new ScnNodeLexer(text, "memcell");
                if (lexer.Nodes != null)
                    foreach (var node in lexer.Nodes) {
                        var m = new ScnMemCell(node, param_list, rotation);
                        cells.Add(m.Name, m);
                        log.Trace("memcell, type {0}, name {1} params {2}", m.Type, m.Name, param_list);
                    }
            }
            catch (Exception x) {
                log.Error(x.Message + "\r\n" + x.StackTrace + "\r\n");
                //System.Windows.Forms.MessageBox.Show(x.Message + "\r\n\r\n" + x.StackTrace);
            }
            return cells;
        }
    }
}
