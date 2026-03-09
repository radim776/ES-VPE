using System.Collections.Generic;

namespace EventScriptIDE
{
    /// <summary>
    /// Built-in TRIGGERS, CONDITIONS and ACTIONS — mirrors the Python dicts exactly.
    /// </summary>
    public static class BuiltinDefinitions
    {
        static ParamDef E(string name, string label)
        {
            return new ParamDef { Name = name, Label = label };
        }

        static ParamDef S(string name, string label)
        {
            return new ParamDef { Name = name, Label = label + " (text or {expr})", Kind = "str" };
        }

        // ── TRIGGERS ──────────────────────────────────────────────────────────
        public static readonly Dictionary<string, List<ParamDef>> Triggers = new Dictionary<string, List<ParamDef>>
        {
            { "Form Load",        new List<ParamDef>() },
            { "Control Clicked",  new List<ParamDef> { E("control","Control Name") } },
            { "TextBox Modified", new List<ParamDef> { E("control","TextBox Name") } },
            { "Timer Tick",       new List<ParamDef> { E("control","Timer Name") } },
            { "Menu Item Click",  new List<ParamDef> { E("control","MenuItem Name") } },
            { "Custom Sub",       new List<ParamDef> { E("sub_name","Sub Name") } },
        };

        // ── CONDITIONS ────────────────────────────────────────────────────────
        public static readonly Dictionary<string, Dictionary<string, List<ParamDef>>> Conditions =
            new Dictionary<string, Dictionary<string, List<ParamDef>>>
        {
            { "Variable", new Dictionary<string, List<ParamDef>> {
                { "Equals",          new List<ParamDef> { E("var","Variable"), E("value","Value/Expr") } },
                { "Not Equals",      new List<ParamDef> { E("var","Variable"), E("value","Value/Expr") } },
                { "Greater Than",    new List<ParamDef> { E("var","Variable"), E("value","Value/Expr") } },
                { "Less Than",       new List<ParamDef> { E("var","Variable"), E("value","Value/Expr") } },
                { "Greater Or Equal",new List<ParamDef> { E("var","Variable"), E("value","Value/Expr") } },
                { "Less Or Equal",   new List<ParamDef> { E("var","Variable"), E("value","Value/Expr") } },
            } },
            { "String", new Dictionary<string, List<ParamDef>> {
                { "Contains",   new List<ParamDef> { E("expr","String Expr"), E("substr","Substring") } },
                { "Starts With",new List<ParamDef> { E("expr","String Expr"), E("prefix","Prefix") } },
                { "Ends With",  new List<ParamDef> { E("expr","String Expr"), E("suffix","Suffix") } },
                { "Is Empty",   new List<ParamDef> { E("expr","String Expr") } },
                { "Not Empty",  new List<ParamDef> { E("expr","String Expr") } },
            } },
            { "FS", new Dictionary<string, List<ParamDef>> {
                { "File Exists",      new List<ParamDef> { E("path","Path Expression") } },
                { "File Not Exists",  new List<ParamDef> { E("path","Path Expression") } },
                { "Directory Exists", new List<ParamDef> { E("path","Path Expression") } },
            } },
            { "OS", new Dictionary<string, List<ParamDef>> {
                { "Env Var Equals", new List<ParamDef> { E("var","Env Var Name"), E("value","Value") } },
            } },
            { "Custom", new Dictionary<string, List<ParamDef>> {
                { "Expression", new List<ParamDef> { E("expr","VB.NET Boolean Expression") } },
            } },
        };

