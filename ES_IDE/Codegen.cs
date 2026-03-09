using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EventScriptIDE
{
    /// <summary>
    /// Generates VB.NET source code from a ProjectModel.
    /// </summary>
    public static class CodeGen
    {
        // -- String helpers ----------------------------------------------------

        public static string VbStr(string val)
        {
            val = (val ?? "").Trim();
            if (string.IsNullOrEmpty(val)) return "\"\"";
            if (val.StartsWith("\"")) return val;
            if (val.StartsWith("{") && val.EndsWith("}")) return val.Substring(1, val.Length - 2);
            return "\"" + val.Replace("\"", "\"\"") + "\"";
        }

        static string Sp(List<ParamDef> defs, Dictionary<string, string> parms, string key)
        {
            string val;
            if (!parms.TryGetValue(key, out val)) val = "";
            foreach (var pd in defs)
            {
                if (pd.Name == key && pd.Kind == "str") return VbStr(val);
            }
            return val;
        }

        public static string ExtSub(string template, Dictionary<string, string> parms,
                                    List<ParamDef> paramDefs = null)
        {
            if (paramDefs == null) paramDefs = new List<ParamDef>();
            var used = new HashSet<string>();
            foreach (var pd in paramDefs)
            {
                string v;
                if (!parms.TryGetValue(pd.Name, out v)) continue;
                if (pd.Kind == "str") v = VbStr(v);
                template = template.Replace("%" + pd.Name + "%", v);
                used.Add(pd.Name);
            }
            foreach (var kvp in parms)
            {
                if (!used.Contains(kvp.Key))
                    template = template.Replace("%" + kvp.Key + "%", kvp.Value);
            }
            return template;
        }

        // -- Condition → VB.NET ------------------------------------------------

        public static ConditionResult ConditionToVb(ItemDefinition cond)
        {
            var cat = cond.Category;
            var act = cond.Action;
            var p   = cond.Params;

            // Extension condition?
            Dictionary<string, ExtConditionDef> catDefs;
            ExtConditionDef edef;
            if (ExtensionRegistry.Conditions.TryGetValue(cat, out catDefs) && catDefs.TryGetValue(act, out edef))
            {
                return new ConditionResult
                {
                    IsExt   = true,
                    Prep    = ExtSub(edef.Prep,    p, edef.Params).Trim(),
                    IfLine  = ExtSub(edef.IfLine,  p, edef.Params),
                    EndLine = ExtSub(edef.EndLine, p, edef.Params),
                };
            }

            string expr = "True";
            string tmp;
            if (cat == "Variable")
            {
                string op = "=";
                switch (act)
                {
                    case "Equals":          op = "=";  break;
                    case "Not Equals":      op = "<>"; break;
                    case "Greater Than":    op = ">";  break;
                    case "Less Than":       op = "<";  break;
                    case "Greater Or Equal":op = ">="; break;
                    case "Less Or Equal":   op = "<="; break;
                }
                p.TryGetValue("var",   out var varVal);
                p.TryGetValue("value", out var valVal);
                expr = (varVal ?? "") + " " + op + " " + (valVal ?? "");
            }
            else if (cat == "String")
            {
                p.TryGetValue("expr", out var e);
                e = e ?? "";
                switch (act)
                {
                    case "Contains":   expr = e + ".Contains(" + p.GetOrDefault("substr", "") + ")"; break;
                    case "Starts With":expr = e + ".StartsWith(" + p.GetOrDefault("prefix", "") + ")"; break;
                    case "Ends With":  expr = e + ".EndsWith(" + p.GetOrDefault("suffix", "") + ")"; break;
                    case "Is Empty":   expr = "String.IsNullOrEmpty(" + e + ")"; break;
                    case "Not Empty":  expr = "Not String.IsNullOrEmpty(" + e + ")"; break;
                }
            }
            else if (cat == "FS")
            {
                p.TryGetValue("path", out tmp);
                tmp = tmp ?? "";
                switch (act)
                {
                    case "File Exists":      expr = "File.Exists(" + tmp + ")"; break;
                    case "File Not Exists":  expr = "Not File.Exists(" + tmp + ")"; break;
                    case "Directory Exists": expr = "Directory.Exists(" + tmp + ")"; break;
                }
            }
            else if (cat == "OS" && act == "Env Var Equals")
            {
                expr = "Environment.GetEnvironmentVariable(" + p.GetOrDefault("var", "") + ") = " + p.GetOrDefault("value", "");
            }
            else if (cat == "Custom")
            {
                p.TryGetValue("expr", out tmp);
                expr = string.IsNullOrEmpty(tmp) ? "True" : tmp;
            }

            return new ConditionResult { IsExt = false, Expr = expr };
        }

        // -- Action → VB.NET lines ---------------------------------------------

        public static List<string> ActionToVb(ItemDefinition action, string indent)
        {
            var lines = new List<string>();
            var cat   = action.Category;
            var act   = action.Action;
            var p     = action.Params;

            List<ParamDef> defs;
            Dictionary<string, List<ParamDef>> catDict;
            if (!BuiltinDefinitions.Actions.TryGetValue(cat, out catDict)) catDict = new Dictionary<string, List<ParamDef>>();
            if (!catDict.TryGetValue(act, out defs)) defs = new List<ParamDef>();

            Action<string> L = s => lines.Add(indent + s);
            Func<string, string> G = key =>
            {
                string v; p.TryGetValue(key, out v);
                return !string.IsNullOrEmpty(v) ? Sp(defs, p, key) : "";
            };
            Func<string, string, string> R = (key, def) => { string v; return p.TryGetValue(key, out v) ? v : def; };

            switch (cat)
            {
                case "Variable":
                    var varN = R("var",""); var valN = R("value","");
                    switch (act)
                    {
                        case "Set Variable":            L(varN + " = " + valN);  break;
                        case "Add To Variable":         L(varN + " += " + valN); break;
                        case "Subtract From Variable":  L(varN + " -= " + valN); break;
                        case "Multiply Variable":       L(varN + " *= " + valN); break;
                        case "Divide Variable":         L(varN + " /= " + valN); break;
                    }
                    break;

                case "UI":
                    var ctrl = R("control","");
                    switch (act)
                    {
                        case "Set Control Text":
                            L(ctrl + ".Text = " + G("text")); break;
                        case "Get Control Text":
                            L(R("var","") + " = " + ctrl + ".Text"); break;
                        case "Show MessageBox":
                            L("MessageBox.Show(" + G("msg") + ", " + G("title") + ")"); break;
                        case "Show MessageBox (YesNo)":
                            L(R("var","") + " = (MessageBox.Show(" + G("msg") + ", " + G("title") + ", MessageBoxButtons.YesNo) = DialogResult.Yes)"); break;
                        case "Set Form Title":
                            L("Me.Text = " + G("title")); break;
                        case "Set Form Size":
                            L("Me.Size = New System.Drawing.Size(" + R("w","") + ", " + R("h","") + ")"); break;
                        case "Set Control Visible":
                            L(ctrl + ".Visible = " + R("visible","True")); break;
                        case "Set Control Enabled":
                            L(ctrl + ".Enabled = " + R("enabled","True")); break;
                        case "Set Control Color":
                            L(ctrl + ".BackColor = " + R("color","Color.White")); break;
                        case "Add ListBox Item":
                            L(ctrl + ".Items.Add(" + G("item") + ")"); break;
                        case "Clear ListBox":
                            L(ctrl + ".Items.Clear()"); break;
                        case "Get ListBox Selection":
                            L(R("var","") + " = " + ctrl + ".SelectedItem?.ToString()"); break;
                        case "Focus Control":
                            L(ctrl + ".Focus()"); break;
                        case "Close Form":
                            L("Me.Close()"); break;
                        case "Open File Dialog":
                            var ofilt = !string.IsNullOrEmpty(R("filter","")) ? G("filter") : "\"All files|*.*\"";
                            L("Dim _ofd As New OpenFileDialog()");
                            L("_ofd.Filter = " + ofilt);
                            L("If _ofd.ShowDialog() = DialogResult.OK Then");
                            L("\t" + R("var","") + " = _ofd.FileName");
                            L("End If");
                            break;
                        case "Save File Dialog":
                            var sfilt = !string.IsNullOrEmpty(R("filter","")) ? G("filter") : "\"All files|*.*\"";
                            L("Dim _sfd As New SaveFileDialog()");
                            L("_sfd.Filter = " + sfilt);
                            L("If _sfd.ShowDialog() = DialogResult.OK Then");
                            L("\t" + R("var","") + " = _sfd.FileName");
                            L("End If");
                            break;
                    }
                    break;

                case "Math":
                    var mVar = R("var",""); var mExpr = R("expr","");
                    switch (act)
                    {
                        case "Set To Expression": L(mVar + " = " + mExpr); break;
                        case "Set To Random": L(mVar + " = CInt(Math.Floor(New Random().NextDouble() * (" + R("max","") + " - " + R("min","") + ") + " + R("min","") + "))"); break;
                        case "Round":  L(mVar + " = Math.Round(" + mVar + ", " + R("digits","0") + ")"); break;
                        case "Abs":    L(mVar + " = Math.Abs(" + mExpr + ")"); break;
                        case "Sqrt":   L(mVar + " = Math.Sqrt(" + mExpr + ")"); break;
                        case "Power":  L(mVar + " = Math.Pow(" + R("base","") + ", " + R("exp","") + ")"); break;
                        case "Min Of Two": L(mVar + " = Math.Min(" + R("a","") + ", " + R("b","") + ")"); break;
                        case "Max Of Two": L(mVar + " = Math.Max(" + R("a","") + ", " + R("b","") + ")"); break;
                    }
                    break;

                case "String":
                    var sVar = R("var",""); var sOut = R("out",""); var sExpr = R("expr","");
                    switch (act)
                    {
                        case "Concatenate":    L(sVar + " = " + R("a","") + " & " + R("b","")); break;
                        case "Replace":        L(sVar + " = " + sVar + ".Replace(" + G("old") + ", " + G("new") + ")"); break;
                        case "To Uppercase":   L(sVar + " = " + sVar + ".ToUpper()"); break;
                        case "To Lowercase":   L(sVar + " = " + sVar + ".ToLower()"); break;
                        case "Trim":           L(sVar + " = " + sVar + ".Trim()"); break;
                        case "Get Length":     L(sOut + " = " + sExpr + ".Length"); break;
                        case "Substring":      L(sOut + " = " + sExpr + ".Substring(" + R("start","") + ", " + R("length","") + ")"); break;
                        case "Split":          L(sOut + " = " + sExpr + ".Split(CChar(" + G("sep") + "))"); break;
                        case "Index Of":       L(sOut + " = " + sExpr + ".IndexOf(" + G("search") + ")"); break;
                        case "Format Number":  L(sOut + " = " + sExpr + ".ToString(" + G("fmt") + ")"); break;
                    }
                    break;

                case "Win32":
                    switch (act)
                    {
                        case "MessageBox API":      L("MessageBoxW(IntPtr.Zero, " + G("msg") + ", " + G("title") + ", " + R("flags","0") + ")"); break;
                        case "FindWindow":           L(R("out","") + " = FindWindow(" + R("class_name","Nothing") + ", " + G("title") + ")"); break;
                        case "SendMessage":          L("SendMessage(" + R("hwnd","") + ", " + R("msg_code","") + ", New IntPtr(" + R("wparam","0") + "), New IntPtr(" + R("lparam","0") + "))"); break;
                        case "PostMessage":          L("PostMessage(" + R("hwnd","") + ", " + R("msg_code","") + ", New IntPtr(" + R("wparam","0") + "), New IntPtr(" + R("lparam","0") + "))"); break;
                        case "SetForegroundWindow":  L("SetForegroundWindow(" + R("hwnd","") + ")"); break;
                        case "GetSystemMetrics":     L(R("out","") + " = GetSystemMetrics(" + R("index","0") + ")"); break;
                        case "ShowWindow":           L("ShowWindowAPI(" + R("hwnd","") + ", " + R("cmd","9") + ")"); break;
                    }
                    break;

                case "FS":
                    var fsPath = G("path");
                    switch (act)
                    {
                        case "Write File":       L("File.WriteAllText(" + fsPath + ", " + R("content","") + ")"); break;
                        case "Append File":      L("File.AppendAllText(" + fsPath + ", " + R("content","") + ")"); break;
                        case "Read File":        L(R("out","") + " = File.ReadAllText(" + G("path") + ")"); break;
                        case "Delete File":      L("File.Delete(" + fsPath + ")"); break;
                        case "Copy File":        L("File.Copy(" + G("src") + ", " + G("dst") + ")"); break;
                        case "Move File":        L("File.Move(" + G("src") + ", " + G("dst") + ")"); break;
                        case "Create Directory": L("Directory.CreateDirectory(" + fsPath + ")"); break;
                        case "Delete Directory": L("Directory.Delete(" + fsPath + ", True)"); break;
                        case "Get Files":        L(R("out","") + " = Directory.GetFiles(" + fsPath + ")"); break;
                        case "Get Directories":  L(R("out","") + " = Directory.GetDirectories(" + fsPath + ")"); break;
                    }
                    break;

                case "IO":
                    switch (act)
                    {
                        case "Console Write":     L("Console.Write(" + G("text") + ")"); break;
                        case "Console WriteLine": L("Console.WriteLine(" + G("text") + ")"); break;
                        case "Console Read Line": L(R("out","") + " = Console.ReadLine()"); break;
                    }
                    break;

                case "OS":
                    switch (act)
                    {
                        case "Run Process":
                            L("Process.Start(" + G("exe") + ", " + G("args") + ")"); break;
                        case "Run Process (Wait)":
                            L("Dim _proc As New Process()");
                            L("_proc.StartInfo.FileName = " + G("exe"));
                            L("_proc.StartInfo.Arguments = " + G("args"));
                            L("_proc.Start()");
                            L("_proc.WaitForExit()");
                            L(R("out","") + " = _proc.ExitCode");
                            break;
                        case "Get Env Variable":      L(R("out","") + " = Environment.GetEnvironmentVariable(" + G("var") + ")"); break;
                        case "Set Env Variable":      L("Environment.SetEnvironmentVariable(" + G("var") + ", " + G("value") + ")"); break;
                        case "Get Current Directory": L(R("out","") + " = Directory.GetCurrentDirectory()"); break;
                        case "Set Current Directory": L("Directory.SetCurrentDirectory(" + G("path") + ")"); break;
                        case "Exit": L("Environment.Exit(" + R("code","0") + ")"); break;
                    }
                    break;

                case "Clipboard":
                    switch (act)
                    {
                        case "Set Clipboard Text": L("Clipboard.SetText(" + G("text") + ")"); break;
                        case "Get Clipboard Text": L(R("out","") + " = Clipboard.GetText()"); break;
                        case "Clear Clipboard":    L("Clipboard.Clear()"); break;
                    }
                    break;

                case "Timer":
                    var tCtrl = R("control","");
                    switch (act)
                    {
                        case "Start Timer":   L(tCtrl + ".Start()"); break;
                        case "Stop Timer":    L(tCtrl + ".Stop()"); break;
                        case "Set Interval":  L(tCtrl + ".Interval = " + R("ms","1000")); break;
                    }
                    break;

                case "Flow":
                    switch (act)
                    {
                        case "Comment":   L("' " + R("text","")); break;
                        case "Wait (ms)": L("System.Threading.Thread.Sleep(" + R("ms","1000") + ")"); break;
                        case "Call Sub":  L(R("sub_name","MySub") + "()"); break;
                    }
                    break;

                default:
                    // Extension action
                    Dictionary<string, ExtActionDef> eCat;
                    ExtActionDef eAct;
                    if (ExtensionRegistry.Actions.TryGetValue(cat, out eCat) && eCat.TryGetValue(act, out eAct))
                    {
                        var code = ExtSub(eAct.Code, p, eAct.Params);
                        foreach (var cl in code.Split('\n')) L(cl);
                    }
                    else L("' TODO: " + cat + " → " + act);
                    break;
            }

            if (lines.Count == 0) lines.Add(indent + "' TODO: " + cat + " → " + act);
            return lines;
        }

        // -- Trigger sub info --------------------------------------------------

        public static void GetEventSubInfo(string triggerType, Dictionary<string, string> tparams, int idx,
                                           out string subName, out string sig)
        {
            string ctrl;
            if (!tparams.TryGetValue("control", out ctrl)) ctrl = "ctrl1";

            if (ExtensionRegistry.IsExtTrigger(triggerType))
            {
                var edef = ExtensionRegistry.Triggers[triggerType];
                subName = ExtSub(edef.Subname,  tparams, edef.Params);
                var sp  = ExtSub(edef.Subparams, tparams, edef.Params);
                sig     = "\tPrivate Sub " + subName + "(" + sp + ")";
                return;
            }

            switch (triggerType)
            {
                case "Form Load":
                    subName = "Form1_Load";
                    sig     = "\tPrivate Sub Form1_Load(sender As Object, e As EventArgs)";
                    break;
                case "Control Clicked":
                    subName = ctrl + "_Click";
                    sig     = "\tPrivate Sub " + ctrl + "_Click(sender As Object, e As EventArgs)";
                    break;
                case "TextBox Modified":
                    subName = ctrl + "_TextChanged";
                    sig     = "\tPrivate Sub " + ctrl + "_TextChanged(sender As Object, e As EventArgs)";
                    break;
                case "Timer Tick":
                    subName = ctrl + "_Tick";
                    sig     = "\tPrivate Sub " + ctrl + "_Tick(sender As Object, e As EventArgs)";
                    break;
                case "Menu Item Click":
                    subName = ctrl + "_Click";
                    sig     = "\tPrivate Sub " + ctrl + "_Click(sender As Object, e As EventArgs)";
                    break;
                case "Custom Sub":
                    string sn;
                    if (!tparams.TryGetValue("sub_name", out sn)) sn = "MySub" + idx;
                    subName = sn;
                    sig     = "\tPrivate Sub " + sn + "()";
                    break;
                default:
                    subName = "Event_" + idx;
                    sig     = "\tPrivate Sub Event_" + idx + "(sender As Object, e As EventArgs)";
                    break;
            }
        }

        // -- Condition emission ------------------------------------------------

        static string EmitConditions(List<ItemDefinition> conditions, List<string> L, string baseIndent,
                                     out List<Tuple<string, string>> endLines)
        {
            var groups = new List<Tuple<string, object>>(); // (type, value)
            var run = new List<string>();

            foreach (var c in conditions)
            {
                var r = ConditionToVb(c);
                if (r.IsExt)
                {
                    if (run.Count > 0) { groups.Add(Tuple.Create<string,object>("builtin", new List<string>(run))); run.Clear(); }
                    groups.Add(Tuple.Create<string,object>("ext", (object)r));
                }
                else run.Add(r.Expr);
            }
            if (run.Count > 0) groups.Add(Tuple.Create<string,object>("builtin", new List<string>(run)));

            endLines = new List<Tuple<string, string>>();
            var indent = baseIndent;

            foreach (var grp in groups)
            {
                if (grp.Item1 == "builtin")
                {
                    var exprs = (List<string>)grp.Item2;
                    var expr  = string.Join(" AndAlso ", exprs);
                    L.Add(indent + "If " + expr + " Then");
                    endLines.Add(Tuple.Create(indent, "End If"));
                    indent += "\t";
                }
                else
                {
                    var cr = (ConditionResult)grp.Item2;
                    if (!string.IsNullOrEmpty(cr.Prep)) L.Add(indent + cr.Prep);
                    L.Add(indent + cr.IfLine);
                    endLines.Add(Tuple.Create(indent, cr.EndLine));
                    indent += "\t";
                }
            }
            return indent;
        }

        // -- Import collection -------------------------------------------------

        static HashSet<string> CollectImportKeys(ProjectModel project)
        {
            var seen = new HashSet<string>();
            foreach (var group in project.EventGroups)
                foreach (var ev in group.Events)
                {
                    foreach (var item in ev.Conditions)
                        if (!string.IsNullOrEmpty(item.Imports)) seen.Add(item.Imports);
                    foreach (var item in ev.Actions)
                        if (!string.IsNullOrEmpty(item.Imports)) seen.Add(item.Imports);
                }
            return seen;
        }

        static void CollectExtInlineImports(ProjectModel project,
                                            out List<string> imports, out List<string> dlls)
        {
            var seenImp = new HashSet<string>();
            var seenDll = new HashSet<string>();
            imports = new List<string>();
            dlls    = new List<string>();

            foreach (var group in project.EventGroups)
                foreach (var ev in group.Events)
                    foreach (var act in ev.Actions)
                    {
                        if (!ExtensionRegistry.IsExtAction(act)) continue;
                        Dictionary<string, ExtActionDef> cd;
                        ExtActionDef edef;
                        if (!ExtensionRegistry.Actions.TryGetValue(act.Category, out cd)) continue;
                        if (!cd.TryGetValue(act.Action, out edef)) continue;
                        if (!string.IsNullOrEmpty(edef.Imports_) && seenImp.Add(edef.Imports_)) imports.Add(edef.Imports_);
                        if (!string.IsNullOrEmpty(edef.DllImport) && seenDll.Add(edef.DllImport)) dlls.Add(edef.DllImport);
                    }
        }

        static List<string> CollectExtCustomSubs(ProjectModel project)
        {
            var seen = new HashSet<string>();
            var result = new List<string>();
            foreach (var group in project.EventGroups)
                foreach (var ev in group.Events)
                    foreach (var act in ev.Actions)
                    {
                        if (!ExtensionRegistry.IsExtAction(act)) continue;
                        Dictionary<string, ExtActionDef> cd;
                        ExtActionDef edef;
                        if (!ExtensionRegistry.Actions.TryGetValue(act.Category, out cd)) continue;
                        if (!cd.TryGetValue(act.Action, out edef)) continue;
                        var cs = ExtSub(edef.CustomSubs.Trim(), act.Params, edef.Params);
                        if (!string.IsNullOrEmpty(cs) && seen.Add(cs)) result.Add(cs);
                    }
            return result;
        }

        // -- Main code generator -----------------------------------------------

        public static string GenerateVbNet(ProjectModel project)
        {
            var L  = new List<string>();
            var fw = project.FormWidth;
            var fh = project.FormHeight;

            // -- Collect imports ----------------------------------------------
            var seenImp = new HashSet<string>();
            var seenDll = new HashSet<string>();
            var seenCs  = new HashSet<string>();
            var extraImp = new List<string>();
            var extraDll = new List<string>();
            var extraCs  = new List<string>();

            foreach (var k in CollectImportKeys(project))
            {
                ImportEntry entry;
                if (!ExtensionRegistry.Imports.TryGetValue(k, out entry)) continue;
                foreach (var line in (entry.Imports_ ?? "").Split('\n'))
                {
                    var t = line.Trim();
                    if (!string.IsNullOrEmpty(t) && seenImp.Add(t)) extraImp.Add(t);
                }
                foreach (var line in (entry.DllImport ?? "").Split('\n'))
                {
                    var t = line.Trim();
                    if (!string.IsNullOrEmpty(t) && seenDll.Add(t)) extraDll.Add(t);
                }
                var cs = (entry.CustomSubs ?? "").Trim();
                if (!string.IsNullOrEmpty(cs) && seenCs.Add(cs)) extraCs.Add(cs);
            }

            List<string> extImp, extDll;
            CollectExtInlineImports(project, out extImp, out extDll);
            foreach (var line in extImp) if (seenImp.Add(line)) extraImp.Add(line);
            foreach (var line in extDll) if (seenDll.Add(line)) extraDll.Add(line);
            foreach (var cs in CollectExtCustomSubs(project)) if (seenCs.Add(cs)) extraCs.Add(cs);

            // -- Header -------------------------------------------------------
            L.Add("' ============================================================");
            L.Add("' Generated by Extremly gooS(good) IDE (ES IDE)");
            L.Add("' Project: " + project.Name);
            if (ExtensionRegistry.Metadata.Count > 0)
            {
                L.Add("' Extensions active at compile time:");
                foreach (var em in ExtensionRegistry.Metadata)
                    L.Add("'   [" + em.Type + "] " + em.Name + " v" + em.Version + " by " + em.Developer);
            }
            else L.Add("' No extensions loaded");
            if (project.EmbeddedFiles.Count > 0)
            {
                L.Add("' Embedded resources:");
                foreach (var fp in project.EmbeddedFiles)
                    L.Add("'   " + Path.GetFileName(fp));
            }
            L.Add("' ============================================================");

            L.Add("Imports System");
            L.Add("Imports System.IO");
            L.Add("Imports System.Diagnostics");
            L.Add("Imports System.Runtime.InteropServices");
            L.Add("Imports System.Windows.Forms");
            L.Add("Imports System.Drawing");
            foreach (var imp in extraImp) L.Add(imp);
            L.Add("");

            L.Add("Public Class Form1");
            L.Add("\tInherits System.Windows.Forms.Form");
            L.Add("");

            // Win32
            L.Add("\t' -- Win32 API Declarations ----------------------------------");
            var apis = new[]
            {
                new[]{"FindWindow",         "IntPtr",  "ByVal lpClassName As String, ByVal lpWindowName As String"},
                new[]{"SendMessage",        "IntPtr",  "ByVal hWnd As IntPtr, ByVal Msg As UInteger, ByVal wParam As IntPtr, ByVal lParam As IntPtr"},
                new[]{"PostMessage",        "Boolean", "ByVal hWnd As IntPtr, ByVal Msg As UInteger, ByVal wParam As IntPtr, ByVal lParam As IntPtr"},
                new[]{"SetForegroundWindow","Boolean", "ByVal hWnd As IntPtr"},
                new[]{"GetSystemMetrics",   "Integer", "ByVal nIndex As Integer"},
                new[]{"ShowWindowAPI",      "Boolean", "ByVal hWnd As IntPtr, ByVal nCmdShow As Integer"},
                new[]{"MessageBoxW",        "Integer", "ByVal hWnd As IntPtr, ByVal text As String, ByVal caption As String, ByVal type As UInteger"},
            };
            foreach (var api in apis)
            {
                L.Add("\t<DllImport(\"user32.dll\", CharSet:=CharSet.Unicode, SetLastError:=True)>");
                L.Add("\tPrivate Shared Function " + api[0] + "(" + api[2] + ") As " + api[1]);
                L.Add("\tEnd Function");
                L.Add("");
            }
            foreach (var line in extraDll) L.Add("\t" + line);
            if (extraDll.Count > 0) L.Add("");

            // Control fields
            if (project.Controls.Count > 0)
            {
                L.Add("\t' -- Control Fields ------------------------------------------");
                foreach (var c in project.Controls)
                    L.Add("\tPrivate " + c.Name + " As New System.Windows.Forms." + c.Type + "()");
                L.Add("");
            }

            // Variables
            if (project.Variables.Count > 0)
            {
                L.Add("\t' -- Variables -----------------------------------------------");
                foreach (var v in project.Variables)
                {
                    if (v.Type == "String")
                        L.Add("\tPrivate " + v.Name + " As String = \"" + v.Default + "\"");
                    else if (v.Type == "Boolean")
                        L.Add("\tPrivate " + v.Name + " As Boolean = " + (string.IsNullOrEmpty(v.Default) ? "False" : v.Default));
                    else if (v.Type == "List(Of String)")
                    {
                        if(string.IsNullOrEmpty(v.Default))
                        {
                            L.Add("\tPrivate " + v.Name + " As New System.Collections.Generic.List(Of String)");
                        }
                        else
                        {
                            L.Add("\tPrivate " + v.Name + " As New System.Collections.Generic.List(Of String) From { \n\t\t" + v.Default+"\n\t}");
                        }
                        
                    }
                    else if (v.Type == "List(Of Integer)")
                    {
                        if (string.IsNullOrEmpty(v.Default))
                        {
                            L.Add("\tPrivate " + v.Name + " As New System.Collections.Generic.List(Of Integer)");
                        }
                        else
                        {
                            L.Add("\tPrivate " + v.Name + " As New System.Collections.Generic.List(Of Integer) From { \n\t\t" + v.Default + "\n\t}");
                        }

                    }
                    else
                        L.Add("\tPrivate " + v.Name + " As " + v.Type + " = " + (string.IsNullOrEmpty(v.Default) ? "0" : v.Default));
                }
                L.Add("");
            }

            // Custom subs
            foreach (var cs in extraCs)
            {
                foreach (var line in cs.Split('\n')) L.Add("\t" + line);
                L.Add("");
            }

            L.Add("\tPublic Sub New()");
            L.Add("\t\tInitializeComponent()");
            L.Add("\tEnd Sub");
            L.Add("");

            L.Add("\tPrivate Sub InitializeComponent()");
            L.Add("\t\tMe.SuspendLayout()");
            L.Add("\t\tMe.Text = \"" + project.Name + "\"");
            L.Add("\t\tMe.ClientSize = New System.Drawing.Size(" + fw + ", " + fh + ")");
            if (!project.Resizable)
            {
                L.Add("\t\tMe.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle");
                L.Add("\t\tMe.MaximizeBox = False");
            }
            L.Add("\t\tDim asm1 As Reflection.Assembly = Reflection.Assembly.GetExecutingAssembly()");
            L.Add("\t\tUsing strm1 As IO.Stream = asm1.GetManifestResourceStream(\"ico.ico\")");
            L.Add("\t\t\tIf strm1 IsNot Nothing Then Me.Icon = New Icon(strm1)");
            L.Add("\t\tEnd Using");
            L.Add("");

            foreach (var c in project.Controls)
            {
                if (BuiltinDefinitions.NonVisualControls.Contains(c.Type))
                {
                    if (c.Type == "Timer")
                    {
                        L.Add("\t\t" + c.Name + ".Interval = 1000");
                        L.Add("\t\t" + c.Name + ".Enabled = False");
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(c.Text))
                        L.Add("\t\t" + c.Name + ".Text = \"" + c.Text.Replace("\"", "\"\"") + "\"");
                    L.Add("\t\t" + c.Name + ".Location = New System.Drawing.Point(" + c.X + ", " + c.Y + ")");
                    L.Add("\t\t" + c.Name + ".Size = New System.Drawing.Size(" + c.W + ", " + c.H + ")");
                    L.Add("\t\tMe.Controls.Add(" + c.Name + ")");
                }
            }
            if (project.Controls.Count > 0) L.Add("");

            // AddHandlers
            var seenHandlers = new HashSet<string>();
            for (int gi = 0; gi < project.EventGroups.Count; gi++)
            {
                var group  = project.EventGroups[gi];
                var trig   = group.Trigger;
                var ttype  = trig.Type;
                var tparms = trig.Params;
                if (ttype == "Custom Sub") continue;
                string subName, sigTmp;
                GetEventSubInfo(ttype, tparms, gi, out subName, out sigTmp);
                string tcCtrl; tparms.TryGetValue("control", out tcCtrl);
                var hKey = ttype + "|" + (tcCtrl ?? "") + "|" + subName;
                if (!seenHandlers.Add(hKey)) continue;

                if (ExtensionRegistry.IsExtTrigger(ttype))
                {
                    var edef = ExtensionRegistry.Triggers[ttype];
                    L.Add("\t\t" + ExtSub(edef.InitComponent, tparms, edef.Params));
                }
                else
                {
                    switch (ttype)
                    {
                        case "Form Load":        L.Add("\t\tAddHandler Me.Load, AddressOf " + subName); break;
                        case "Control Clicked":  L.Add("\t\tAddHandler " + tcCtrl + ".Click, AddressOf " + subName); break;
                        case "TextBox Modified": L.Add("\t\tAddHandler " + tcCtrl + ".TextChanged, AddressOf " + subName); break;
                        case "Timer Tick":       L.Add("\t\tAddHandler " + tcCtrl + ".Tick, AddressOf " + subName); break;
                        case "Menu Item Click":  L.Add("\t\tAddHandler " + tcCtrl + ".Click, AddressOf " + subName); break;
                        default: L.Add("\t\t' (unhandled trigger: " + ttype + ")"); break;
                    }
                }
            }
            L.Add("");
            L.Add("\t\tMe.ResumeLayout(False)");
            L.Add("\tEnd Sub");
            L.Add("");

            // Event Subs
            for (int gi = 0; gi < project.EventGroups.Count; gi++)
            {
                var group  = project.EventGroups[gi];
                var ttype  = group.Trigger.Type;
                var tparms = group.Trigger.Params;
                string subName, headerSig;
                GetEventSubInfo(ttype, tparms, gi, out subName, out headerSig);

                L.Add("\t' ==== Event Group: " + group.Name + " ====");
                L.Add(headerSig);

                if (group.Events.Count == 0) L.Add("\t\t' (empty group)");

                foreach (var ev in group.Events)
                {
                    L.Add("\t\t' -- " + ev.Name + " --");
                    string actionIndent;
                    List<Tuple<string, string>> endLines;
                    if (ev.Conditions.Count > 0)
                        actionIndent = EmitConditions(ev.Conditions, L, "\t\t", out endLines);
                    else
                    {
                        actionIndent = "\t\t";
                        endLines = new List<Tuple<string, string>>();
                    }

                    foreach (var act in ev.Actions)
                        L.AddRange(ActionToVb(act, actionIndent));

                    for (int ei = endLines.Count - 1; ei >= 0; ei--)
                        L.Add(endLines[ei].Item1 + endLines[ei].Item2);
                }

                L.Add("\tEnd Sub");
                L.Add("");
            }

            L.Add("End Class");
            L.Add("");
            L.Add("' -- Entry point ----------------------------------------------");
            L.Add("Module Program");
            L.Add("\tSub Main()");
            if (project.VStyle)
            {
                L.Add("\t\tApplication.EnableVisualStyles()");
            }
            L.Add("\t\tApplication.SetCompatibleTextRenderingDefault(False)");
            L.Add("\t\tApplication.Run(New Form1())");
            L.Add("\tEnd Sub");
            L.Add("End Module");

            return string.Join("\n", L);
        }
    }
}
