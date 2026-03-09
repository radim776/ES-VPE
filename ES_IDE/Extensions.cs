using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace EventScriptIDE
{
    /// <summary>
    /// Loads extension JSON files from the Extensions folder.
    /// Format is 100% compatible with the Python version.
    /// </summary>
    public static class ExtensionRegistry
    {
        public static Dictionary<string, ImportEntry> Imports { get; } = new Dictionary<string, ImportEntry>
        {
            { "threading", new ImportEntry { Imports_ = "Imports System.Threading" } },
            { "regex", new ImportEntry { Imports_ = "Imports System.Text.RegularExpressions" } },
            { "net_http", new ImportEntry { Imports_ = "Imports System.Net\nImports System.Net.Http" } },
            { "xml", new ImportEntry { Imports_ = "Imports System.Xml\nImports System.Xml.Linq" } },
            { "json_net", new ImportEntry { Imports_ = "Imports Newtonsoft.Json" } },
        };

        public static List<ExtMeta> Metadata { get; } = new List<ExtMeta>();

        public static Dictionary<string, ExtTriggerDef>  Triggers   { get; } = new Dictionary<string, ExtTriggerDef>();
        public static Dictionary<string, Dictionary<string, ExtConditionDef>> Conditions { get; } = new Dictionary<string, Dictionary<string, ExtConditionDef>>();
        public static Dictionary<string, Dictionary<string, ExtActionDef>>    Actions    { get; } = new Dictionary<string, Dictionary<string, ExtActionDef>>();
        
        public static void Reload()
        {
            Metadata.Clear();
            Triggers.Clear();
            Conditions.Clear();
            Actions.Clear();

            foreach (var sub in new[] { "Imports", "Triggers", "Conditions", "Actions" })
            {
                var folder = Path.Combine(SettingsManager.ExtensionsDir, sub);
                if (!Directory.Exists(folder)) continue;
                foreach (var file in Directory.GetFiles(folder, "*.json").OrderBy(f => f))
                    TryLoad(sub, file);
            }
        }

        static void TryLoad(string subdir, string path)
        {
            try
            {
                var text = File.ReadAllText(path);
                var arr = JArray.Parse(text);
                if (arr.Count == 0) return;

                var meta = arr[0] as JObject;
                if (meta == null || meta.Value<string>("SpecialType") != "ExtensionData") return;

                var extMeta = new ExtMeta
                {
                    Type = subdir,
                    File = path,
                    Name = meta.Value<string>("Name") ?? Path.GetFileName(path),
                    Version = meta.Value<string>("Version") ?? "?",
                    Developer = meta.Value<string>("Developer") ?? "",
                    Website = meta.Value<string>("Website") ?? "",
                };
                Metadata.Add(extMeta);

                var items = arr.Skip(1).OfType<JObject>().ToList();
                switch (subdir)
                {
                    case "Imports": LoadImports(items); break;
                    case "Triggers": LoadTriggers(items, extMeta); break;
                    case "Conditions": LoadConditions(items, extMeta); break;
                    case "Actions": LoadActions(items, extMeta); break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Extensions] Failed to load " + path + ": " + ex.Message);
            }
        }

        static void LoadImports(List<JObject> items)
        {
            foreach (var item in items)
                foreach (var prop in item.Properties())
                {
                    var v = prop.Value as JObject;
                    if (v != null)
                        Imports[prop.Name] = new ImportEntry
                        {
                            Imports_ = v.Value<string>("Imports") ?? "",
                            DllImport = v.Value<string>("DLLImport") ?? "",
                            CustomSubs = v.Value<string>("CustomSubs") ?? "",
                        };
                }
        }

        static void LoadTriggers(List<JObject> items, ExtMeta meta)
        {
            foreach (var item in items)
            {
                var name = item.Value<string>("Name");
                if (string.IsNullOrEmpty(name)) continue;
                Triggers[name] = new ExtTriggerDef
                {
                    Params = ParseParams(item["Parameters"] as JArray),
                    InitComponent = item.Value<string>("InitComponent") ?? "",
                    Subname = item.Value<string>("Subname") ?? ("ExtTrigger_" + name.Replace(" ", "_")),
                    Subparams = item.Value<string>("Subparams") ?? "sender As Object, e As EventArgs",
                    Meta = meta,
                };
            }
        }

        static void LoadConditions(List<JObject> items, ExtMeta meta)
        {
            var cat = meta.Name;
            if (!Conditions.ContainsKey(cat)) Conditions[cat] = new Dictionary<string, ExtConditionDef>();
            foreach (var item in items)
            {
                var name = item.Value<string>("Name");
                if (string.IsNullOrEmpty(name)) continue;
                Conditions[cat][name] = new ExtConditionDef
                {
                    Params = ParseParams(item["Parameters"] as JArray),
                    Prep = item.Value<string>("Prep") ?? "",
                    IfLine = item.Value<string>("IfLine") ?? "",
                    EndLine = item.Value<string>("EndLine") ?? "End If",
                    Meta = meta,
                };
            }
        }

        static void LoadActions(List<JObject> items, ExtMeta meta)
        {
            var cat = meta.Name;
            if (!Actions.ContainsKey(cat)) Actions[cat] = new Dictionary<string, ExtActionDef>();
            foreach (var item in items)
            {
                var name = item.Value<string>("Name");
                if (string.IsNullOrEmpty(name)) continue;
                Actions[cat][name] = new ExtActionDef
                {
                    Params = ParseParams(item["Parameters"] as JArray),
                    Code = item.Value<string>("Code") ?? "",
                    CustomSubs = item.Value<string>("CustomSubs") ?? "",
                    Imports_ = item.Value<string>("Imports") ?? "",
                    DllImport = item.Value<string>("DLLImport") ?? "",
                    Meta = meta,
                };
            }
        }

        static List<ParamDef> ParseParams(JArray arr)
        {
            var list = new List<ParamDef>();
            if (arr == null) return list;
            foreach (var tok in arr.OfType<JObject>())
            {
                var t = tok.Value<string>("Type") ?? "Expr";
                var pd = new ParamDef
                {
                    Name = tok.Value<string>("Id") ?? "",
                    Label = tok.Value<string>("Label") ?? tok.Value<string>("Id") ?? "",
                    Required = tok.Value<bool?>("Required") ?? true,
                    Default = tok.Value<string>("Default") ?? "",
                };
                if (t == "Str" || t == "String") pd.Kind = "str";
                else if (t == "Num") pd.Kind = "num";
                else if (t == "NumInt") pd.Kind = "numint";
                else if (t == "Bool") pd.Kind = "bool";
                else if (t == "Select") pd.Kind = "select";

                if (pd.Kind == "select")
                {
                    var valObj = tok["Values"] as JObject;
                    if (valObj != null)
                        foreach (var p in valObj.Properties())
                            pd.Values[p.Name] = p.Value.ToString();
                }
                list.Add(pd);
            }
            return list;
        }
        
        public static Dictionary<string, List<ParamDef>> GetAllTriggers()
        {
            var merged = new Dictionary<string, List<ParamDef>>(BuiltinDefinitions.Triggers);
            foreach (var kvp in Triggers)
                merged[kvp.Key] = kvp.Value.Params;
            return merged;
        }

        public static Dictionary<string, Dictionary<string, List<ParamDef>>> GetAllConditions()
        {
            var merged = new Dictionary<string, Dictionary<string, List<ParamDef>>>();
            foreach (var kvp in BuiltinDefinitions.Conditions)
                merged[kvp.Key] = new Dictionary<string, List<ParamDef>>(kvp.Value);
            foreach (var catKvp in Conditions)
            {
                if (!merged.ContainsKey(catKvp.Key)) merged[catKvp.Key] = new Dictionary<string, List<ParamDef>>();
                foreach (var condKvp in catKvp.Value)
                    merged[catKvp.Key][condKvp.Key] = condKvp.Value.Params;
            }
            return merged;
        }

        public static Dictionary<string, Dictionary<string, List<ParamDef>>> GetAllActions()
        {
            var merged = new Dictionary<string, Dictionary<string, List<ParamDef>>>();
            foreach (var kvp in BuiltinDefinitions.Actions)
                merged[kvp.Key] = new Dictionary<string, List<ParamDef>>(kvp.Value);
            foreach (var catKvp in Actions)
            {
                if (!merged.ContainsKey(catKvp.Key)) merged[catKvp.Key] = new Dictionary<string, List<ParamDef>>();
                foreach (var actKvp in catKvp.Value)
                    merged[catKvp.Key][actKvp.Key] = actKvp.Value.Params;
            }
            return merged;
        }

        public static bool IsExtTrigger(string type) => Triggers.ContainsKey(type);
        public static bool IsExtCondition(ItemDefinition c)
        {
            Dictionary<string, ExtConditionDef> d;
            return Conditions.TryGetValue(c.Category, out d) && d.ContainsKey(c.Action);
        }
        public static bool IsExtAction(ItemDefinition a)
        {
            Dictionary<string, ExtActionDef> d;
            return Actions.TryGetValue(a.Category, out d) && d.ContainsKey(a.Action);
        }
    }
    
    public class ImportEntry
    {
        public string Imports_ { get; set; } = "";
        public string DllImport { get; set; } = "";
        public string CustomSubs { get; set; } = "";
    }

    public class ExtTriggerDef
    {
        public List<ParamDef> Params { get; set; } = new List<ParamDef>();
        public string InitComponent { get; set; } = "";
        public string Subname { get; set; } = "";
        public string Subparams { get; set; } = "sender As Object, e As EventArgs";
        public ExtMeta Meta { get; set; }
    }

    public class ExtConditionDef
    {
        public List<ParamDef> Params { get; set; } = new List<ParamDef>();
        public string Prep { get; set; } = "";
        public string IfLine { get; set; } = "";
        public string EndLine { get; set; } = "End If";
        public ExtMeta Meta { get; set; }
    }

    public class ExtActionDef
    {
        public List<ParamDef> Params { get; set; } = new List<ParamDef>();
        public string Code { get; set; } = "";
        public string CustomSubs { get; set; } = "";
        public string Imports_ { get; set; } = "";
        public string DllImport { get; set; } = "";
        public ExtMeta Meta { get; set; }
    }
}
