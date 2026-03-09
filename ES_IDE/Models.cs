using System.Collections.Generic;
using Newtonsoft.Json;

namespace EventScriptIDE
{
    // ── Project Model ──────────────────────────────────────────────────────────
    public class ProjectModel
    {
        [JsonProperty("name")]          public string Name        { get; set; } = "MyProject";
        [JsonProperty("form_width")]    public int    FormWidth   { get; set; } = 800;
        [JsonProperty("form_height")]   public int    FormHeight  { get; set; } = 600;
        [JsonProperty("resizable")]     public bool   Resizable   { get; set; } = true;
        [JsonProperty("VStyle")]        public bool   VStyle      { get; set; } = true;
        [JsonProperty("extra_dlls")]    public List<string>       ExtraDlls      { get; set; } = new List<string>();
        [JsonProperty("embedded_files")]public List<string>       EmbeddedFiles  { get; set; } = new List<string>();
        [JsonProperty("variables")]     public List<VariableModel>Variables      { get; set; } = new List<VariableModel>();
        [JsonProperty("controls")]      public List<ControlModel> Controls       { get; set; } = new List<ControlModel>();
        [JsonProperty("event_groups")]  public List<EventGroup>   EventGroups    { get; set; } = new List<EventGroup>();

        public ProjectModel DeepClone()
        {
            return JsonConvert.DeserializeObject<ProjectModel>(JsonConvert.SerializeObject(this));
        }
    }

    // ── Event Group ────────────────────────────────────────────────────────────
    public class EventGroup
    {
        [JsonProperty("name")]    public string      Name    { get; set; } = "Group 1";
        [JsonProperty("trigger")] public TriggerInfo Trigger { get; set; } = new TriggerInfo();
        [JsonProperty("events")]  public List<EventModel> Events { get; set; } = new List<EventModel>();
    }

    // ── Trigger ────────────────────────────────────────────────────────────────
    public class TriggerInfo
    {
        [JsonProperty("type")]   public string Type   { get; set; } = "Form Load";
        [JsonProperty("params")] public Dictionary<string, string> Params { get; set; } = new Dictionary<string, string>();
    }

    // ── Event ──────────────────────────────────────────────────────────────────
    public class EventModel
    {
        [JsonProperty("name")]       public string Name       { get; set; } = "Event 1";
        [JsonProperty("conditions")] public List<ItemDefinition> Conditions { get; set; } = new List<ItemDefinition>();
        [JsonProperty("actions")]    public List<ItemDefinition> Actions    { get; set; } = new List<ItemDefinition>();
    }

    // ── Condition / Action item ────────────────────────────────────────────────
    public class ItemDefinition
    {
        [JsonProperty("category")] public string Category { get; set; } = "";
        [JsonProperty("action")]   public string Action   { get; set; } = "";
        [JsonProperty("params")]   public Dictionary<string, string> Params { get; set; } = new Dictionary<string, string>();
        [JsonProperty("imports")]  public string Imports  { get; set; } = "";
    }

    // ── Variable ───────────────────────────────────────────────────────────────
    public class VariableModel
    {
        [JsonProperty("name")]    public string Name    { get; set; } = "";
        [JsonProperty("type")]    public string Type    { get; set; } = "String";
        [JsonProperty("default")] public string Default { get; set; } = "";
    }

    // ── Control ────────────────────────────────────────────────────────────────
    public class ControlModel
    {
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("type")] public string Type { get; set; } = "Button";
        [JsonProperty("text")] public string Text { get; set; } = "";
        [JsonProperty("x")]    public int    X    { get; set; } = 10;
        [JsonProperty("y")]    public int    Y    { get; set; } = 10;
        [JsonProperty("w")]    public int    W    { get; set; } = 100;
        [JsonProperty("h")]    public int    H    { get; set; } = 30;
    }

    // ── Param definition (internal, not serialised) ────────────────────────────
    public class ParamDef
    {
        public string Name     { get; set; } = "";
        public string Label    { get; set; } = "";
        /// <summary>"str" | "num" | "numint" | "bool" | "select" | ""</summary>
        public string Kind     { get; set; } = "";
        public bool   Required { get; set; } = true;
        public string Default  { get; set; } = "";
        /// <summary>For Kind=="select": key→display label</summary>
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
        /// <summary>Import-key injection (built-in only)</summary>
        public string Import   { get; set; } = "";
    }

    // ── Extension metadata ─────────────────────────────────────────────────────
    public class ExtMeta
    {
        public string Type      { get; set; } = "";
        public string File      { get; set; } = "";
        public string Name      { get; set; } = "";
        public string Version   { get; set; } = "?";
        public string Developer { get; set; } = "";
        public string Website   { get; set; } = "";
    }

    // ── Condition result (for code-gen) ────────────────────────────────────────
    public class ConditionResult
    {
        public bool   IsExt   { get; set; }
        public string Expr    { get; set; }   // built-in
        public string Prep    { get; set; }   // ext
        public string IfLine  { get; set; }   // ext
        public string EndLine { get; set; }   // ext
    }
}