        // ── ACTIONS ───────────────────────────────────────────────────────────
        public static readonly Dictionary<string, Dictionary<string, List<ParamDef>>> Actions =
            new Dictionary<string, Dictionary<string, List<ParamDef>>>
        {
            { "Variable", new Dictionary<string, List<ParamDef>> {
                { "Set Variable",           new List<ParamDef> { E("var","Variable Name"), E("value","Value/Expression") } },
                { "Add To Variable",        new List<ParamDef> { E("var","Variable Name"), E("value","Amount") } },
                { "Subtract From Variable", new List<ParamDef> { E("var","Variable Name"), E("value","Amount") } },
                { "Multiply Variable",      new List<ParamDef> { E("var","Variable Name"), E("value","Factor") } },
                { "Divide Variable",        new List<ParamDef> { E("var","Variable Name"), E("value","Divisor") } },
            } },
            { "UI", new Dictionary<string, List<ParamDef>> {
                { "Set Control Text",        new List<ParamDef> { E("control","Control Name"), S("text","Text") } },
                { "Get Control Text",        new List<ParamDef> { E("var","Result Variable"), E("control","Control Name") } },
                { "Show MessageBox",         new List<ParamDef> { S("msg","Message"), S("title","Title") } },
                { "Show MessageBox (YesNo)", new List<ParamDef> { E("var","Result Variable (Boolean)"), S("msg","Message"), S("title","Title") } },
                { "Set Form Title",          new List<ParamDef> { S("title","Title") } },
                { "Set Form Size",           new List<ParamDef> { E("w","Width"), E("h","Height") } },
                { "Set Control Visible",     new List<ParamDef> { E("control","Control Name"), E("visible","True or False") } },
                { "Set Control Enabled",     new List<ParamDef> { E("control","Control Name"), E("enabled","True or False") } },
                { "Set Control Color",       new List<ParamDef> { E("control","Control Name"), E("color","Color (e.g. Color.Red)") } },
                { "Add ListBox Item",        new List<ParamDef> { E("control","ListBox Name"), S("item","Item") } },
                { "Clear ListBox",           new List<ParamDef> { E("control","ListBox Name") } },
                { "Get ListBox Selection",   new List<ParamDef> { E("var","Result Variable"), E("control","ListBox Name") } },
                { "Focus Control",           new List<ParamDef> { E("control","Control Name") } },
                { "Close Form",              new List<ParamDef>() },
                { "Open File Dialog",        new List<ParamDef> { E("var","Result Variable (path)"), S("filter","Filter e.g. Text|*.txt") } },
                { "Save File Dialog",        new List<ParamDef> { E("var","Result Variable (path)"), S("filter","Filter e.g. Text|*.txt") } },
            } },
            { "Math", new Dictionary<string, List<ParamDef>> {
                { "Set To Expression", new List<ParamDef> { E("var","Variable Name"), E("expr","VB.NET Expression") } },
                { "Set To Random",     new List<ParamDef> { E("var","Variable Name"), E("min","Min"), E("max","Max") } },
                { "Round",             new List<ParamDef> { E("var","Variable Name"), E("digits","Decimal Digits") } },
                { "Abs",               new List<ParamDef> { E("var","Variable Name"), E("expr","Expression") } },
                { "Sqrt",              new List<ParamDef> { E("var","Variable Name"), E("expr","Expression") } },
                { "Power",             new List<ParamDef> { E("var","Variable Name"), E("base","Base"), E("exp","Exponent") } },
                { "Min Of Two",        new List<ParamDef> { E("var","Variable Name"), E("a","A"), E("b","B") } },
                { "Max Of Two",        new List<ParamDef> { E("var","Variable Name"), E("a","A"), E("b","B") } },
            } },
            { "String", new Dictionary<string, List<ParamDef>> {
                { "Concatenate",   new List<ParamDef> { E("var","Variable Name"), E("a","A"), E("b","B") } },
                { "Replace",       new List<ParamDef> { E("var","Variable Name"), S("old","Old Text"), S("new","New Text") } },
                { "To Uppercase",  new List<ParamDef> { E("var","Variable Name") } },
                { "To Lowercase",  new List<ParamDef> { E("var","Variable Name") } },
                { "Trim",          new List<ParamDef> { E("var","Variable Name") } },
                { "Get Length",    new List<ParamDef> { E("out","Result Variable"), E("expr","String Expression") } },
                { "Substring",     new List<ParamDef> { E("out","Result Variable"), E("expr","String Expression"), E("start","Start Index"), E("length","Length") } },
                { "Split",         new List<ParamDef> { E("out","Result Variable (String())"), E("expr","String Expression"), S("sep","Separator Char") } },
                { "Index Of",      new List<ParamDef> { E("out","Result Variable"), E("expr","String Expression"), S("search","Search") } },
                { "Format Number", new List<ParamDef> { E("out","Result Variable"), E("expr","Number Expression"), S("fmt","Format e.g. F2") } },
            } },
            { "Win32", new Dictionary<string, List<ParamDef>> {
                { "MessageBox API",      new List<ParamDef> { S("msg","Message"), S("title","Title"), E("flags","Flags (0=OK, 4=YesNo)") } },
                { "FindWindow",          new List<ParamDef> { E("out","Result Variable (IntPtr)"), E("class_name","ClassName (Nothing for any)"), S("title","Window Title") } },
                { "SendMessage",         new List<ParamDef> { E("hwnd","hWnd Variable"), E("msg_code","Message Code (hex ok)"), E("wparam","wParam"), E("lparam","lParam") } },
                { "PostMessage",         new List<ParamDef> { E("hwnd","hWnd Variable"), E("msg_code","Message Code"), E("wparam","wParam"), E("lparam","lParam") } },
                { "SetForegroundWindow", new List<ParamDef> { E("hwnd","hWnd Variable") } },
                { "GetSystemMetrics",    new List<ParamDef> { E("out","Result Variable"), E("index","Index (0=ScreenW, 1=ScreenH)") } },
                { "ShowWindow",          new List<ParamDef> { E("hwnd","hWnd Variable"), E("cmd","Cmd (0=Hide,1=Normal,9=Restore)") } },
            } },
            { "FS", new Dictionary<string, List<ParamDef>> {
                { "Write File",        new List<ParamDef> { S("path","Path"), E("content","Content Expression") } },
                { "Append File",       new List<ParamDef> { S("path","Path"), E("content","Content Expression") } },
                { "Read File",         new List<ParamDef> { E("out","Result Variable"), S("path","Path") } },
                { "Delete File",       new List<ParamDef> { S("path","Path") } },
                { "Copy File",         new List<ParamDef> { S("src","Source Path"), S("dst","Destination Path") } },
                { "Move File",         new List<ParamDef> { S("src","Source Path"), S("dst","Destination Path") } },
                { "Create Directory",  new List<ParamDef> { S("path","Path") } },
                { "Delete Directory",  new List<ParamDef> { S("path","Path") } },
                { "Get Files",         new List<ParamDef> { E("out","Result Variable (String())"), S("path","Directory Path") } },
                { "Get Directories",   new List<ParamDef> { E("out","Result Variable (String())"), S("path","Directory Path") } },
            } },
            { "IO", new Dictionary<string, List<ParamDef>> {
                { "Console Write",     new List<ParamDef> { S("text","Text") } },
                { "Console WriteLine", new List<ParamDef> { S("text","Text") } },
                { "Console Read Line", new List<ParamDef> { E("out","Result Variable") } },
            } },
            { "OS", new Dictionary<string, List<ParamDef>> {
                { "Run Process",           new List<ParamDef> { S("exe","Executable"), S("args","Arguments") } },
                { "Run Process (Wait)",    new List<ParamDef> { S("exe","Executable"), S("args","Arguments"), E("out","Exit Code Variable") } },
                { "Get Env Variable",      new List<ParamDef> { E("out","Result Variable"), S("var","Env Var Name") } },
                { "Set Env Variable",      new List<ParamDef> { S("var","Env Var Name"), S("value","Value") } },
                { "Get Current Directory", new List<ParamDef> { E("out","Result Variable") } },
                { "Set Current Directory", new List<ParamDef> { S("path","Path") } },
                { "Exit",                  new List<ParamDef> { E("code","Exit Code") } },
            } },
            { "Clipboard", new Dictionary<string, List<ParamDef>> {
                { "Set Clipboard Text", new List<ParamDef> { S("text","Text") } },
                { "Get Clipboard Text", new List<ParamDef> { E("out","Result Variable") } },
                { "Clear Clipboard",    new List<ParamDef>() },
            } },
            { "Flow", new Dictionary<string, List<ParamDef>> {
                { "Comment",   new List<ParamDef> { S("text","Comment Text") } },
                { "Wait (ms)", new List<ParamDef> { E("ms","Milliseconds Expression") } },
                { "Call Sub",  new List<ParamDef> { E("sub_name","Sub Name") } },
            } },
            { "Timer", new Dictionary<string, List<ParamDef>> {
                { "Start Timer",  new List<ParamDef> { E("control","Timer Name") } },
                { "Stop Timer",   new List<ParamDef> { E("control","Timer Name") } },
                { "Set Interval", new List<ParamDef> { E("control","Timer Name"), E("ms","Interval (ms)") } },
            } },
        };

        public static readonly HashSet<string> NonVisualControls = new HashSet<string> { "Timer","ToolStripMenuItem" };

        public static readonly string[] ControlTypes =
        {
            "Button","Label","TextBox","CheckBox","RadioButton","ComboBox",
            "ListBox","GroupBox","Panel","PictureBox","ProgressBar",
            "TrackBar","NumericUpDown","RichTextBox","TabControl","Timer",
            "MenuStrip","ToolStripMenuItem","DataGridView"
        };
    }
}
