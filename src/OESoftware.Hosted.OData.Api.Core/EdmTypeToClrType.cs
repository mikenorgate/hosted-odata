using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;

namespace OESoftware.Hosted.OData.Api.Core
{
    public static class EdmTypeToClrType
    {
        public static Type Parse(IEdmType type)
        {
            switch (type.FullTypeName())
            {
                case EdmConstants.EdmBinaryTypeName:
                    return typeof(byte[]);
                case EdmConstants.EdmBooleanTypeName:
                    return typeof(bool);
                case EdmConstants.EdmByteTypeName:
                    return typeof(byte);
                case EdmConstants.EdmDateTypeName:
                    return typeof(Date);
                case EdmConstants.EdmDateTimeOffsetTypeName:
                    return typeof(DateTimeOffset);
                case EdmConstants.EdmDecimalTypeName:
                    return typeof(Decimal);
                case EdmConstants.EdmDoubleTypeName:
                    return typeof(Double);
                case EdmConstants.EdmGuidTypeName:
                    return typeof(Guid);
                case EdmConstants.EdmSingleTypeName:
                    return typeof(Single);
                case EdmConstants.EdmSByteTypeName:
                    return typeof(SByte);
                case EdmConstants.EdmInt16TypeName:
                    return typeof(Int16);
                case EdmConstants.EdmInt32TypeName:
                    return typeof(Int32);
                case EdmConstants.EdmInt64TypeName:
                    return typeof(Int64);
                case EdmConstants.EdmStringTypeName:
                    return typeof(string);
                case EdmConstants.EdmDurationTypeName:
                    return typeof(TimeSpan);
                case EdmConstants.EdmStreamTypeName:
                    return typeof(Stream);
                case EdmConstants.EdmTimeTypeName:
                case EdmConstants.EdmTimeOfDayTypeName:
                    return typeof(TimeOfDay);
                case EdmConstants.EdmDateTimeTypeName:
                    return typeof(DateTime);
                default:
                    throw new NotSupportedException(string.Format("Unsupported type: {0}", type.FullTypeName()));
            }
        }

        public static object Default(IEdmTypeReference propertyType)
        {
            bool isCollection = propertyType.IsCollection();
            if (!propertyType.IsNullable || isCollection)
            {
                if (propertyType.IsComplex())
                {
                    return new EdmComplexObject(propertyType.Definition as IEdmComplexType);
                }

                if (propertyType.IsEnum())
                {
                    var type = propertyType.Definition as IEdmEnumType;
                    return new EdmEnumObject(type, type.Members.First().Name, false);
                }

                Type clrType = Parse(propertyType.Definition);

                if (clrType.IsArray || (isCollection && propertyType.AsCollection().ElementType().IsPrimitive()))
                {
                    return Array.CreateInstance(clrType.GetElementType(), 0);
                }

                if (clrType == typeof(string))
                {
                    return propertyType.IsNullable ? null : string.Empty;
                }
                return Activator.CreateInstance(clrType);
            }

            return null;
        }

        public static object ParseDefaultString(IEdmTypeReference propertyType, string defaultValue)
        {
            if (propertyType.IsEnum())
            {
                var type = propertyType.Definition as IEdmEnumType;
                return new EdmEnumObject(type, defaultValue, false);
            }

            switch (propertyType.Definition.FullTypeName())
            {
                case EdmConstants.EdmBooleanTypeName:
                    return bool.Parse(defaultValue);
                case EdmConstants.EdmDateTypeName:
                    return Date.Parse(defaultValue);
                case EdmConstants.EdmDateTimeOffsetTypeName:
                    return DateTimeOffset.Parse(defaultValue);
                case EdmConstants.EdmDecimalTypeName:
                    return Decimal.Parse(defaultValue);
                case EdmConstants.EdmDoubleTypeName:
                    return Double.Parse(defaultValue);
                case EdmConstants.EdmGuidTypeName:
                    return Guid.Parse(defaultValue);
                case EdmConstants.EdmSingleTypeName:
                    return Single.Parse(defaultValue);
                case EdmConstants.EdmSByteTypeName:
                    return SByte.Parse(defaultValue);
                case EdmConstants.EdmInt16TypeName:
                    return Int16.Parse(defaultValue);
                case EdmConstants.EdmInt32TypeName:
                    return Int32.Parse(defaultValue);
                case EdmConstants.EdmInt64TypeName:
                    return Int64.Parse(defaultValue);
                case EdmConstants.EdmStringTypeName:
                    return defaultValue;
                case EdmConstants.EdmDurationTypeName:
                    return TimeSpan.Parse(defaultValue);
                case EdmConstants.EdmTimeTypeName:
                case EdmConstants.EdmTimeOfDayTypeName:
                    return TimeOfDay.Parse(defaultValue);
                case EdmConstants.EdmDateTimeTypeName:
                    return DateTime.Parse(defaultValue);
                default:
                    return null;
            }

            return null;
        }
    }
}
