using System;
using Microsoft.OData.Edm;

namespace OESoftware.Hosted.OData.Api
{
    public class KeyParser
    {
        public object FromString(string keyAsString, IEdmType type)
        {
            object value = null;
            switch (type.FullTypeName())
            {
                case EdmConstants.EdmInt16TypeName:
                    {
                        value = Int16.Parse(keyAsString);
                        break;
                    }
                case EdmConstants.EdmInt32TypeName:
                    {
                        value = Int32.Parse(keyAsString);
                        break;
                    }
                case EdmConstants.EdmInt64TypeName:
                    {
                        value = Int64.Parse(keyAsString);
                        break;
                    }
                case EdmConstants.EdmDecimalTypeName:
                    {
                        value = decimal.Parse(keyAsString);
                        break;
                    }
                case EdmConstants.EdmDoubleTypeName:
                    {
                        value = double.Parse(keyAsString);
                        break;
                    }
                case EdmConstants.EdmGuidTypeName:
                    {
                        value = Guid.Parse(keyAsString);
                        break;
                    }
                case EdmConstants.EdmSingleTypeName:
                    {
                        value = Single.Parse(keyAsString);
                        break;
                    }
                default:
                    {
                        throw new ApplicationException(string.Format("Unable to parse key {0}", keyAsString));
                    }
            }

            return value;
        }
    }
}