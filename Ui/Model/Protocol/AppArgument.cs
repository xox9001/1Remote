﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using _1RM.Service;
using _1RM.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shawn.Utils;
using Shawn.Utils.Interface;
using Shawn.Utils.Wpf;
using Shawn.Utils.Wpf.FileSystem;

namespace _1RM.Model.Protocol;

public enum AppArgumentType
{
    Normal,
    Int,
    /// <summary>
    /// e.g. -f X:\makefile
    /// </summary>
    File,
    Secret,
    /// <summary>
    /// e.g. --hide
    /// </summary>
    Flag,
    Selection,
}

public class AppArgument : NotifyPropertyChangedBase, ICloneable, IDataErrorInfo
{
    public AppArgument(bool? isEditable = true)
    {
        IsEditable = isEditable;
    }

    /// <summary>
    /// todo 批量编辑时，如果参数列表不同，禁用
    /// </summary>
    [JsonIgnore]
    public bool? IsEditable { get; }


    private AppArgumentType _type;
    [JsonConverter(typeof(StringEnumConverter))]
    public AppArgumentType Type
    {
        get => _type;
        set
        {
            if (SetAndNotifyIfChanged(ref _type, value))
            {
                // TODO reset value when type is changed
            }
        }
    }

    private bool _isNullable = true;
    public bool IsNullable
    {
        get => _isNullable;
        set
        {
            SetAndNotifyIfChanged(ref _isNullable, value);
            RaisePropertyChanged(nameof(HintDescription));
        }
    }

    private string _name = "";
    public string Name
    {
        get => _name.Trim();
        set => SetAndNotifyIfChanged(ref _name, value.Trim());
    }

    private string _key = "";
    [DefaultValue("")]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    public string Key
    {
        get => _key.Trim();
        set
        {
            if (SetAndNotifyIfChanged(ref _key, value.Trim()))
                RaisePropertyChanged(nameof(DemoArgumentString));
        }
    }



