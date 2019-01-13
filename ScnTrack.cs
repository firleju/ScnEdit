﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Trax {
    
    /// <summary>
    /// Scenery track definition
    /// </summary>
    public class ScnTrack : ScnVectorObject<ScnTrack> {

        const double LinkDistance = 0.01;

        #region Fields
        
        // definition
        public string Name;
        public string TrackType;
        public double TrackLength;
        public double TrackWidth;
        public double Friction;
        public double SoundDist;
        public int Quality;
        public int DamageFlag;
        public string Environment;
        public string Visibility;
        // optional for visible
        public string Tex1;
        public double? TexLength;
        public string Tex2;
        public double? TexHeight;
        public double? TexWidth;
        public double? TexSlope;
        // geometry
        public V3D Point1;
        public double Roll1;
        public V3D CVec1;
        public V3D CVec2;
        public V3D Point2;
        public double Roll2;
        public double Radius1;
        // geometry (switch)
        public V3D Point3;
        public double? Roll3;
        public V3D CVec3;
        public V3D CVec4;
        public V3D Point4;
        public double? Roll4;
        public double? Radius2;
        // optional
        public double? TrackLength2;
        public double? Velocity;
        public List<string> Event0 = new List<string>();
        public List<string> Event1 = new List<string>();
        public List<string> Event2 = new List<string>();
        public List<string> Eventall0 = new List<string>();
        public List<string> Eventall1 = new List<string>();
        public List<string> Eventall2 = new List<string>();
        public List<string> Isolated = new List<string>();
        public string Extras;
        // editor specific
        public string IncludesBefore;
        public string IncludesAfter;
        // connected tracks
        private ScnTrack[] Connected = new ScnTrack[3] { null, null, null };
        #endregion

        #region Properties

        public bool IsSwitch { get { return Point3 != null; } }
        public bool IsDerail { get { return Point3 != null && Connected[2] == null; } }
        public bool IsTrackEnd { get { return Connected[0] == null ^ Connected[1] == null; } }
        public int SwitchState { get; set; }
        public V3D P1 { get { return SwitchState == 0 ? Point1 : Point3; } }
        public V3D P2 { get { return SwitchState == 0 ? Point2 : Point4; } }
        public ScnTrack[] Neighbors { get { return Connected; } }
        #endregion

        /// <summary>
        /// Track empty constructor
        /// </summary>
        public ScnTrack() { }

        /// <summary>
        /// Track created from lexer node
        /// </summary>
        /// <param name="path"></param>
        /// <param name="buffer"></param>
        /// <param name="node"></param>
        public ScnTrack(string path, ScnNodeLexer.Node node) {
            int i = 0, block = 0;
            string xname = null;
            List<string> extras = new List<string>();
            foreach (var value in node.Values) {
                switch (block) {
                    case 0: // common properties
                        switch (i++) {
                            case 0: TrackType = GetLCString(value); break;
                            case 1: TrackLength = (double)value; break;
                            case 2: TrackWidth = (double)value; break;
                            case 3: Friction = (double)value; break;
                            case 4: SoundDist = (double)value; break;
                            case 5: Quality = (int)(double)value; break;
                            case 6: DamageFlag = (int)(double)value; break;
                            case 7: Environment = GetLCString(value); break;
                            case 8: Visibility = GetLCString(value);
                                block = Visibility == "vis" ? 1 : 2;
                                i = 0;
                                break;
                        }
                        break;
                    case 1: // fields for visible tracks
                        switch (i++) {
                            case 0: Tex1 = GetString(value);  break;
                            case 1: TexLength = (double?)value; break;
                            case 2: Tex2 = GetString(value); break;
                            case 3: TexHeight = (double?)value; break;
                            case 4: TexWidth = (double?)value; break;
                            case 5: TexSlope = (double?)value;
                                block++;
                                i = 0;
                                break;
                        }
                        break;
                    case 2: // track vectors and parameters
                        switch (i++) {
                            case 0: Point1 = new V3D { X = (double)value }; break;
                            case 1: Point1.Y = (double)value; break;
                            case 2: Point1.Z = (double)value; break;
                            case 3: Roll1 = (double)value; break;
                            case 4: CVec1 = new V3D { X = (double)value }; break;
                            case 5: CVec1.Y = (double)value; break;
                            case 6: CVec1.Z = (double)value; break;
                            case 7: CVec2 = new V3D { X = (double)value }; break;
                            case 8: CVec2.Y = (double)value; break;
                            case 9: CVec2.Z = (double)value; break;
                            case 10: Point2 = new V3D { X = (double)value }; break;
                            case 11: Point2.Y = (double)value; break;
                            case 12: Point2.Z = (double)value; break;
                            case 13: Roll2 = (double)value; break;
                            case 14: Radius1 = (double)value;
                                block = (TrackType == "switch" || TrackType == "cross") ? 3 : 4;
                                i = 0;
                                break;
                        }
                        break;
                    case 3: // switch vectors and parameters
                        switch (i++) {
                            case 0: Point3 = new V3D { X = (double)value }; break;
                            case 1: Point3.Y = (double)value; break;
                            case 2: Point3.Z = (double)value; break;
                            case 3: Roll3 = (double)value; break;
                            case 4: CVec3 = new V3D { X = (double)value }; break;
                            case 5: CVec3.Y = (double)value; break;
                            case 6: CVec3.Z = (double)value; break;
                            case 7: CVec4 = new V3D { X = (double)value }; break;
                            case 8: CVec4.Y = (double)value; break;
                            case 9: CVec4.Z = (double)value; break;
                            case 10: Point4 = new V3D { X = (double)value }; break;
                            case 11: Point4.Y = (double)value; break;
                            case 12: Point4.Z = (double)value; break;
                            case 13: Roll4 = (double)value; break;
                            case 14: Radius2 = (double)value;
                                block++;
                                i = 0;
                                break;
                        }
                        break;
                    case 4: // extras
                        if (i++ % 2 == 0) xname = GetLCString(value);
                        else {
                            switch (xname) {
                                case "velocity": Velocity = (double)value; break;
                                case "event0": Event0.Add((string)value); break;
                                case "event1": Event1.Add((string)value); break;
                                case "event2": Event2.Add((string)value); break;
                                case "eventall0": Eventall0.Add((string)value); break;
                                case "eventall1": Eventall1.Add((string)value); break;
                                case "eventall2": Eventall2.Add((string)value); break;
                                case "isolated": Isolated.Add((string)value); break;
                                default: extras.Add(xname); extras.Add(GetString(value)); break;
                            }
                        }
                        break;
                }
            }
            if (extras.Count > 0) Extras = String.Join(" ", extras);
            ScnType = "track";
            SourcePath = path;
            Name = node.Name;
            SourceIndex = node.SourceIndex;
            SourceLength = node.SourceLength;
        }

        /// <summary>
        /// Gets string value from object which could not necessarily be boxed string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string GetString(object value) { return value == null ? null : value.ToString(); }
        
        /// <summary>
        /// Gets lower case string value from object which could not necessarily be boxed string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string GetLCString(object value) { return value == null ? null : value.ToString().ToLowerInvariant(); }


        /// <summary>
        /// Returns true if the distance between 2 points is less than treshold
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private bool IsLinked(V3D p1, V3D p2) {
            if (p1 == null || p2 == null) return false;
            return (p1 - p2).Length < LinkDistance;
        }

        /// <summary>
        /// Returns number of point where given track is connected to. 0 if there is no connection.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private int IsLinked(ScnTrack t) {
            if (IsLinked(Point1, t.Point1) || IsLinked(Point1, t.Point2) || IsLinked(Point1, t.Point4)) {
                return 1;
            }
            else if (IsLinked(Point2, t.Point1) || IsLinked(Point2, t.Point2) || IsLinked(Point2, t.Point4)) {
                return 2;
            }
            else if (IsLinked(Point4, t.Point1) || IsLinked(Point4, t.Point2) || IsLinked(Point4, t.Point4)) {
                return 3;
            }
            else {
                return 0;
            }
        }

        /// <summary>
        /// Returns true if this track's start is linked to given track end
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        public bool IsStartLinkedTo(ScnTrack track, out bool switchEnd) {
            switchEnd = false;
            if (track == null) return false;
            bool mainEnd = IsLinked(Point1, track.Point2) || IsLinked(Point3, track.Point2);
            switchEnd = IsLinked(Point1, track.Point4) || IsLinked(Point3, track.Point4);
            return mainEnd || switchEnd;
        }

        /// <summary>
        /// Returns true if this track's end is linked to given track start
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        public bool IsEndLinkedTo(ScnTrack track, out bool switchEnd) {
            switchEnd = false;
            if (track == null) return false;
            bool mainEnd = IsLinked(Point2, track.Point1) || IsLinked(Point4, track.Point1);
            switchEnd = IsLinked(Point2, track.Point3) || IsLinked(Point4, track.Point3);
            return mainEnd || switchEnd;
        }

        /// <summary>
        /// Returns true if this track is linked to given track
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        public bool IsLinkedTo(ScnTrack track, out bool switchEnd) {
            return IsStartLinkedTo(track, out switchEnd) || IsEndLinkedTo(track, out switchEnd);
        }

        /// <summary>
        /// Returns track that is on opposite site of given track
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        public ScnTrack Next(ScnTrack prev) {
            var n = IsLinked(prev);
            if (n == 1) return SwitchState == 0 ? Connected[1] : Connected[2]; //Point2 i Point4
            else if (n == 2 && n == 3) return Connected[0]; //Point1 || Point3
            else return null;
        }

        /// <summary>
        /// Set track to list of neighbours in given connected point of parent
        /// </summary>
        public void SetNeighbourTrack(ScnTrack neighbour, int connection_no) {
            Connected[connection_no] = neighbour;
        }

        /// <summary>
        /// Set track to list of neighbours with check on wich point it is connected
        /// </summary>
        public void SetNeighbourTrack(ScnTrack neighbour) {
            var n = IsLinked(neighbour);
            if (n > 0)
                Connected[n - 1] = neighbour;
        }

        /// <summary>
        /// Sets Curves field of ScnVectorObject base
        /// </summary>
        public void GetCurves() {
            Curves =
                (IsSwitch)
                    ? new ScnBezier[] {
                        new ScnBezier(Point1, CVec1, CVec2, Point2),
                        new ScnBezier(Point3, CVec3, CVec4, Point4)
                    }
                    : new ScnBezier[] { new ScnBezier(Point1, CVec1, CVec2, Point2) };
        }

        /// <summary>
        /// Calculates track length
        /// </summary>
        /// <param name="n">1 for alternative track in switch</param>
        /// <returns></returns>
        public double GetLength(int? switchState = null) {
            if ((switchState != null ? switchState : SwitchState) == 0) {
                if (CVec1 == null || CVec2 == null || (CVec1.Zero && CVec2.Zero)) return Math.Round((Point1 - Point2).Length, 9);
                return Math.Round(new ScnBezier(Point1, CVec1, CVec2, Point2).QLength, 9);
            } else {
                if (CVec3 == null || CVec4 == null || (CVec3.Zero && CVec4.Zero)) return Math.Round((Point3 - Point4).Length, 9);
                return Math.Round(new ScnBezier(Point3, CVec3, CVec4, Point4).QLength, 9);
            }
        }

        /// <summary>
        /// Calculates track lenghts (main and swith if applicable)
        /// </summary>
        public void GetLengths() {
            TrackLength = GetLength(0);
            if (IsSwitch) TrackLength2 = GetLength(1);
        }

        /// <summary>
        /// Returns track's text representation for debugging purpose
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return Name + " : " + (
                (Point3 != null && Point4 != null)
                    ? String.Format("{0} -> {1} x {2} -> {3}", Point1, Point2, Point3, Point4)
                    : String.Format("{0} -> {1}", Point1, Point2)
                );
        }

        public string AsText() {
            string text = IncludesBefore != null ? (IncludesBefore + "\r\n") : "";
            string basePart =
                String.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "node -1 0 {0} track {1} {2} {3} {4} {5} {6}",
                    Name, TrackType, ScnNumbers.ToString(new[] { TrackLength, TrackWidth, Friction, SoundDist }),
                    Quality, DamageFlag, Environment.ToLowerInvariant(), Visibility.ToLowerInvariant()
                );
            text += basePart;
            string visiblePart = null;
            if (Tex1 != null || Tex2 != null)
                visiblePart =
                    String.Format(
                        "{0} {1} {2} {3}",
                        Tex1 ?? "none", ScnNumbers.ToString(TexLength), Tex2 ?? "none", ScnNumbers.ToString(new[] { TexHeight, TexWidth, TexSlope })
                    );
            string trackPart =
                String.Format(
                    "{0}\r\n{1}\r\n{2}\r\n{3}\r\n{4}",
                    ScnNumbers.ToString(new[] { Point1.X, Point1.Y, Point1.Z, Roll1 }),
                    ScnNumbers.ToString(new[] { CVec1.X, CVec1.Y, CVec1.Z }),
                    ScnNumbers.ToString(new[] { CVec2.X, CVec2.Y, CVec2.Z }),
                    ScnNumbers.ToString(new[] { Point2.X, Point2.Y, Point2.Z, Roll2 }),
                    ScnNumbers.ToString(Radius1)
                );
            string switchPart = null;
            if (Point3 != null && Point4 != null)
                switchPart =
                    String.Format(
                        "{0}\r\n{1}\r\n{2}\r\n{3}\r\n{4}",
                        ScnNumbers.ToString(new[] { Point3.X, Point3.Y, Point3.Z, Roll3 }),
                        ScnNumbers.ToString(new[] { CVec3.X, CVec3.Y, CVec3.Z }),
                        ScnNumbers.ToString(new[] { CVec4.X, CVec4.Y, CVec4.Z }),
                        ScnNumbers.ToString(new[] { Point4.X, Point4.Y, Point4.Z, Roll4 }),
                        ScnNumbers.ToString(Radius2)
                    );
            
            if (visiblePart != null) text += "\r\n" + visiblePart;
            text += "\r\n" + trackPart;
            if (switchPart != null) text += "\r\n" + switchPart;
            if (TrackLength2 != null) text += String.Format("\r\ntracklength {0}", ScnNumbers.ToString(TrackLength2));
            if (Velocity != null) text += String.Format("\r\nvelocity {0}", ScnNumbers.ToString(Velocity));
            foreach (var e in Event0) text += String.Format("\r\nevent0 {0}", e);
            foreach (var e in Event1) text += String.Format("\r\nevent1 {0}", e);
            foreach (var e in Event2) text += String.Format("\r\nevent2 {0}", e);
            foreach (var e in Eventall0) text += String.Format("\r\neventall0 {0}", e);
            foreach (var e in Eventall1) text += String.Format("\r\neventall1 {0}", e);
            foreach (var e in Eventall2) text += String.Format("\r\neventall2 {0}", e);
            foreach (var e in Isolated) text += String.Format("\r\nisolated {0}", e);
            if (Extras != null) text += "\r\n" + Extras;
            text += "\r\nendtrack";
            if (IncludesAfter != null) text += "\r\n" + IncludesAfter;
            return text;
        }

    }

    /// <summary>
    /// Scenery track list
    /// </summary>
    public class ScnTrackCollection : ScnVectorObjects<ScnTrack> {

        #region Private

        #region Regular expressions

        private const string PatComment = @"\s*//.*$";
        private const string PatXvs = @"(?:\r?\n){3,}";
        private const string PatIncludeBefore = @"(?:(include[^\r\n]+end)[ \t;]*\r?\n)?";
        private const string PatIncludeAfter = @"(?:\r?\n[ \t;]*(include[^\r\n]+end))?";
        private static Regex RxComment = new Regex(PatComment, RegexOptions.Compiled | RegexOptions.Multiline);
        private static Regex RxXvs = new Regex(PatXvs, RegexOptions.Compiled);

        #endregion

        private ScnTrackIncludes SourceIncludes;
        private int[][] SourceFragments;

        private void Parse() {
            try {
                var lexer = new ScnNodeLexer(Source, "track");
                if (lexer.Nodes != null)
                    foreach (var node in lexer.Nodes)
                        Add(new ScnTrack(SourcePath, node));
            } catch (Exception x) {
                System.Windows.Forms.MessageBox.Show(x.Message + "\r\n\r\n" + x.StackTrace);
            }
        }

        private void GetSourceFragments() {
            if (Count < 1) return;
            int a = 0, b = 0, c = 0, d = 0;
            SourceFragments = new int[Count][];
            SourceFragments[0] = new int[] { 0, this[0].SourceIndex };
            for (int i = 0, ct = Count; i < ct; i++) {
                if (i == 0) {
                    c = this[i].SourceIndex;
                    d = this[i].SourceLength;
                    a = c + d;
                    continue;
                }
                b = this[i].SourceIndex;
                SourceFragments[i] = new int[] { a, b - a };
                a = b + this[i].SourceLength;
            }
            a = this[Count - 1].SourceIndex;
            b = a + this[Count - 1].SourceLength;
            SourceFragments[Count - 1] = new int[] { b, Source.Length - 1 - b };
        }

        #endregion

        public string Source;
        public string SourcePath;

        public static ScnTrackCollection Parse(string text, string path = null, ScnTrackIncludes includes = ScnTrackIncludes.Before) {
            var tracks = new ScnTrackCollection();
            tracks.Source = text;
            tracks.SourcePath = path;
            tracks.SourceIncludes = includes;
            tracks.Parse();
            tracks.GetSourceFragments();
            return tracks;
        }

        public static ScnTrackCollection Load() {
            var tracks = new ScnTrackCollection();
            ProjectFile.MainFiles.ForEach(f => {
                if (f.Type == ProjectFile.Types.SceneryPart || f.Type == ProjectFile.Types.SceneryMain) {
                    var set = ScnTrackCollection.Parse(f.Text, f.Path, ScnTrackIncludes.Ignore);
                    if (set.Count > 0) tracks.AddRange(set);
                }
            });
            return tracks;
        }

        /// <summary>
        /// Gets a rectangle containing all tracks
        /// </summary>
        /// <returns></returns>
        public RectangleF GetBounds() {
            double x = 0, left = 0, top = 0, right = 0, bottom = 0;
            foreach (var track in this) {
                if ((x = track.Point1.X) < left) left = x;
                if ((x = track.Point2.X) < left) left = x;
                if ((x = track.Point1.X) > right) right = x;
                if ((x = track.Point2.X) > right) right = x;
                if ((x = track.Point1.Z) < top) top = x;
                if ((x = track.Point2.Z) < top) top = x;
                if ((x = track.Point1.Z) > bottom) bottom = x;
                if ((x = track.Point2.Z) > bottom) bottom = x;
                if (track.IsSwitch) {
                    if ((x = track.Point3.X) < left) left = x;
                    if ((x = track.Point4.X) < left) left = x;
                    if ((x = track.Point3.X) > right) right = x;
                    if ((x = track.Point4.X) > right) right = x;
                    if ((x = track.Point3.Z) < top) top = x;
                    if ((x = track.Point4.Z) < top) top = x;
                    if ((x = track.Point3.Z) > bottom) bottom = x;
                    if ((x = track.Point4.Z) > bottom) bottom = x;
                }
            }
            return new RectangleF((float)left, (float)top, (float)(right - left), (float)(bottom - top));
        }

        public float GetFitScale(RectangleF mapBounds, Rectangle displayBounds) {
            var x = (float)displayBounds.Width / mapBounds.Width;
            var y = (float)displayBounds.Height / mapBounds.Height;
            return new[] { x, y }.Min();
        }

        public ScnTrackCollection GetVisible(float scale, RectangleF viewport) {
            var visible = new ScnTrackCollection();
            return visible;
        }

        /// <summary>
        /// Sorts tracks as linked list
        /// </summary>
        public new void Sort() {
            var q = new Queue<ScnTrack>(this);
            var l = new LinkedList<ScnTrack>();
            while (q.Count > 0) {
                var t = q.Dequeue();
                if (l.Count < 1) {
                    l.AddFirst(t);
                    continue;
                }
                var added = false;
                var current = l.First;
                var switchEnd = false;
                while (current != null) {
                    if (t.IsEndLinkedTo(current.Value, out switchEnd)) {
                        l.AddBefore(current, t);
                        current = current.Next;
                        added = true;
                        break;
                    }
                    if (t.IsStartLinkedTo(current.Value, out switchEnd)) {
                        l.AddAfter(current, t);
                        current = current.Next;
                        added = true;
                        break;
                    }
                    current = current.Next;
                }
                if (!added) l.AddLast(t);
            }
            Clear();
            AddRange(l);
        }

        /// <summary>
        /// Add neighbour tracks to each others
        /// </summary>
        public void FindAndSetNeighbours() {
            foreach (var p in this) {
                foreach (var n in this) {
                    if (n != p) p.SetNeighbourTrack(n); // check and connect neighbours for t track
                }
            }
        }

        /// <summary>
        /// Adds unique and meaningful names for unnamed tracks
        /// </summary>
        public void AddNames() {
            int trackIndex = 0, switchIndex = 0, distIndex = 0;
            string baseName = null;
            string prevBaseName = null;
            ScnTrack prev = null;
            this.ForEach(track => {
                bool isSwitch = track.Point3 != null;
                bool isPrevSwitchEnd = false;
                bool isLinkedToPrev = track.IsLinkedTo(prev, out isPrevSwitchEnd);
                bool isNamed = track.Name != null && track.Name != "none";
                int prevLength = prev != null ? (int)Math.Round(prev.GetLength(isPrevSwitchEnd ? 1 : 0)) : 0;
                if (!isLinkedToPrev) {
                    if (isSwitch) switchIndex++; else trackIndex++;
                }
                baseName = isNamed ? track.Name : (isSwitch ? ("s" + switchIndex.ToString()) : ("t" + trackIndex.ToString()));
                if (isLinkedToPrev) {
                    distIndex += prevLength;
                    if (!isNamed && distIndex > 0) track.Name = prevBaseName + "_" + distIndex.ToString();
                } else {
                    distIndex = 0;
                    if (!isNamed) track.Name = baseName;
                }
                prev = track;
                if (!isLinkedToPrev || isNamed) prevBaseName = baseName;
            });
        }

        /// <summary>
        /// Adds unique and meaningful names for unnamed tracks, but first sorts tracks as linked list
        /// </summary>
        public void SortAddNames() {
            Sort();
            AddNames();
        }

        public string ReplaceText() {
            if (Count < 1) return Source;
            var b = new StringBuilder();
            foreach (var f in SourceFragments) if (f[1] > 0) b.Append(Source.Substring(f[0], f[1]));
            var oc = RxXvs.Replace(b.ToString(), "\r\n\r\n").Trim();
            return Source = (
                oc.Length > 0
                    ? ("// Tracks:\r\n\r\n" + AsText() + "\r\n\r\n// Original content:\r\n\r\n" + oc)
                    : AsText()
            );
        }

        /// <summary>
        /// Returns scenery track list as text
        /// </summary>
        /// <returns></returns>
        public string AsText() {
            return String.Join("\r\n\r\n", this.ConvertAll<string>(i => i.AsText()));
        }

    }

    public enum ScnTrackIncludes {
        Ignore, Before, After
    }

}
