using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    return typeof(EdmBinaryTypeReference);
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
                case EdmConstants.EdmTimeOfDayTypeName:
                    return typeof(TimeOfDay);
                default:
                    throw new NotSupportedException(string.Format("Unsupported type: {0}", type.FullTypeName()));
            }
        }
    }
}
