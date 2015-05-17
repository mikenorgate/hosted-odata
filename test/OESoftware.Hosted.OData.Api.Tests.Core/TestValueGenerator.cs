using System;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using OESoftware.Hosted.OData.Api.Db;
using EdmConstants = OESoftware.Hosted.OData.Api.Core.EdmConstants;

namespace OESoftware.Hosted.OData.Api.Tests.Core
{
    public class TestValueGenerator : IValueGenerator
    {
        public async Task<object> CreateKey(string dbIdentifier, string keyName, IEdmType type, IEdmType entityType)
        {
            return await Task<object>.Run(() =>
            {
                object value = null;
                switch (type.FullTypeName())
                {
                    case EdmConstants.EdmInt16TypeName:
                            value = Int16.MaxValue;
                            break;
                    case EdmConstants.EdmInt32TypeName:
                            value = Int32.MaxValue;
                            break;
                    case EdmConstants.EdmInt64TypeName:
                            value = Int64.MaxValue;
                            break;
                    case EdmConstants.EdmDecimalTypeName:
                            value = Decimal.MaxValue;
                            break;
                    case EdmConstants.EdmDoubleTypeName:
                            value = Double.MaxValue;
                            break;
                    case EdmConstants.EdmGuidTypeName:
                            value = Guid.Parse("e3f2977c-4bf1-4a22-b560-2155d05abe58");
                            break;
                    case EdmConstants.EdmSingleTypeName:
                            value = Single.MaxValue;
                            break;
                    case EdmConstants.EdmDateTimeTypeName:
                        value = new DateTime(2015, 2, 20, 12, 1, 15);
                        break;
                    case EdmConstants.EdmTimeOfDayTypeName:
                    case EdmConstants.EdmTimeTypeName:
                        value = new TimeOfDay(12, 1, 15, 20);
                        break;
                    default:
                            throw new ApplicationException(string.Format("Unable to compute value of type {0}", type.FullTypeName()));
                }

                return value;
            });
        }
    }
}