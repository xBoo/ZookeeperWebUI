using System;
using System.ComponentModel;

namespace ZooKeeper.Mgt.Website.Common
{
    public static class EnumExtensions
    {
        public static string ToDescription(this System.Enum enumeration)
        {
            var enumType = enumeration.GetType();
            string name = System.Enum.GetName(enumType, enumeration);
            if (name == null) return null;

            var fieldInfo = enumType.GetField(name);
            if (fieldInfo == null) return null;

            var attr = Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute), false) as DescriptionAttribute;
            return attr?.Description;
        }

        public static int ToInt(this System.Enum enumeration)
        {
            return Convert.ToInt32(enumeration);
        }

        public static T ToEnum<T>(this string val)
        {
            return (T)Enum.Parse(typeof(T), val);
        }
    }
}