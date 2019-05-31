using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Trax {

    public class ScnSignal {
        private PointF Point;
        private EventTypes Type;
        private string Name;
        public ScnSignal (ScnMemCell memcell) {
            Point = memcell.Point;
            Type = memcell.Type;
            Name = memcell.Name;
        }

        /// <summary>
        /// Returns true if any spline point is contained within specified rectangle
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public bool HasPointIn(RectangleF r) {
            return r.Contains(Point);
        }

    }

    public class ScnSignalCollection : List<ScnSignal> {

        public static ScnSignalCollection Load() {
            var signals = new ScnSignalCollection();
            foreach (var m in ProjectFile.MemCellCollection.Values) {
                if (m.Type == EventTypes.SetVelocity || m.Type == EventTypes.ShuntVelocity) {
                    signals.Add(new ScnSignal(m));
                }
            }
            return signals;
        }
    }

}
