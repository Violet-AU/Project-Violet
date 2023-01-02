using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Interface.Menus;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TownOfHost.ReduxOptions;

public class OptionHolder
{
    public string Name;
    public string Header;
    public StringOption Behaviour;
    public Color? color;
    public GameOptionTab Tab { get => _tab;
        set {
            _tab = value;
            value?.AddHolder(this);
            if (value != null) Name.DebugLog("Added As Holder: ");
        }
    }

    public bool IsHeader;

    public OptionValueHolder valueHolder { get => _valueHolder; init => _valueHolder = value; }
    public List<OptionHolder> SubOptions { private get => _subOptions; init => _subOptions = value; }
    public Func<object, bool> ShowOptionPredicate { private get => _showOptionPredicate; init => _showOptionPredicate = value; }
    public int Level;

    private GameOptionTab _tab;
    private readonly OptionValueHolder _valueHolder;
    private readonly List<OptionHolder> _subOptions = new();
    private readonly Func<object, bool> _showOptionPredicate;

    public string GetAsString(int index = -1) => this.valueHolder == null ? "N/A" : this.valueHolder.GetAsString(index);
    public object GetValue(int index = -1) => this.valueHolder.GetValue(index);
    public T GetValue<T>(int index = -1) => this.valueHolder.GetValue<T>(index);

    public object Increment() => this.valueHolder.Increment();
    public object Decrement() => this.valueHolder.Decrement();

    public string ColorName => color == null ? Name : color.Value.Colorize(Name);

    public List<OptionHolder> GetHoldersRecursive() {
        List<OptionHolder> holders = new();
        holders.Add(this);
        holders.AddRange(this.SubOptions.SelectMany(opt => opt.GetHoldersRecursive()));
        return holders;
    }

    public List<OptionBehaviour> CreateBehaviours(StringOption template, Transform parent)
    {
        List<OptionBehaviour> behaviours = new();
        if (this.Behaviour == null)
        {
            this.Behaviour = Object.Instantiate(template, parent);
            behaviours.Add(this.Behaviour);
        }
        this.Behaviour.name = ColorName;
        this.Behaviour.gameObject.SetActive(true);
        this.Behaviour.TitleText.text = ColorName;
        this.Behaviour.ValueText.text = valueHolder.GetAsString();
        this.Behaviour.Value = 0;
        this.Behaviour.transform.FindChild("Background").localScale = new Vector3(1.2f, 1f, 1f);
        this.Behaviour.transform.FindChild("Plus_TMP").localPosition += new Vector3(0.3f, 0f, 0f);
        this.Behaviour.transform.FindChild("Minus_TMP").localPosition += new Vector3(0.3f, 0f, 0f);
        this.Behaviour.transform.FindChild("Value_TMP").localPosition += new Vector3(0.3f, 0f, 0f);
        this.Behaviour.transform.FindChild("Title_TMP").localPosition += new Vector3(0.15f, 0f, 0f);
        this.Behaviour.transform.FindChild("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(3.5f, 0.37f);
        this.Behaviour.FixedUpdate();
        this.Behaviour.gameObject.SetActive(false);
        behaviours.AddRange(SubOptions.SelectMany(opt => opt.CreateBehaviours(template, parent)));
        return behaviours;
    }

    public List<OptionHolder> EnabledOptions(bool forceShow = false)
    {
        List<OptionHolder> holders = new();
        holders.Add(this);
        Behaviour.gameObject.SetActive(false);
        if (!forceShow && ShowOptionPredicate != null && !ShowOptionPredicate.Invoke(GetValue()))
        {
            GetHoldersRecursive().Do(holder => holder?.Behaviour?.gameObject?.SetActive(false));
            return holders;
        }

        holders.AddRange(SubOptions.SelectMany(opt => opt.EnabledOptions()));
        return holders;
    }

    public override string ToString() => $"OptionsHolder({Name}: {valueHolder.GetAsString()} => {SubOptions.PrettyString()})";
}
 public class SmartOptionBuilder
    {
        private string name;
        private string header;
        private GameOptionTab tab;
        private object key;
        private Action<object> lateBinding;
        private bool isHeader;
        private int valueIndex = -1;
        private int level;
        private Color? color;
        private List<Func<string[], OptionValue>> values = new();
        public List<Func<string, OptionHolder[]>> SubOptions = new();
        private Func<object, bool> showOptionsPredicate;
        //private OptionPage page;

        public SmartOptionBuilder(string header = null, int level = 0)
        {
            this.header = header;
            this.level = level;
        }

        public SmartOptionBuilder Name(string name)
        {
            this.name = name;
            return this;
        }

        public SmartOptionBuilder Color(Color color)
        {
            this.color = color;
            return this;
        }

        public SmartOptionBuilder IsHeader(bool isHeader)
        {
            this.isHeader = isHeader;
            return this;
        }

        public SmartOptionBuilder Tab(GameOptionTab tab)
        {
            this.tab = tab;
            return this;
        }

        public SmartOptionBuilder ShowSubOptionsWhen(Func<object, bool> predicate)
        {
            showOptionsPredicate = predicate;
            return this;
        }

        public SmartOptionBuilder AddValue(object value, string prefix = "", string suffix = "", Color? color = null)
        {
            this.values.Add(v => new OptionValue(value, cfgHeader: v[0], cfgEntry: v[1], null, prefix, suffix, color));
            return this;
        }

