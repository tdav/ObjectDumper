using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable once CheckNamespace
public class DumpOptions
{
    public DumpOptions()
    {
        this.DumpStyle = DumpStyle.Console;
        this.IndentSize = 2;
        this.IndentChar = ' ';
        this.LineBreakChar = Environment.NewLine;
        this.SetPropertiesOnly = false;
        this.MaxLevel = int.MaxValue;
        this.ExcludeProperties = new HashSet<string>();
        this.PropertyOrderBy = null;
        this.IgnoreDefaultValues = false;
        this.IgnoreIndexers = true;
        this.CustomTypeFormatter = new Dictionary<Type, Func<Type, string>>();
        this.CustomInstanceFormatters = new CustomInstanceFormatters();
        this.TrimInitialVariableName = false;
        this.UseTypeFullName = false;
        this.OnlyValues = false;
        this.NullValue = "";
        this.ForWeb = true;
    }

    public bool OnlyValues { get;set; }
    public bool ForWeb { get;set; }
    public string NullValue { get;set; }

    public DumpStyle DumpStyle { get; set; }

    public int IndentSize { get; set; }

    public char IndentChar { get; set; }

    public string LineBreakChar { get; set; }

    public bool SetPropertiesOnly { get; set; }

    public int MaxLevel { get; set; }

    public ICollection<string> ExcludeProperties { get; set; }

    public IDictionary<Type, Func<Type, string>> CustomTypeFormatter { get; set; }

    public Expression<Func<PropertyInfo, object>> PropertyOrderBy { get; set; }

    /// <summary>
    /// Ignores default values if set to <c>true</c>.
    /// Default: <c>false</c>
    /// </summary>
    public bool IgnoreDefaultValues { get; set; }

    /// <summary>
    /// Ignores index properties if set to <c>true</c>.
    /// Default: <c>true</c>
    /// </summary>
    public bool IgnoreIndexers { get; set; }

    public bool TrimInitialVariableName { get; set; }

    public bool TrimTrailingColonName { get; set; }

    public CustomInstanceFormatters CustomInstanceFormatters { get; }

    public bool UseTypeFullName { get; set; }
}

public class CustomInstanceFormatters
{
    private readonly Dictionary<Type, CustomInstanceFormatter> customFormatters = new Dictionary<Type, CustomInstanceFormatter>();

    public void AddFormatter<T>(Func<T, string> formatInstance)
    {
        this.customFormatters.Add(typeof(T), new CustomInstanceFormatter(typeof(T), o => formatInstance((T)o)));
    }

    public bool HasFormatterFor<T>()
    {
        return this.customFormatters.ContainsKey(typeof(T));
    }

    public bool HasFormatterFor(object obj)
    {
        return this.customFormatters.ContainsKey(obj.GetType());
    }

    public bool TryGetFormatter(Type type, out Func<object, string> formatter)
    {
        if (this.customFormatters.TryGetValue(type, out var customInstanceFormatter))
        {
            formatter = customInstanceFormatter.Formatter;
            return true;
        }

        formatter = null;
        return false;
    }

    public void Clear()
    {
        this.customFormatters.Clear();
    }

    public void RemoveFormatter<T>()
    {
        this.RemoveFormatter(typeof(T));
    }

    public void RemoveFormatter(Type type)
    {
        this.customFormatters.Remove(type);
    }

    private class CustomInstanceFormatter
    {
        public CustomInstanceFormatter(Type type, Func<object, string> formatter)
        {
            this.InstanceType = type;
            this.Formatter = formatter;
        }

        public Func<object, string> Formatter { get; }

        public Type InstanceType { get; }
    }
}
