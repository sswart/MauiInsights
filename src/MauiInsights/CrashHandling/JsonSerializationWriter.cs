using Microsoft.ApplicationInsights.Extensibility;
using System.Globalization;

namespace MauiInsights.CrashHandling
{
    internal class JsonSerializationWriter : ISerializationWriter
    {
        private readonly TextWriter textWriter;
        private bool currentObjectHasProperties;

        public JsonSerializationWriter(TextWriter textWriter)
        {
            this.textWriter = textWriter;
        }

        /// <inheritdoc/>
        public void WriteStartObject()
        {
            textWriter.Write('{');
            currentObjectHasProperties = false;
        }

        /// <inheritdoc/>
        public void WriteStartObject(string name)
        {
            WritePropertyName(name);
            textWriter.Write('{');
            currentObjectHasProperties = false;
        }

        /// <inheritdoc/>
        public void WriteProperty(string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                WritePropertyName(name);
                WriteString(value);
            }
        }

        /// <inheritdoc/>
        public void WriteProperty(string name, int? value)
        {
            if (value.HasValue)
            {
                WritePropertyName(name);
                textWriter.Write(value.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <inheritdoc/>
        public void WriteProperty(string name, bool? value)
        {
            if (value.HasValue)
            {
                WritePropertyName(name);
                textWriter.Write(value.Value ? "true" : "false");
            }
        }

        /// <inheritdoc/>
        public void WriteProperty(string name, double? value)
        {
            if (value.HasValue)
            {
                WritePropertyName(name);
                textWriter.Write(value.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <inheritdoc/>
        public void WriteProperty(string name, TimeSpan? value)
        {
            if (value.HasValue)
            {
                WriteProperty(name, value.Value.ToString(string.Empty, CultureInfo.InvariantCulture));
            }
        }

        /// <inheritdoc/>
        public void WriteProperty(string name, DateTimeOffset? value)
        {
            if (value.HasValue)
            {
                WriteProperty(name, value.Value.ToString("o", CultureInfo.InvariantCulture));
            }
        }

        /// <inheritdoc/>
        public void WriteProperty(string name, IList<string> items)
        {
            bool commaNeeded = false;
            if (items != null && items.Count > 0)
            {
                WritePropertyName(name);

                WriteStartArray();

                foreach (var item in items)
                {
                    if (commaNeeded)
                    {
                        WriteComma();
                    }

                    WriteString(item);
                    commaNeeded = true;
                }

                WriteEndArray();
            }
        }

        /// <inheritdoc/>
        public void WriteProperty(string name, IList<ISerializableWithWriter> items)
        {
            bool commaNeeded = false;
            if (items != null && items.Count > 0)
            {
                WritePropertyName(name);
                WriteStartArray();
                foreach (var item in items)
                {
                    if (commaNeeded)
                    {
                        WriteComma();
                    }

                    WriteStartObject();
                    item.Serialize(this);
                    commaNeeded = true;
                    WriteEndObject();
                }

                WriteEndArray();
            }
        }

        /// <inheritdoc/>
        public void WriteProperty(string name, ISerializableWithWriter value)
        {
            if (value != null)
            {
                WriteStartObject(name);
                value.Serialize(this);
                WriteEndObject();
            }
        }

        /// <inheritdoc/>
        public void WriteProperty(ISerializableWithWriter value)
        {
            if (value != null)
            {
                value.Serialize(this);
            }
        }

        /// <inheritdoc/>
        public void WriteProperty(string name, IDictionary<string, double> values)
        {
            if (values != null && values.Count > 0)
            {
                WritePropertyName(name);
                WriteStartObject();
                foreach (KeyValuePair<string, double> item in values)
                {
                    WriteProperty(item.Key, item.Value);
                }

                WriteEndObject();
            }
        }

        /// <inheritdoc/>
        public void WriteProperty(string name, IDictionary<string, string> values)
        {
            if (values != null && values.Count > 0)
            {
                WritePropertyName(name);
                WriteStartObject();
                foreach (KeyValuePair<string, string> item in values)
                {
                    WriteProperty(item.Key, item.Value);
                }

                WriteEndObject();
            }
        }

        /// <inheritdoc/>
        public void WriteEndObject()
        {
            textWriter.Write('}');
        }

        internal void WritePropertyName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException($"{nameof(name)} cannot be empty", nameof(name));
            }

            if (currentObjectHasProperties)
            {
                textWriter.Write(',');
            }
            else
            {
                currentObjectHasProperties = true;
            }

            WriteString(name);
            textWriter.Write(':');
        }

        internal void WriteStartArray()
        {
            textWriter.Write('[');
        }

        internal void WriteEndArray()
        {
            textWriter.Write(']');
        }

        internal void WriteComma()
        {
            textWriter.Write(',');
        }

        internal void WriteRawValue(object value)
        {
            textWriter.Write(string.Format(CultureInfo.InvariantCulture, "{0}", value));
        }

        internal void WriteString(string value)
        {
            textWriter.Write('"');

            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                        textWriter.Write("\\\\");
                        break;
                    case '"':
                        textWriter.Write("\\\"");
                        break;
                    case '\n':
                        textWriter.Write("\\n");
                        break;
                    case '\b':
                        textWriter.Write("\\b");
                        break;
                    case '\f':
                        textWriter.Write("\\f");
                        break;
                    case '\r':
                        textWriter.Write("\\r");
                        break;
                    case '\t':
                        textWriter.Write("\\t");
                        break;
                    default:
                        if (!char.IsControl(c))
                        {
                            textWriter.Write(c);
                        }
                        else
                        {
                            textWriter.Write(@"\u");
                            textWriter.Write(((ushort)c).ToString("x4", CultureInfo.InvariantCulture));
                        }

                        break;
                }
            }

            textWriter.Write('"');
        }
    }
}
