using System;

namespace MD.Framework.Utility
{
    internal static class ExtensionMethods
    {
        public static bool IsNumericType(this Type type)
        {
            if (type == null)
            {
                return false;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                case TypeCode.Object:
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return IsNumericType(Nullable.GetUnderlyingType(type));
                    }
                    return false;
            }
            return false;
        }
        public static bool IsBoolean(this Type type)
        {
            if (type == null)
            {
                return false;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return true;
                case TypeCode.Object:
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return IsBoolean(Nullable.GetUnderlyingType(type));
                    }
                    return false;
            }
            return false;
        }
		public static string ToPersianNumber(this string inputString, bool extractEnglishNumber = false)
		{
			if (string.IsNullOrEmpty(inputString)) return string.Empty;

			//۰ ۱ ۲ ۳ ۴ ۵ ۶ ۷ ۸ ۹
			inputString =
				inputString.Replace("0", "۰")
					.Replace("1", "۱")
					.Replace("2", "۲")
					.Replace("3", "۳")
					.Replace("4", "۴")
					.Replace("5", "۵")
					.Replace("6", "۶")
					.Replace("7", "۷")
					.Replace("8", "۸")
					.Replace("9", "۹");

			return inputString;
		}
		public static string ToEnglishNumber(this string input)
		{
			if (string.IsNullOrEmpty(input)) return string.Empty;

			//۰ ۱ ۲ ۳ ۴ ۵ ۶ ۷ ۸ ۹
			return input.Replace(",", "")
				.Replace("۰", "0")
				.Replace("۱", "1")
				.Replace("۲", "2")
				.Replace("۳", "3")
				.Replace("۴", "4")
				.Replace("۵", "5")
				.Replace("۶", "6")
				.Replace("۷", "7")
				.Replace("۸", "8")
				.Replace("۹", "9");
		}
    }
}