    private bool _addBlankAfterKey = true;
    /// <summary>
    /// argument like "sftp://%USERNAME%:%PASSWORD%@%HOSTNAME%:%PORT%" need it to be false
    /// </summary>
    [DefaultValue(true)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    public bool AddBlankAfterKey
    {
        get => _addBlankAfterKey;
        set => SetAndNotifyIfChanged(ref _addBlankAfterKey, value);
    }

    private string _value = "";
    public string Value
    {
        get
        {
            if (Type == AppArgumentType.Selection
                && !_selections.Keys.Contains(_value))
            {
                _value = _selections.Keys.FirstOrDefault() ?? "";
            }
            return _value.Trim();
        }
        set
        {
            if (SetAndNotifyIfChanged(ref _value, value.Trim()))
            {
                RaisePropertyChanged(nameof(DemoArgumentString));
                if (Type == AppArgumentType.Selection)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        if (IsNullable == false)
                        {
                            _value = _selections.Keys.FirstOrDefault() ?? "";
                        }
                    }
                    else if (!_selections.Keys.Contains(_value))
                    {
                        _value = _selections.Keys.FirstOrDefault() ?? "";
                    }
                }
            }
        }
    }

    private bool _addBlankAfterValue = true;
    /// <summary>
    /// argument like "sftp://%USERNAME%:%PASSWORD%@%HOSTNAME%:%PORT%" need it to be false
    /// </summary>
    [DefaultValue(true)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    public bool AddBlankAfterValue
    {
        get => _addBlankAfterValue;
        set => SetAndNotifyIfChanged(ref _addBlankAfterValue, value);
    }

    private string _description = "";
    public string Description
    {
        get => _description.Trim();
        set
        {
            SetAndNotifyIfChanged(ref _description, value.Trim());
            RaisePropertyChanged(nameof(HintDescription));
        }
    }

    [JsonIgnore] public string HintDescription => IsNullable ? "(optional)" + _description : _description;



    private Dictionary<string, string> _selections = new Dictionary<string, string>();
    public Dictionary<string, string> Selections
    {
        get => _selections;
        set
        {
            var n = new Dictionary<string, string>();
            var auto = value.Where(x => x.Key.Trim() != "").ToList();
            if (IsNullable)
            {
                n.Add("", "");
            }
            if (Type == AppArgumentType.Selection)
            {
                if (auto.Any() == false)
                {
                    n.Add("", "null");
                }
            }
            if (auto.Any())
            {
                foreach (var keyValuePair in auto)
                {
                    var v = keyValuePair.Value.Trim();
                    if (string.IsNullOrEmpty(v))
                    {
                        v = keyValuePair.Key.Trim();
                    }
                    n.Add(keyValuePair.Key.Trim(), v);
                }
                if (n.All(x => x.Key != Value))
                    Value = n.First().Value;
            }
            _selections = n;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(SelectionKeys));
        }
    }

    [JsonIgnore] public List<string> SelectionKeys => Selections.Keys.ToList();

    public object Clone()
    {
        var copy = (AppArgument)MemberwiseClone();
        copy.Selections = new System.Collections.Generic.Dictionary<string, string>(this.Selections);
        return copy;
    }

    public string DemoArgumentString => GetArgumentString(true);

    public string GetArgumentString(bool forDemo = false)
    {
        if (Type == AppArgumentType.Flag)
        {
            return Value == "1" ? Key : "";
        }

        if (string.IsNullOrEmpty(Value) && !IsNullable)
        {
            return "";
        }

        // REPLACE %xxx% with SystemEnvironment, 替换系统环境变量
        var value = Environment.ExpandEnvironmentVariables(Value);
        if (Type == AppArgumentType.Secret && !string.IsNullOrEmpty(Value))
        {
            if (forDemo)
            {
                value = "******";
            }
            else
            {
                value = UnSafeStringEncipher.DecryptOrReturnOriginalString(Value);
            }
        }
        if (value.IndexOf(" ", StringComparison.Ordinal) > 0)
            value = $"\"{value}\"";
        if (!string.IsNullOrEmpty(Key))
        {
            value = $"{Key}{(AddBlankAfterKey ? " " : "")}{value}{(AddBlankAfterValue ? " " : "")}";
        }
        return value;
    }

    public static string GetArgumentsString(IEnumerable<AppArgument> arguments, bool isDemo)
    {
        string cmd = "";
        foreach (var argument in arguments)
        {
            cmd += argument.GetArgumentString(isDemo);
        }
        return cmd.Trim();
    }

    private RelayCommand? _cmdSelectArgumentFile;
    [JsonIgnore]
    public RelayCommand CmdSelectArgumentFile
    {
        get
        {
            return _cmdSelectArgumentFile ??= new RelayCommand((o) =>
            {
                string initPath;
                try
                {
                    initPath = new FileInfo(o?.ToString() ?? "").DirectoryName!;
                }
                catch (Exception)
                {
                    initPath = Environment.CurrentDirectory;
                }
                var path = SelectFileHelper.OpenFile(initialDirectory: initPath, currentDirectoryForShowingRelativePath: Environment.CurrentDirectory);
                if (path == null) return;
                Value = path;
            });
        }
    }


    public static Tuple<bool, string> CheckName(List<AppArgument> argumentList, string name)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return new Tuple<bool, string>(false, $"`{IoC.Get<ILanguageService>().Translate(LanguageService.NAME)}` {IoC.Get<ILanguageService>().Translate(LanguageService.CAN_NOT_BE_EMPTY)}");
        }

        if (argumentList?.Any(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase)) == true)
        {
            return new Tuple<bool, string>(false, IoC.Get<ILanguageService>().Translate(LanguageService.XXX_IS_ALREADY_EXISTED, name));
        }

        return new Tuple<bool, string>(true, "");
    }

    public static Tuple<bool, string> CheckKey(List<AppArgument> argumentList, string key)
    {
        key = key.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            return new Tuple<bool, string>(true, "");
        }

        if (argumentList?.Any(x => string.Equals(x.Key, key, StringComparison.CurrentCultureIgnoreCase)) == true)
        {
            return new Tuple<bool, string>(false, IoC.Get<ILanguageService>().Translate(LanguageService.XXX_IS_ALREADY_EXISTED, key));
        }

        return new Tuple<bool, string>(true, "");
    }

    public static Tuple<bool, string> CheckValue(string value, bool isNullable, AppArgumentType type)
    {
        if (!isNullable && string.IsNullOrWhiteSpace(value))
        {
            return new Tuple<bool, string>(false, IoC.Get<ILanguageService>().Translate(LanguageService.CAN_NOT_BE_EMPTY));
        }

        if (type == AppArgumentType.File
            && !File.Exists(value))
        {
            return new Tuple<bool, string>(false, "TXT: not existed");
        }

        if (type == AppArgumentType.Int
            && !int.TryParse(value, out _))
        {
            return new Tuple<bool, string>(false, "TXT: not a number");
        }

        return new Tuple<bool, string>(true, "");
    }

    public bool IsValueEqualTo(in AppArgument newValue)
    {
        if (this.Type != newValue.Type) return false;
        if (this.Name != newValue.Name) return false;
        if (this.Key != newValue.Key) return false;
        if (this.Value != newValue.Value) return false;
        if (this.Selections.Count != Selections.Count) return false;
        foreach (var selection in Selections)
        {
            if (!newValue.Selections.Contains(selection)) return false;
        }
        return true;
    }

    #region IDataErrorInfo
    [JsonIgnore] public string Error => "";

    [JsonIgnore]
    public string this[string columnName]
    {
        get
        {
            switch (columnName)
            {
                case nameof(Value):
                    {
                        var t = CheckValue(Value, IsNullable, Type);
                        if (t.Item1 == false)
                        {
                            return string.IsNullOrEmpty(t.Item2) ? "error" : t.Item2;
                        }
                        break;
                    }
            }
            return "";
        }
    }
    #endregion
}