        public SmartOptionBuilder AddValue(Func<OptionValue.Builder, OptionValue> value)
        {
            this.values.Add(v => value.Invoke(OptionValue.ToBuilder(null, cfgHeader: v[0], cfgEntry: v[1])));
            return this;
        }

        public SmartOptionBuilder AddValues(int startIndex = -1, params object[] values)
        {
            this.valueIndex = startIndex != -1 ? startIndex : this.valueIndex;
            this.values.AddRange(values.Select(v => (Func<string[], OptionValue>)(s => new OptionValue(v, cfgHeader: s[0], cfgEntry: s[1]))));
            return this;
        }

        public SmartOptionBuilder AddValues(int startIndex = -1, params OptionValue[] values)
        {
            this.valueIndex = startIndex != -1 ? startIndex : this.valueIndex;
            this.values.AddRange(values.Select(v => (Func<string[], OptionValue>)(_ => v)));
            return this;
        }

        public SmartOptionBuilder AddValues(Range range, int startIndex = 0, string prefix = "", string suffix = "", Color? color = null)
        {
            this.valueIndex = startIndex != -1 ? startIndex : this.valueIndex;
            range.ToEnumerator().Do(i => this.AddValue(i, prefix, suffix, color));
            return this;
        }

        public SmartOptionBuilder AddSubOption(Func<SmartOptionBuilder, OptionHolder> subOptionBuilder)
        {
            this.SubOptions.Add(h =>  new [] {subOptionBuilder.Invoke(new SmartOptionBuilder(h, level + 1))});
            return this;
        }

        public SmartOptionBuilder AddFloatRangeValues(float start, float stop, float step = 1, int startIndex = -1, string suffix = "")
        {
            this.valueIndex = startIndex != -1 ? startIndex : this.valueIndex;
            this.values.AddRange(new FloatRangeGenerator(start, stop, step).GetRange().Select(v => (Func<string[], OptionValue>)(s => new OptionValue(v, cfgHeader: s[0], cfgEntry: s[1], suffix: suffix))));
            return this;
        }

        public SmartOptionBuilder AddIntRangeValues(int start, int stop, int step = 1, int startIndex = -1, string suffix = "")
        {
            this.valueIndex = startIndex != -1 ? startIndex : this.valueIndex;
            this.values.AddRange(new IntRangeGenerator(start, stop, step).GetRange().Select(v => (Func<string[], OptionValue>)(s => new OptionValue(v, cfgHeader: s[0], cfgEntry: s[1], suffix: suffix))));
            return this;
        }

        /// <summary>
        /// Lazy method to add ON and OFF values to this option
        /// </summary>
        /// <returns>Current builder</returns>
        public SmartOptionBuilder AddOnOffValues(bool showOnFirst = true)
        {
            return this
                .AddValue(val =>
                    val.Text(showOnFirst ? "ON" : "OFF")
                        .Value(showOnFirst)
                        .Color(showOnFirst ? UnityEngine.Color.cyan : UnityEngine.Color.red)
                        .Build())
                .AddValue(val =>
                    val.Text(showOnFirst ? "OFF" : "ON")
                        .Value(!showOnFirst)
                        .Color(showOnFirst ? UnityEngine.Color.red : UnityEngine.Color.cyan)
                        .Build());
        }

        public SmartOptionBuilder Bind(object key)
        {
            this.key = key;
            return this;
        }

        public SmartOptionBuilder Bind(Action<object> lateBinding)
        {
            this.lateBinding = lateBinding;
            return this;
        }

        public SmartOptionBuilder BindInt(Action<int> lateBinding)
        {
            this.lateBinding = obj => lateBinding.Invoke((int)obj);
            return this;
        }

        public SmartOptionBuilder BindBool(Action<bool> lateBinding)
        {
            this.lateBinding = obj => lateBinding.Invoke((bool)obj);
            return this;
        }

        public SmartOptionBuilder BindFloat(Action<float> lateBinding)
        {
            this.lateBinding = obj => lateBinding.Invoke((float)obj);
            return this;
        }

        /*public SmartOptionBuilder Page(OptionPage page)
        {
            this.page = page;
            return this;
        }*/

        public OptionHolder Build(bool createHeaderIfNull = true)
        {
            this.header ??= (!createHeaderIfNull ? this.header : (this.name + "Options"));
            OptionValueHolder valueHolder = new(this.values.Select(value => value.Invoke(new[] { this.header, this.name })).ToList());
            if (this.key != null)
                Main.OptionManager.BindValueHolder(this.key, valueHolder);
            if (this.lateBinding != null)
                valueHolder.LateBinding = this.lateBinding;
            if (valueIndex != -1 && valueHolder.Index == -1)
                valueHolder.Index = valueIndex;
            return new OptionHolder
            {
                Name = this.name,
                Header = this.header,
                Tab = this.tab,
                color =  this.color,
                IsHeader = this.isHeader,
                valueHolder = valueHolder,
                Level = level,
                ShowOptionPredicate = this.showOptionsPredicate,
                SubOptions = this.SubOptions.SelectMany(opt => opt.Invoke(this.header)).ToList(),
                //Page = this.page
            };
        }

        public SmartOptionBuilder Clone()
        {
            SmartOptionBuilder builder = (SmartOptionBuilder)this.MemberwiseClone();
            builder.SubOptions = new List<Func<string, OptionHolder[]>>(builder.SubOptions);
            builder.values = new List<Func<string[], OptionValue>>(builder.values);
            return builder;
        }
    }