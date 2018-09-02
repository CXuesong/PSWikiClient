using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using WikiClientLibrary.Wikibase.DataTypes;

namespace PSWikiClient.Wikibase
{

    [TypeConverter(typeof(WbMonolingualText))]
    public class WbMonolingualTextConverter : TypeConverter
    {
        /// <inheritdoc />
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;
            return false;
        }

        /// <inheritdoc />
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string)) return true;
            return false;
        }

        /// <inheritdoc />
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null) return WbMonolingualText.Null;
            if (value is string s)
            {
                int pos;
                pos = s.LastIndexOf('@');
                if (pos < 0 || pos == s.Length - 1) throw new FormatException("Monolingual text expression should be in the form of text@language.");
                return new WbMonolingualText(s.Substring(pos + 1), s.Substring(0, pos));
            }
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            var src = (WbMonolingualText)value;
            if (destinationType == typeof(string))
            {
                return src.Text + "@" + src.Language;
            }
            throw new NotSupportedException();
        }
    }
}
