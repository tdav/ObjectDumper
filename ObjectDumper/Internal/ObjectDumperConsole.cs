using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ObjectDumping.Internal
{
    internal class ObjectDumperConsole : DumperBase
    {
        public PropertyAndValue Current { get; private set; }

        public ObjectDumperConsole(DumpOptions dumpOptions) : base(dumpOptions) { }

        public static string Dump(object element, DumpOptions dumpOptions = null)
        {
            if (dumpOptions == null)
            {
                dumpOptions = new DumpOptions();
            }

            var instance = new ObjectDumperConsole(dumpOptions);

            instance.FormatValue(element);

            return instance.ToString();
        }

        private void CreateObject(object o, int intentLevel = 0)
        {
            this.AddAlreadyTouched(o);

            var type = o.GetType();
            var typeName = type.IsAnonymous() ? "AnonymousObject" : type.GetFormattedName(this.DumpOptions.UseTypeFullName);

            if (!this.DumpOptions.OnlyValues)
            {
                this.Write($"{{{typeName}}}", intentLevel);
            }

            this.LineBreak();
            this.Level++;

            var properties = type.GetRuntimeProperties()
                .Where(p => p.GetMethod != null && p.GetMethod.IsPublic && p.GetMethod.IsStatic == false)
                .ToList();

            if (this.DumpOptions.ExcludeProperties != null && this.DumpOptions.ExcludeProperties.Any())
            {
                properties = properties
                    .Where(p => !this.DumpOptions.ExcludeProperties.Contains(p.Name))
                    .ToList();
            }

            if (this.DumpOptions.SetPropertiesOnly)
            {
                properties = properties
                    .Where(p => p.SetMethod != null && p.SetMethod.IsPublic && p.SetMethod.IsStatic == false)
                    .ToList();
            }

            if (this.DumpOptions.PropertyOrderBy != null)
            {
                properties = properties.OrderBy(this.DumpOptions.PropertyOrderBy.Compile())
                    .ToList();
            }

            var propertiesAndValues = properties
                  .Select(p => new PropertyAndValue(o, p))
                  .ToList();

            PropertyAndValue lastProperty;
            if (this.DumpOptions.IgnoreDefaultValues)
            {
                lastProperty = propertiesAndValues.LastOrDefault(pv => !pv.IsDefaultValue);
            }
            else
            {
                lastProperty = propertiesAndValues.LastOrDefault();
            }

            foreach (var propertiesAndValue in propertiesAndValues)
            {
                this.Current = propertiesAndValue;

                var value = propertiesAndValue.Value;

                if (this.AlreadyTouched(value))
                {
                    if (!this.DumpOptions.OnlyValues)
                    {
                        this.Write($"{propertiesAndValue.Property.Name}: ");
                    }

                    this.FormatValue(propertiesAndValue.DefaultValue);
                    this.Write(" --> Circular reference detected");
                    if (!Equals(propertiesAndValue, lastProperty))
                    {
                        this.LineBreak();
                    }
                    continue;
                }

                if (this.DumpOptions.IgnoreDefaultValues)
                {
                    if (propertiesAndValue.IsDefaultValue)
                    {
                        continue;
                    }
                }

                var indexParameters = propertiesAndValue.Property.GetIndexParameters();
                if (indexParameters.Length > 0)
                {
                    if (!this.DumpOptions.IgnoreIndexers)
                    {
                        this.DumpIntegerArrayIndexer(o, propertiesAndValue.Property, indexParameters);
                    }
                }
                else
                {
                    if (!this.DumpOptions.OnlyValues)
                    {
                        this.Write($"{propertiesAndValue.Property.Name}: ");
                    }

                    this.FormatValue(value);
                    if (!Equals(propertiesAndValue, lastProperty))
                    {
                        this.LineBreak();
                    }
                }
            }

            this.Level--;
        }

        private void DumpIntegerArrayIndexer(object o, PropertyInfo property, ParameterInfo[] indexParameters)
        {
            if (indexParameters.Length == 1 && indexParameters[0].ParameterType == typeof(int))
            {
                // get an integer count value
                // issues, what if it's not an integer index (Dictionary?), what if it's multi-dimensional?
                // just need to be able to iterate through each value in the indexed property
                // Source: https://stackoverflow.com/questions/4268244/iterating-through-an-indexed-property-reflection

                var arrayValues = new List<object>();
                var index = 0;
                while (true)
                {
                    try
                    {
                        arrayValues.Add(property.GetValue(o, new object[] { index }));
                        index++;
                    }
                    catch (TargetInvocationException) { break; }
                }

                var lastArrayValue = arrayValues.LastOrDefault();

                for (var arrayIndex = 0; arrayIndex < arrayValues.Count; arrayIndex++)
                {
                    var arrayValue = arrayValues[arrayIndex];

                    if (!this.DumpOptions.OnlyValues)
                    {
                        this.Write($"[{arrayIndex}]: ");
                    }

                    this.FormatValue(arrayValue);
                    if (!Equals(arrayValue, lastArrayValue))
                    {
                        this.Write($",{this.DumpOptions.LineBreakChar}");
                    }
                    else
                    {
                        this.Write($",");
                    }
                }

                this.LineBreak();
            }
        }

        protected override void FormatValue(object o, int intentLevel = 0)
        {
            if (this.IsMaxLevel())
            {
                return;
            }

            if (o == null)
            {
                this.Write(this.DumpOptions.NullValue, intentLevel);
                return;
            }

            if (o is bool)
            {
                this.Write($"{o.ToString().ToLower()}", intentLevel);
                return;
            }

            if (o is string)
            {
                var str = $@"{o}".Escape();
                if (this.DumpOptions.OnlyValues)
                {
                    this.Write(str, intentLevel);
                }
                else
                {
                    this.Write($"\"{str}\"", intentLevel);
                }
                return;
            }

            if (o is char)
            {
                var c = o.ToString().Replace("\0", "").Trim();

                if (this.DumpOptions.OnlyValues)
                {
                    this.Write($"{c}", intentLevel);
                }
                else
                {
                    this.Write($"\'{c}\'", intentLevel);
                }
                return;
            }

            if (o is byte || o is sbyte)
            {
                this.Write($"{o}", intentLevel);
                return;
            }

            if (o is short @short)
            {
                if (this.DumpOptions.OnlyValues)
                {
                    this.Write($"{@short}", intentLevel);
                }
                else
                {
                    if (@short == short.MinValue)
                    {
                        this.Write($"MinValue", intentLevel);
                    }
                    else if (@short == short.MaxValue)
                    {
                        this.Write($"MaxValue", intentLevel);
                    }
                    else
                    {
                        this.Write($"{@short.ToString(CultureInfo.InvariantCulture)}", intentLevel);
                    }
                }
                return;
            }

            if (o is ushort @ushort)
            {
                if (this.DumpOptions.OnlyValues)
                {
                    this.Write($"{@ushort}", intentLevel);
                }
                else
                {
                    if (@ushort == ushort.MaxValue)
                    {
                        this.Write($"MaxValue", intentLevel);
                    }
                    else
                    {
                        this.Write($"{@ushort.ToString(CultureInfo.InvariantCulture)}", intentLevel);
                    }
                }

                return;
            }

            if (o is int @int)
            {
                if (this.DumpOptions.OnlyValues)
                {
                    this.Write($"{@int}", intentLevel);
                }
                else
                {
                    if (@int == int.MinValue)
                    {
                        this.Write($"MinValue", intentLevel);
                    }
                    else if (@int == int.MaxValue)
                    {
                        this.Write($"MaxValue", intentLevel);
                    }
                    else
                    {
                        this.Write($"{@int.ToString(CultureInfo.InvariantCulture)}", intentLevel);
                    }
                }

                return;
            }

            if (o is uint @uint)
            {
                if (this.DumpOptions.OnlyValues)
                {
                    this.Write($"{@uint}", intentLevel);
                }
                else
                {
                    if (@uint == uint.MaxValue)
                    {
                        this.Write($"MaxValue", intentLevel);
                    }
                    else
                    {
                        this.Write($"{@uint.ToString(CultureInfo.InvariantCulture)}", intentLevel);
                    }
                }
                return;
            }

            if (o is long @long)
            {
                if (this.DumpOptions.OnlyValues)
                {
                    this.Write($"{@long}", intentLevel);
                }
                else
                {
                    if (@long == long.MinValue)
                    {
                        this.Write($"MinValue", intentLevel);
                    }
                    else if (@long == long.MaxValue)
                    {
                        this.Write($"MaxValue", intentLevel);
                    }
                    else
                    {
                        this.Write($"{@long.ToString(CultureInfo.InvariantCulture)}", intentLevel);
                    }
                }

                return;
            }

            if (o is ulong @ulong)
            {
                if (this.DumpOptions.OnlyValues)
                {
                    this.Write($"{@ulong}", intentLevel);
                }
                else
                {
                    if (@ulong == ulong.MaxValue)
                    {
                        this.Write($"MaxValue", intentLevel);
                    }
                    else
                    {
                        this.Write($"{@ulong.ToString(CultureInfo.InvariantCulture)}", intentLevel);
                    }
                }

                return;
            }

            if (o is double @double)
            {
                if (this.DumpOptions.OnlyValues)
                {
                    this.Write($"{@double}", intentLevel);
                }
                else
                {
                    if (@double == double.MinValue)
                    {
                        this.Write($"MinValue", intentLevel);
                    }
                    else if (@double == double.MaxValue)
                    {
                        this.Write($"MaxValue", intentLevel);
                    }
                    else if (double.IsNaN(@double))
                    {
                        this.Write($"NaN", intentLevel);
                    }
                    else if (double.IsPositiveInfinity(@double))
                    {
                        this.Write($"PositiveInfinity", intentLevel);
                    }
                    else if (double.IsNegativeInfinity(@double))
                    {
                        this.Write($"NegativeInfinity", intentLevel);
                    }
                    else
                    {
                        this.Write($"{@double.ToString(CultureInfo.InvariantCulture)}", intentLevel);
                    }
                }
                return;
            }

            if (o is decimal @decimal)
            {
                if (this.DumpOptions.OnlyValues)
                {
                    this.Write($"{@decimal}", intentLevel);
                }
                else
                {
                    if (@decimal == decimal.MinValue)
                    {
                        this.Write($"MinValue", intentLevel);
                    }
                    else if (@decimal == decimal.MaxValue)
                    {
                        this.Write($"MaxValue", intentLevel);
                    }
                    else
                    {
                        this.Write($"{@decimal.ToString(CultureInfo.InvariantCulture)}", intentLevel);
                    }
                }

                return;
            }

            if (o is float @float)
            {
                if (this.DumpOptions.OnlyValues)
                {
                    this.Write($"{@float}", intentLevel);
                }
                else
                {
                    if (@float == float.MinValue)
                    {
                        this.Write($"MinValue", intentLevel);
                    }
                    else if (@float == float.MaxValue)
                    {
                        this.Write($"MaxValue", intentLevel);
                    }
                    else
                    {
                        this.Write($"{@float.ToString(CultureInfo.InvariantCulture)}", intentLevel);
                    }
                }

                return;
            }

            if (o is DateTime dateTime)
            {
                if (this.DumpOptions.OnlyValues)
                {
                    if (this.DumpOptions.ForWeb)
                    {
                        this.Write($"{dateTime.ToString("yyyy.MM.dd")}T{dateTime.ToString("HH:mm:ss")}", intentLevel);
                    }
                    else
                    {
                        this.Write($"{dateTime}", intentLevel);
                    }
                }
                else
                {
                    if (dateTime == DateTime.MinValue)
                    {
                        this.Write($"DateTime.MinValue", intentLevel);
                    }
                    else if (dateTime == DateTime.MaxValue)
                    {
                        this.Write($"DateTime.MaxValue", intentLevel);
                    }
                    else
                    {

                        this.Write($"{dateTime}", intentLevel);

                    }
                }

                return;
            }

            if (o is DateTimeOffset dateTimeOffset)
            {
                if (this.DumpOptions.OnlyValues)
                {
                    this.Write($"{dateTimeOffset:O}", intentLevel);
                }
                else
                {
                    if (dateTimeOffset == DateTimeOffset.MinValue)
                    {
                        this.Write($"DateTimeOffset.MinValue", intentLevel);
                    }
                    else if (dateTimeOffset == DateTimeOffset.MaxValue)
                    {
                        this.Write($"DateTimeOffset.MaxValue", intentLevel);
                    }
                    else
                    {
                        this.Write($"{dateTimeOffset:O}", intentLevel);
                    }
                }

                return;
            }

            if (o is TimeSpan timeSpan)
            {
                if (this.DumpOptions.OnlyValues)
                {
                    this.Write($"{timeSpan:c}", intentLevel);
                }
                else
                {
                    if (timeSpan == TimeSpan.Zero)
                    {
                        this.Write($"TimeSpan.Zero", intentLevel);
                    }
                    else if (timeSpan == TimeSpan.MinValue)
                    {
                        this.Write($"TimeSpan.MinValue", intentLevel);
                    }
                    else if (timeSpan == TimeSpan.MaxValue)
                    {
                        this.Write($"TimeSpan.MaxValue", intentLevel);
                    }
                    else
                    {
                        this.Write($"{timeSpan:c}", intentLevel);
                    }
                }

                return;
            }

            if (o is CultureInfo cultureInfo)
            {
                this.Write($"{cultureInfo}", intentLevel);
                return;
            }

            var type = o.GetType();

            if (o is Enum)
            {
                var enumFlags = $"{o}".Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var enumTypeName = type.GetFormattedName(this.DumpOptions.UseTypeFullName);
                var enumValues = "";

                if (!this.DumpOptions.OnlyValues)
                {
                    // In case of multiple flags, we prefer short class name here
                    enumValues = string.Join(" | ", enumFlags.Select(f => $"{(enumFlags.Length > 1 ? "" : $"{enumTypeName}.")}{f.Replace(" ", "")}"));
                }
                else
                {
                    enumValues = string.Join(string.Empty, enumFlags.Select(f => $"{(enumFlags.Length > 1 ? "" : $"{enumTypeName}.")}{f.Replace(" ", "")}"));
                }

                this.Write($"{enumValues}", intentLevel);
                return;
            }

            if (o is Guid guid)
            {
                this.Write($"{guid:B}", intentLevel);
                return;
            }

            if (this.DumpOptions.CustomInstanceFormatters.TryGetFormatter(type, out var func))
            {
                this.Write(func(o));
                return;
            }

            if (o is Type systemType)
            {
                if (this.DumpOptions.CustomTypeFormatter.TryGetValue(systemType, out var formatter) ||
                    this.DumpOptions.CustomTypeFormatter.TryGetValue(typeof(Type), out formatter))
                {
                    this.Write(formatter(systemType));
                    return;
                }

                this.Write($"{systemType.GetFormattedName(this.DumpOptions.UseTypeFullName)}", intentLevel);
                return;
            }

            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                var kvpKey = type.GetRuntimeProperty(nameof(KeyValuePair<object, object>.Key)).GetValue(o, null);
                var kvpValue = type.GetRuntimeProperty(nameof(KeyValuePair<object, object>.Value)).GetValue(o, null);

                if (!this.DumpOptions.OnlyValues)
                {
                    this.Write("{ ", intentLevel);
                    this.FormatValue(kvpKey);
                    this.Write(", ");
                    this.FormatValue(kvpValue);
                    this.Write(" }");
                }
                else
                {
                    this.FormatValue(kvpValue);
                }

                return;
            }

#if NETSTANDARD2_0_OR_GREATER
            if (type.IsValueTuple())
            {
                this.WriteValueTuple(o, type);
                return;
            }
#endif

            if (o is IEnumerable enumerable)
            {
                if (!this.DumpOptions.OnlyValues)
                {
                    if (this.Level > 0)
                    {
                        this.Write($"...", intentLevel);
                        this.Level++;
                    }
                }
                else
                {
                    if (this.Level > 0)
                    {
                        this.Write("", intentLevel);
                        this.Level++;
                    }
                }

                this.WriteItems(enumerable);
                return;
            }

            this.CreateObject(o, intentLevel);
        }



#if NETSTANDARD2_0_OR_GREATER
        protected void WriteValueTuple(object o, Type type)
        {
            var fields = type.GetFields().ToList();
            var last = fields.LastOrDefault();

            if (this.DumpOptions.OnlyValues)
            {
                foreach (var field in fields)
                {
                    var fieldValue = field.GetValue(o);
                    this.FormatValue(fieldValue, 0);                    
                }
            }
            else
            {
                this.Write("(");
                foreach (var field in fields)
                {
                    var fieldValue = field.GetValue(o);
                    this.FormatValue(fieldValue, 0);
                    if (!Equals(field, last))
                    {
                        this.Write(", ");
                    }
                }
                this.Write(")");
            }
        }
#endif

        private void WriteItems(IEnumerable items)
        {
            if (this.IsMaxLevel())
            {
                this.Level--;
                return;
            }

            var e = items.GetEnumerator();
            if (e.MoveNext())
            {
                if (this.Level > 0)
                {
                    this.LineBreak();
                }
                this.FormatValue(e.Current, this.Level);

                while (e.MoveNext())
                {
                    this.LineBreak();

                    this.FormatValue(e.Current, this.Level);
                }

                //this.LineBreak();
            }

            if (this.Level > 0)
            {
                this.Level--;
            }
        }
    }
}
