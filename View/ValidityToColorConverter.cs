using ShaderBaker.GlRenderer;
using System;
using System.Globalization;

namespace ShaderBaker.View
{

class ValidityToColorConverter : NoConvertBackValueConverter
{
    public override object Convert(
        object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!(value is Validity))
        {
            return null;
        }

        Validity validity = (Validity) value;
        switch (validity)
        {
            case Validity.Unknown:
                return "Yellow";
            case Validity.Invalid:
                return "Red";
            case Validity.Valid:
                return "Green";
            default:
                throw new NotImplementedException(
                    "Missing switch branch for validity: " + validity.ToString());
        }
    }
}

}
