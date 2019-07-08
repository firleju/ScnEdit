using FastColoredTextBoxNS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Trax {

    internal class ProjectFile {

        #region Constants and enumerations

        private const string _SceneryDirectoryName = "scenery";

        internal enum Types { Text, SceneryMain, SceneryPart, Timetable, HTML, CSS }

        internal enum Roles { Any, Main, Include, Timetable, Description, Log }

        #endregion

        #region Properties

        internal List<ProjectFile> References { get { return _references; } set { _references = value; } }

        #endregion

        #region Fields

        internal Types Type;
        internal Roles Role;
        internal string BaseDirectory;
        internal string SceneryDirectory;
        internal string Path;
        internal string Directory;
        internal string FileName;
        internal string RelativeDirectory;
        internal List<string> FileParams;
        internal bool IsConverted;
        internal bool IsNormalized;
        internal bool IsChanged;
        internal V3D Rotoation;

        public string TextCache;
        internal static Dictionary<string, string> TextCacheDictionary = new Dictionary<string, string>();
        private static List<string> FilesWithoutEvents = new List<string>();
        private static List<string> FilesWithoutMemCells = new List<string>();

        internal static List<ProjectFile> AllFiles = new List<ProjectFile>();
        internal static List<ProjectFile> MainFiles = new List<ProjectFile>();
        private List<ProjectFile> _references;
        internal static Dictionary<string, ScnEvent> EventCollection = new Dictionary<string, ScnEvent>();
        internal static Dictionary<string, ScnMemCell> MemCellCollection = new Dictionary<string, ScnMemCell>();

        private static readonly IFormatProvider FP = System.Globalization.CultureInfo.InvariantCulture; // needed to have C default decimal separator

        #endregion

        #region Properties

        internal static ProjectFile Project {
            get {
                return ProjectFile.MainFiles.Find(i => i.Role == Roles.Main);
            }
        }

        internal static List<ProjectFile> ProjectFiles {
            get { return ProjectFile.AllFiles; }
        }

        internal bool IsOpen { get { return this is EditorFile; } }
        internal bool HasHtmlEncoding { get { return Type == Types.HTML || Type == Types.CSS; } }

        /// <summary>
        /// Encoding set default for current file type
        /// </summary>
        internal Encoding EncodingDefault {
            get {
                if (_EncodingDefault != null) return _EncodingDefault;
                var settings = Properties.Settings.Default;
                return _EncodingDefault = Encoding.GetEncoding(HasHtmlEncoding ? settings.HtmlEncodingDefault : settings.EncodingDefault);
            }
        }

        internal bool AutoDecoding {
            get {
                if (_AutoDecodingSet) return _AutoDecoding;
                _AutoDecodingSet = true;
                return _AutoDecoding = Properties.Settings.Default.AutoDecoding;
            }
        }

        internal virtual string Text {
            get {
                if (TextCache == null) {
                    if (TextCacheDictionary.ContainsKey(Path)) {
                        return TextCache = TextCacheDictionary[Path];
                    }
                    else {
                        using (var stream = File.Open(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                            var buffer = new byte[stream.Length];
                            stream.Read(buffer, 0, (int)stream.Length);
                            stream.Close();
                            return TextCache = TextCacheDictionary[Path] = PreNormalize(
                                (AutoDecoding && !HasHtmlEncoding)
                                    ? AutoDecode(EncodingDefault, buffer, out IsConverted)
                                    : EncodingDefault.GetString(buffer)
                            );
                        }
                    }
                }
                else return TextCache;
            }
        }

        internal Editor Editor {
            get {
                var editorFile = EditorFile.MainFiles.FirstOrDefault(i => i.Path == this.Path);
                if (editorFile == null) Open();
                editorFile = EditorFile.MainFiles.First(i => i.Path == this.Path);
                if (editorFile == null) return null;
                return editorFile.Editor;
            }
        }

        #endregion

        #region Events

        internal event EventHandler ReferencesResolved;

        #endregion

        #region Methods

        /// <summary>
        /// Creates new project file
        /// </summary>
        /// <param name="path">File system path</param>
        /// <param name="role">Project role</param>
        internal ProjectFile(string path, Roles role = Roles.Any, string par = "") {
            Role = role;
            Path = path.Replace('/', '\\');
            Type = GetFileType(Path);
            char[] delim = { ' ', '\t', ';', '\r', '\n' };
            FileParams = par.Split(delim).ToList();
            if (Role == Roles.Timetable) Type = Types.Timetable;
            if (Type == Types.SceneryMain) Role = Roles.Main;
            var sceneryIndex = path.IndexOf(Properties.Settings.Default.SceneryDirectory, StringComparison.InvariantCultureIgnoreCase);
            var sceneryLength = _SceneryDirectoryName.Length;
            Directory = System.IO.Path.GetDirectoryName(Path);
            FileName = System.IO.Path.GetFileName(Path);
            if (sceneryIndex >= 0 && (Role == Roles.Include || Role == Roles.Timetable || Type == Types.SceneryMain || Type == Types.SceneryPart)) {
                BaseDirectory = Path.Substring(0, sceneryIndex).Trim(new[] { '\\' });
                SceneryDirectory = BaseDirectory + "\\" + Path.Substring(sceneryIndex, sceneryLength);
                RelativeDirectory = Directory.Replace(SceneryDirectory, "");
            }
            //if (MainFiles != null && MainFiles.Exists(i => i.Path == Path)) return;
            if (Type == Types.SceneryMain && Role == Roles.Main) GetScenery(this);
            //else {
            //    if (MainFiles == null) MainFiles = new List<ProjectFile>();
            //    MainFiles.Add(this);
            //}
            var input = !Role.HasFlag(Roles.Description) ? new ScnSyntax.Comment().Replace(Text, "") : Text;
            Rotoation = new V3D();
            var m = new ScnSyntax.Rotate().Matches(input);
            if (m.Count > 0) {
                var s = Tools.ChangeParamsToText(m[0].Value, FileParams);
                var l = s.Split(delim).ToList();
                for (int i = 0; i < l.Count; i++) {
                    double d;
                    if (double.TryParse(l[i], System.Globalization.NumberStyles.Number, FP,out d)) {
                        switch (i) {
                            case 0: Rotoation.X = d; break;
                            case 1: Rotoation.Y = d; break;
                            case 2: Rotoation.Z = d; break;
                        }
                    }
                }
            }
        }
    
        /// <summary>
        /// Opens file in editor
        /// </summary>
        internal void Open() { new EditorFile(this); }

        /// <summary>
        /// Closes the current project
        /// </summary>
        internal static void CloseProject() {
            if (Main.Instance.TrackMap != null) {
                Main.Instance.TrackMap.Close();
                Main.Instance.TrackMap.Dispose();
                Main.Instance.TrackMap = null;
            }
            if (Main.Instance.SceneryPanel != null) {
                Main.Instance.SceneryPanel.Close();
                Main.Instance.SceneryPanel.Dispose();
                Main.Instance.SceneryPanel = null;
            }
            Status.FileName = null;
            Status.Visible = false;
            EditorFile.Reset();
            Main.Instance.DockPanel.Refresh();           
        }

        internal static void FindText(string text, bool useRegex = false) {
            var pattern = useRegex ? text : Regex.Escape(text);
            try { FindAll(new Regex(pattern, RegexOptions.Compiled)); } catch (ArgumentException) { }
        }
        
        internal static void FindSymbol(string symbol) {
            var pattern = @"(?<=^|[ :;\r\n]+)" + Regex.Escape(symbol) + "(?=[_ ;\r\n]+|$)";
            FindAll(new Regex(pattern, RegexOptions.Compiled));
        }

        internal static void ReplaceSymbol(string symbol, string replaceText) {
            var pattern = @"(?<=^|[ :;\r\n]+)" + Regex.Escape(symbol) + "(?=[_ ;\r\n]+|$)";
            ReplaceAll(new Regex(pattern, RegexOptions.Compiled), replaceText);
        }

        private static void ProcessAll(Action<ProjectFile> action) {
            var files = ProjectFiles;
            for (int i = 0, n = files.Count; i < n; i++) action(files[i]);
        }

        private static void FindAll(Regex regex) {
            Main.Instance.DisableEdit();
            Status.Text = Messages.Searching;
            Application.DoEvents();
            SearchResultsPanel.Reset();
            ProcessAll(i => {
                foreach (Match m in regex.Matches(i.Text)) {
                    i.Open();
                    var p = i.Editor.PositionToPlace(m.Index);// i.Editor.MarkSearchResult(m.Index, m.Length);
                    SearchResultsPanel.Add(new SearchResult {
                        Path = i.Path, File = i.FileName, Fragment = m.Value, Line = p.iLine + 1, Column = p.iChar + 1
                    });
                }
            });
            SearchResultsPanel.CloseIfEmpty();
            Status.Text = Messages.Ready;
            Application.DoEvents();
            Main.Instance.EnableEdit();
        }

        public static void FindTrack(ScnTrack track) {
            Main.Instance.DisableEdit();
            Status.Text = Messages.Searching;
            Application.DoEvents();
            SearchResultsPanel.Reset();
            ProcessAll(i => {
                if (i.Path == track.SourcePath) {
                    i.Open();
                    var p = i.Editor.PositionToPlace(track.SourceIndex);
                    SearchResultsPanel.Add(new SearchResult {
                        Path = i.Path, File = i.FileName, Fragment = i.Text.Substring(track.SourceIndex, track.SourceLength), Line = p.iLine + 1, Column = 1
                    });
                }
            });
            SearchResultsPanel.CloseIfEmpty();
            Status.Text = Messages.Ready;
            Application.DoEvents();
            Main.Instance.EnableEdit();
        }

        public static void FindTracks(IEnumerable<ScnTrack> tracks) {
            Main.Instance.DisableEdit();
            Status.Text = Messages.Searching;
            Application.DoEvents();
            SearchResultsPanel.Reset();
            ProcessAll(i => {
                foreach (var track in tracks) {
                    if (i.Path == track.SourcePath) {
                        i.Open();
                        var p = i.Editor.PositionToPlace(track.SourceIndex);
                        SearchResultsPanel.Add(new SearchResult {
                            Path = i.Path, File = i.FileName, Fragment = i.Text.Substring(track.SourceIndex, track.SourceLength), Line = p.iLine + 1, Column = 1
                        });
                    }
                }
            });
            SearchResultsPanel.CloseIfEmpty();
            Status.Text = Messages.Ready;
            Application.DoEvents();
            Main.Instance.EnableEdit();
        }

        /// <summary>
        /// Replaces all regex matches in all project files with replaceText
        /// Results are added to results panel and highlighted
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="replaceText"></param>
        private static void ReplaceAll(Regex regex, string replaceText) {
            Main.Instance.DisableEdit();
            Status.Text = Messages.Replacing;
            Application.DoEvents();
            SearchResultsPanel.Reset();
            ProcessAll(i => {
                string t;
                Match m;
                Place p;
                do {
                    m = regex.Match(t = i.Text);
                    if (m.Success) {
                        p = i.Editor.PositionToPlace(m.Index);
                        t = t.Substring(0, m.Index) + replaceText + t.Substring(m.Index + m.Length);
                        i.Editor.BeginAutoUndo();
                        i.Editor.Text = t;
                        i.Editor.EndAutoUndo();
                        SearchResultsPanel.Add(new SearchResult {
                            Path = i.Path, File = i.FileName, Fragment = m.Value, Replacement = replaceText, Line = p.iLine + 1, Column = p.iChar + 1
                        });
                    }
                } while (m.Success);
            });
            SearchResultsPanel.CloseIfEmpty();
            Status.Text = Messages.Ready;
            Application.DoEvents();
            Main.Instance.EnableEdit();
        }

        /// <summary>
        /// Returns file normalized text if applicable, original text otherwise
        /// </summary>
        /// <returns></returns>
        internal string Normalize() {
            var t = this is EditorFile ? (this as EditorFile).Text : (this as ProjectFile).Text;
            if (Type == Types.SceneryMain || Type == Types.SceneryPart) {
                t = new ScnSyntax.HWhiteSpace().Replace(t, " ");
                t = new ScnSyntax.VWhiteSpace().Replace(t, "\r\n");
                t = new ScnSyntax.LineEnd().Replace(t, "");
                t = new ScnSyntax.XVWhiteSpace().Replace(t, "\r\n\r\n");
                t = new ScnSyntax.ExplicitTexExt().Replace(t, "");
                var lines = new ScnSyntax.VWhiteSpace().Split(t);
                var count = lines.Length / 2;
                string[] even = new string[count], odd = new string[count];
                for (int i = 0; i < count; i++) { even[i] = lines[2 * i]; odd[i] = lines[2 * i + 1]; }
                var interleaved = false;
                interleaved |= even.All(i => String.IsNullOrWhiteSpace(i));
                interleaved |= odd.All(i => String.IsNullOrWhiteSpace(i));
                if (interleaved) t = new ScnSyntax.LineInterleave().Replace(t, "\r\n");
                return t.Trim();
            } else return t;
        }

        /// <summary>
        /// Gets all file references associated with main project file
        /// </summary>
        internal void GetReferences() {
            References = new List<ProjectFile>();
            //var w = new BackgroundWorker();
            //w.DoWork += new DoWorkEventHandler((s, e) => {
                GetReferences(Roles.Include, new ScnSyntax.IncludeSimple(), ref _references);
                GetReferences(Roles.Timetable, new ScnSyntax.Timetable(), ref _references, false, new[] { "none", "rozklad" }, null, ".txt");
                GetReferences(Roles.Description, new ScnSyntax.CommandInclude(), ref _references, false, null, new[] { ".txt", ".html" });
            MainFiles.AddRange(References);
            //});
            //if (Role == Roles.Main)
            //    w.RunWorkerCompleted += new RunWorkerCompletedEventHandler((s, e) => {
            //        if (ReferencesResolved != null) ReferencesResolved.Invoke(this, EventArgs.Empty);
            //    });
            //w.RunWorkerAsync();
            //w.Dispose();
        }

        internal void Invoke() {
            ReferencesResolved.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Gets all file references associated with main project file
        /// </summary>
        internal void GetReferencesMain() {
            GetReferences(Roles.Main, new ScnSyntax.IncludeSimple(), ref MainFiles);
        }

        /// <summary>
        /// Gets all events with their params in all included files (such as semaphor inc)
        /// </summary>
        internal void GetReferencesAll() {
            GetReferences(Roles.Include, new ScnSyntax.IncludeWithParams(), ref AllFiles, true);
        }

        internal void GetMemCells() {
            var input = !Role.HasFlag(Roles.Description) ? new ScnSyntax.Comment().Replace(Text, "") : Text;

            var t = ScnMemCellCollection.Parse(ref input, FileParams, Rotoation);
            if (t.Count == 0) FilesWithoutMemCells.Add(Path);
            t.ToList().ForEach(x => MemCellCollection[x.Key] = x.Value);
        }

        internal void GetEvents() {
            var input = !Role.HasFlag(Roles.Description) ? new ScnSyntax.Comment().Replace(Text, "") : Text;
            var has_events = false;
            foreach (Match m in new ScnSyntax.EventParams().Matches(input)) {
                has_events = true;
                var e = new ScnEvent(m.Value, FileParams);
                EventCollection.Add(e.Name, e);
                //log.Trace("Type: {0}, Name: {1}", e.Type, e.Name);
            }
            if (!has_events) FilesWithoutEvents.Add(Path);
        }

        /// <summary>
        /// Opens all project's references
        /// </summary>
        internal void OpenReferences() { MainFiles.ForEach(i => i.Open()); }

        #endregion

        #region Private

        private static bool _AutoDecoding;
        private static bool _AutoDecodingSet;
        private Encoding _EncodingDefault;

        private string PreNormalize(string text) {
            int initialLength = text.Length;
            var tabString = "".PadLeft(4);
            text = new ScnSyntax.LineEnding().Replace(text, "\r\n");
            text = new ScnSyntax.Tab().Replace(text, tabString);
            IsNormalized = text.Length != initialLength;
            return text;
        }

        private static string AutoDecode(Encoding nonUnicodeDefault, byte[] buffer, out bool u) {
            string s;
            var u8 = new UTF8Encoding(false, true);
            try {
                s = u8.GetString(buffer);
                u = s.Length < buffer.Length;
                return s;
            } catch (DecoderFallbackException) {
                u = false;
                return nonUnicodeDefault.GetString(buffer);
            }
        }

        private static void GetScenery(ProjectFile f) {
            if (f.Role != Roles.Main) throw new InvalidOperationException("Main scenery file expected.");
            if (Main.Instance.TrackMap != null) {
                Main.Instance.TrackMap.Close();
                Main.Instance.TrackMap.Dispose();
                Main.Instance.TrackMap = null;
            }
            MainFiles = new List<ProjectFile>();
            MainFiles.Add(f);
        }

        private Types GetFileType(string file) {
            var ext = System.IO.Path.GetExtension(file).ToLower();
            if (ext.Length > 0) ext = ext.Substring(1);
            if (ext == Properties.Settings.Default.SceneryMainExtension) return Types.SceneryMain;
            var parts = Regex.Split(Properties.Settings.Default.SceneryPartsExtensions.ToLower(), @"[ ,;\|]");
            if (parts.Any(p => ext == p)) return Types.SceneryPart;
            switch (ext) {
                case "html": return Types.HTML;
                case "css": return Types.CSS;
            }
            return Types.Text;
        }

        private void GetReferences(Roles role, Regex r, ref List<ProjectFile> ref_list, bool allow_duplicates = false,
            string[] ignore = null, string[] allow = null, string defaultExt = null) { 
            var input = !Role.HasFlag(Roles.Description) ? new ScnSyntax.Comment().Replace(Text, "") : Text;
            foreach (Match m in r.Matches(input)) {
                if (ignore != null && ignore.Contains(m.Value)) continue;
                char[] delim = { ' ', '\t', ';', '\r', '\n' };
                var list = m.Value.Split(delim, 2);
                string file = list[0];

                var path = ((role == Roles.Description ? BaseDirectory : SceneryDirectory) + "\\" + file).Replace('/', '\\');
                var ext = System.IO.Path.GetExtension(file);
                if (defaultExt != null && ext == "") { ext = defaultExt; path += ext; }
                if (!FilesWithoutEvents.Contains(path) && !FilesWithoutMemCells.Contains(path) && System.IO.File.Exists(path)) {
                    if (allow != null && !allow.Contains(ext)) continue;
                    if (!ref_list.Any(p => p.Path == path) || allow_duplicates) {
                        var reference = new ProjectFile(path, role, list.Length == 2 ? list[1] : "");
                        ref_list.Add(reference);
                        reference.GetMemCells();
                        reference.GetEvents();
                        //reference.GetReferences(role,r,ref ref_list,allow_duplicates,ignore,allow,defaultExt);
                    }
                }
            }
        }

        #endregion

    }

    internal class ProjectLocation {
        
        public string Path;
        public int Index;
        public int Length;

        public static ProjectLocation FromRange(Range r) {
            var e = r.tb as Editor;
            var p = e.File.Path;
            var i = e.PlaceToPosition(r.Start);
            var l = e.PlaceToPosition(r.End) - i;
            return new ProjectLocation() { Path = p, Index = i, Length = l };
        }

    }

}
