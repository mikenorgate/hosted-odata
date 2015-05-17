using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.OData;
using System.Xml;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using OESoftware.Hosted.OData.Api.Tests.Core;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Tests
{
    [TestClass]
    public class EntityObjectConversionTests
    {
        IEdmModel _model;
        
        //TODO: Navigation support

        [TestInitialize]
        public void TestInitialize()
        {
            using (var stringReader = new FileStream("TestDataModel.xml", FileMode.Open))
            {
                using (var xmlReader = XmlReader.Create(stringReader))
                {
                    IEnumerable<EdmError> errors;
                    EdmxReader.TryParse(xmlReader, out _model, out errors);
                }
            }
        }

        #region ToDocument

        [TestMethod]
        public void ToDocument_CopiesAllTypesDefaultValues_CreateJObject()
        {
            var type = _model.FindDeclaredType("Test.AllTypes") as IEdmEntityType;

            var obj = new EdmEntityObject(type);

            var converter = new EntityObjectConverter(new TestValueGenerator());
            var result = converter.ToDocument(obj, "", type, ConvertOptions.None, _model).Result;
            
            Assert.AreEqual(new Byte[0], result["Binary"]);
            Assert.AreEqual(false, result["Boolean"]);
            Assert.AreEqual(0, result["Byte"]);
            Assert.AreEqual(new DateTime(), result["DateTime"]);
            Assert.AreEqual(0.0, result["Decimal"]);
            Assert.AreEqual(0.0, result["Double"]);
            Assert.AreEqual(0.0, result["Single"]);
            Assert.AreEqual(new Guid(), result["Guid"]);
            Assert.AreEqual(0, result["Int16"]);
            Assert.AreEqual(0, result["Int32"]);
            Assert.AreEqual(0, result["Int64"]);
            Assert.AreEqual(0, result["SByte"]);
            Assert.AreEqual(null, result["String"].Value<string>());
            Assert.AreEqual(new DateTimeOffset(), result["DateTimeOffset"]);
            Assert.IsNotNull(result["Collection"] as JArray);
            Assert.AreEqual(0, (result["Collection"] as JArray).Count);
            Assert.IsNotNull(result["CollectionComplex"] as JArray);
            Assert.AreEqual(0, (result["CollectionComplex"] as JArray).Count);

            var time = result["Time"] as JObject;
            Assert.AreEqual(0, time["Hours"]);
            Assert.AreEqual(0, time["Minutes"]);
            Assert.AreEqual(0, time["Seconds"]);
            Assert.AreEqual(0, time["Milliseconds"]);
            Assert.AreEqual(0, time["Ticks"]);

            var complex = result["Complex"] as JObject;
            Assert.AreEqual(0, complex["Int1"]);
            Assert.AreEqual(0, complex["Int2"]);
            Assert.AreEqual(0, complex["Int3"]);

            var enumObj = result["Enum"] as JObject;
            Assert.AreEqual("Enum1", enumObj["Value"]);
            Assert.AreEqual(false, enumObj["IsNullable"]);
        }

        [TestMethod]
        public void ToDocument_CopiesAllTypesDefinedDefaultValues_CreateJObject()
        {
            var type = _model.FindDeclaredType("Test.AllTypesWithDefaults") as IEdmEntityType;

            var obj = new EdmEntityObject(type);

            var converter = new EntityObjectConverter(new TestValueGenerator());
            var result = converter.ToDocument(obj, "", type, ConvertOptions.None, _model).Result;

            Assert.AreEqual(true, result["Boolean"]);
            Assert.AreEqual(DateTime.Parse("2000-12-12T12:00"), result["DateTime"]);
            Assert.AreEqual(1.3, result["Decimal"]);
            Assert.AreEqual(1.3, result["Double"]);
            Assert.AreEqual((Single)1.3, result["Single"].Value<Single>());
            Assert.AreEqual(Guid.Parse("83ad5540-dba8-420d-855a-6ee5a187e6cb"), result["Guid"]);
            Assert.AreEqual(5, result["Int16"]);
            Assert.AreEqual(5, result["Int32"]);
            Assert.AreEqual(5, result["Int64"]);
            Assert.AreEqual(1, result["SByte"]);
            Assert.AreEqual("Test", result["String"].Value<string>());
            Assert.AreEqual(DateTimeOffset.Parse("2002-10-10T17:00:00Z"), result["DateTimeOffset"]);

            var expectedTime = TimeOfDay.Parse("13:20:00");
            var time = result["Time"] as JObject;
            Assert.AreEqual(expectedTime.Hours, time["Hours"]);
            Assert.AreEqual(expectedTime.Minutes, time["Minutes"]);
            Assert.AreEqual(expectedTime.Seconds, time["Seconds"]);
            Assert.AreEqual(expectedTime.Milliseconds, time["Milliseconds"]);
            Assert.AreEqual(expectedTime.Ticks, time["Ticks"]);


            var enumObj = result["Enum"] as JObject;
            Assert.AreEqual("Enum2", enumObj["Value"]);
            Assert.AreEqual(false, enumObj["IsNullable"]);
        }

        [TestMethod]
        public void ToDocument_CopiesAllTypesWithSetValues_CreateJObject()
        {
            var type = _model.FindDeclaredType("Test.AllTypes") as IEdmEntityType;

            var obj = new EdmEntityObject(type);
            obj.TrySetPropertyValue("Boolean", true);
            var dateTime = new DateTime(2015, 10, 5, 1, 25, 8);
            obj.TrySetPropertyValue("DateTime", dateTime);
            obj.TrySetPropertyValue("Decimal", Decimal.MaxValue);
            obj.TrySetPropertyValue("Double", Double.MaxValue);
            obj.TrySetPropertyValue("Single", Single.MaxValue);
            var guid = Guid.Parse("2c50b65f-5fdf-46e1-885f-44e7f648d715");
            obj.TrySetPropertyValue("Guid", guid);
            obj.TrySetPropertyValue("Int16", Int16.MaxValue);
            obj.TrySetPropertyValue("Int32", Int32.MaxValue);
            obj.TrySetPropertyValue("Int64", Int64.MaxValue);
            var stringValue = "Test String";
            obj.TrySetPropertyValue("String", stringValue);
            var dateTimeOffset = new DateTimeOffset(DateTime.UtcNow);
            obj.TrySetPropertyValue("DateTimeOffset", dateTimeOffset);
            var timeOfDay = new TimeOfDay(5, 48, 23, 100);
            obj.TrySetPropertyValue("Time", timeOfDay);

            var complexType = _model.FindDeclaredType("Test.ComplexType") as IEdmComplexType;
            var complexObj = new EdmComplexObject(complexType);
            complexObj.TrySetPropertyValue("Int1", 1);
            complexObj.TrySetPropertyValue("Int2", 2);
            complexObj.TrySetPropertyValue("Int3", 3);
            obj.TrySetPropertyValue("Complex", complexObj);

            var enumType = _model.FindDeclaredType("Test.EnumType") as IEdmEnumType;
            var enumObj = new EdmEnumObject(enumType, "Enum3");
            obj.TrySetPropertyValue("Enum", enumObj);

            var converter = new EntityObjectConverter(new TestValueGenerator());
            var result = converter.ToDocument(obj, "", type, ConvertOptions.None, _model).Result;

            Assert.AreEqual(true, result["Boolean"]);
            Assert.AreEqual(dateTime, result["DateTime"]);
            Assert.AreEqual(Decimal.MaxValue, result["Decimal"]);
            Assert.AreEqual(Double.MaxValue, result["Double"]);
            Assert.AreEqual(Single.MaxValue, result["Single"]);
            Assert.AreEqual(guid, result["Guid"]);
            Assert.AreEqual(Int16.MaxValue, result["Int16"]);
            Assert.AreEqual(Int32.MaxValue, result["Int32"]);
            Assert.AreEqual(Int64.MaxValue, result["Int64"]);
            Assert.AreEqual(stringValue, result["String"].Value<string>());
            Assert.AreEqual(dateTimeOffset, result["DateTimeOffset"]);

            var time = result["Time"] as JObject;
            Assert.AreEqual(timeOfDay.Hours, time["Hours"]);
            Assert.AreEqual(timeOfDay.Minutes, time["Minutes"]);
            Assert.AreEqual(timeOfDay.Seconds, time["Seconds"]);
            Assert.AreEqual(timeOfDay.Milliseconds, time["Milliseconds"]);
            Assert.AreEqual(timeOfDay.Ticks, time["Ticks"]);

            var complex = result["Complex"] as JObject;
            Assert.AreEqual(1, complex["Int1"]);
            Assert.AreEqual(2, complex["Int2"]);
            Assert.AreEqual(3, complex["Int3"]);

            var enumOut = result["Enum"] as JObject;
            Assert.AreEqual("Enum3", enumOut["Value"]);
        }

        [TestMethod]
        public void ToDocument_CopiesOnlySetValues_CreateJObject()
        {
            var type = _model.FindDeclaredType("Test.AllTypes") as IEdmEntityType;

            var obj = new EdmEntityObject(type);
            obj.TrySetPropertyValue("Boolean", true);
            obj.TrySetPropertyValue("Int16", Int16.MaxValue);
            obj.TrySetPropertyValue("Int32", Int32.MaxValue);
            obj.TrySetPropertyValue("Int64", Int64.MaxValue);
            var timeOfDay = new TimeOfDay(5, 48, 23, 100);
            obj.TrySetPropertyValue("Time", timeOfDay);

            var complexType = _model.FindDeclaredType("Test.ComplexType") as IEdmComplexType;
            var complexObj = new EdmComplexObject(complexType);
            complexObj.TrySetPropertyValue("Int1", 1);
            complexObj.TrySetPropertyValue("Int2", 2);
            obj.TrySetPropertyValue("Complex", complexObj);
            

            var converter = new EntityObjectConverter(new TestValueGenerator());
            var result = converter.ToDocument(obj, "", type, ConvertOptions.CopyOnlySet, _model).Result;

            Assert.AreEqual(true, result["Boolean"]);
            Assert.AreEqual(Int16.MaxValue, result["Int16"]);
            Assert.AreEqual(Int32.MaxValue, result["Int32"]);
            Assert.AreEqual(Int64.MaxValue, result["Int64"]);;

            var time = result["Time"] as JObject;
            Assert.AreEqual(timeOfDay.Hours, time["Hours"]);
            Assert.AreEqual(timeOfDay.Minutes, time["Minutes"]);
            Assert.AreEqual(timeOfDay.Seconds, time["Seconds"]);
            Assert.AreEqual(timeOfDay.Milliseconds, time["Milliseconds"]);
            Assert.AreEqual(timeOfDay.Ticks, time["Ticks"]);

            Assert.AreEqual(6, result.Properties().Count());

            var complex = result["Complex"] as JObject;
            Assert.AreEqual(1, complex["Int1"]);
            Assert.AreEqual(2, complex["Int2"]);
            Assert.AreEqual(2, complex.Properties().Count());
            
        }

        [TestMethod]
        public void ToDocument_DoesNotCopyComputedPropertiesIfOptionNotSpecified_CreateJObject()
        {
            var type = _model.FindDeclaredType("Test.AllComputedTypes") as IEdmEntityType;

            var obj = new EdmEntityObject(type);
            var converter = new EntityObjectConverter(new TestValueGenerator());
            var result = converter.ToDocument(obj, "", type, ConvertOptions.None, _model).Result;

            Assert.AreEqual(1, result.Properties().Count());

            var complex = result["Complex"] as JObject;
            Assert.AreEqual(0, complex.Properties().Count());

        }

        [TestMethod]
        public void ToDocument_CopiesComputedProperties_CreateJObject()
        {
            var type = _model.FindDeclaredType("Test.AllComputedTypes") as IEdmEntityType;

            var obj = new EdmEntityObject(type);
            var converter = new EntityObjectConverter(new TestValueGenerator());
            var result = converter.ToDocument(obj, "", type, ConvertOptions.ComputeValues, _model).Result;

            Assert.AreEqual(10, result.Properties().Count());
            Assert.AreEqual(new DateTime(2015, 2, 20, 12, 1, 15), result["DateTime"]);
            Assert.AreEqual(Decimal.MaxValue, result["Decimal"]);
            Assert.AreEqual(Double.MaxValue, result["Double"]);
            Assert.AreEqual(Single.MaxValue, result["Single"]);
            Assert.AreEqual(Guid.Parse("e3f2977c-4bf1-4a22-b560-2155d05abe58"), result["Guid"]);
            Assert.AreEqual(Int16.MaxValue, result["Int16"]);
            Assert.AreEqual(Int32.MaxValue, result["Int32"]);
            Assert.AreEqual(Int64.MaxValue, result["Int64"]);

            var time = result["Time"] as JObject;
            Assert.AreEqual(12, time["Hours"]);
            Assert.AreEqual(1, time["Minutes"]);
            Assert.AreEqual(15, time["Seconds"]);
            Assert.AreEqual(20, time["Milliseconds"]);

            var complex = result["Complex"] as JObject;
            Assert.AreEqual(Int32.MaxValue, complex["Int1"]);
            Assert.AreEqual(Int32.MaxValue, complex["Int2"]);
            Assert.AreEqual(Int32.MaxValue, complex["Int3"]);
            Assert.AreEqual(3, complex.Properties().Count());}

        [TestMethod]
        public void ToDocument_CopiesDynamicPropertiesFromOpenType_CreateJObject()
        {
            var type = _model.FindDeclaredType("Test.OpenEntity") as IEdmEntityType;

            var obj = new EdmEntityObject(type);
            obj.TrySetPropertyValue("String", "Some value");
            obj.TrySetPropertyValue("Int", int.MaxValue);

            var converter = new EntityObjectConverter(new TestValueGenerator());
            var result = converter.ToDocument(obj, "", type, ConvertOptions.None, _model).Result;

            Assert.AreEqual(2, result.Properties().Count());
            Assert.AreEqual("Some value", result["String"]);
            Assert.AreEqual(int.MaxValue, result["Int"]);

        }

        [TestMethod]
        public void ToDocument_CopiesCollectionValues_CreateJObject()
        {
            var type = _model.FindDeclaredType("Test.JustCollections") as IEdmEntityType;
            var complexType = _model.FindDeclaredType("Test.ComplexType") as IEdmComplexType;
            var enumType = _model.FindDeclaredType("Test.EnumType") as IEdmEnumType;
            var entityType = _model.FindDeclaredType("Test.AllTypesWithDefaults") as IEdmEntityType;

            var obj = new EdmEntityObject(type);
            obj.TrySetPropertyValue("PrimitiveCollection", new List<string> {"Test1", "Test2"});
            obj.TrySetPropertyValue("ComplexCollection", new List<EdmComplexObject> {new EdmComplexObject(complexType), new EdmComplexObject(complexType) });
            obj.TrySetPropertyValue("EnumCollection", new List<EdmEnumObject> {new EdmEnumObject(enumType, "Enum3"), new EdmEnumObject(enumType, "Enum2")});
            obj.TrySetPropertyValue("EntityCollection", new List<EdmEntityObject> {new EdmEntityObject(entityType), new EdmEntityObject(entityType)});


            var converter = new EntityObjectConverter(new TestValueGenerator());
            var result = converter.ToDocument(obj, "", type, ConvertOptions.None, _model).Result;

            Assert.AreEqual(4, result.Properties().Count());

            var primitiveCollection = result["PrimitiveCollection"] as JArray;
            Assert.AreEqual(2, primitiveCollection.Count);
            Assert.AreEqual("Test1", primitiveCollection[0]);
            Assert.AreEqual("Test2", primitiveCollection[1]);

            var complexCollection = result["ComplexCollection"] as JArray;
            Assert.AreEqual(2, complexCollection.Count);
            Assert.AreEqual(3, (complexCollection[0] as JObject).Properties().Count());
            Assert.AreEqual(3, (complexCollection[1] as JObject).Properties().Count());

            var enumCollection = result["EnumCollection"] as JArray;
            Assert.AreEqual(2, enumCollection.Count);
            Assert.AreEqual(2, (enumCollection[0] as JObject).Properties().Count());
            Assert.AreEqual(2, (enumCollection[1] as JObject).Properties().Count());

            var entityCollection = result["EntityCollection"] as JArray;
            Assert.AreEqual(2, entityCollection.Count);
            Assert.AreEqual(14, (entityCollection[0] as JObject).Properties().Count());
            Assert.AreEqual(14, (entityCollection[1] as JObject).Properties().Count());
        }

        #endregion

        #region ToEdmEntityObject

        [TestMethod]
        public void ToEdmEntityObject_CopiesAllValues_CreateEntityObject()
        {
            var type = _model.FindDeclaredType("Test.AllTypes") as IEdmEntityType;

            var obj = new EdmEntityObject(type);

            var converter = new EntityObjectConverter(new TestValueGenerator());
            var jObject = converter.ToDocument(obj, "", type, ConvertOptions.None, _model).Result;

            var result = converter.ToEdmEntityObject(jObject, "", type);

            Assert.AreEqual(0, ((Byte[])GetValue(result,"Binary")).Length);
            Assert.AreEqual(false, GetValue(result, "Boolean"));
            Assert.AreEqual(0, (Byte)GetValue(result, "Byte"));
            Assert.AreEqual(new DateTime(), GetValue(result, "DateTime"));
            Assert.AreEqual((Decimal)0.0, GetValue(result, "Decimal"));
            Assert.AreEqual((Double)0.0, GetValue(result, "Double"));
            Assert.AreEqual((Single)0.0, GetValue(result, "Single"));
            Assert.AreEqual(new Guid(), GetValue(result, "Guid"));
            Assert.AreEqual((Int16)0, GetValue(result, "Int16"));
            Assert.AreEqual((Int32)0, GetValue(result, "Int32"));
            Assert.AreEqual((Int64)0, GetValue(result, "Int64"));
            Assert.AreEqual((SByte)0, GetValue(result, "SByte"));
            Assert.AreEqual(null, GetValue(result,"String"));
            Assert.AreEqual(new DateTimeOffset(), GetValue(result, "DateTimeOffset"));

            var time = (TimeOfDay)GetValue(result, "Time");
            Assert.AreEqual(0, time.Hours);
            Assert.AreEqual(0, time.Minutes);
            Assert.AreEqual(0, time.Seconds);
            Assert.AreEqual(0, time.Milliseconds);
            Assert.AreEqual(0, time.Ticks);

            var complex = GetValue(result, "Complex") as EdmComplexObject;
            Assert.AreEqual(0, GetValue(complex, "Int1"));
            Assert.AreEqual(0, GetValue(complex, "Int2"));
            Assert.AreEqual(0, GetValue(complex, "Int3"));

            var enumObj = GetValue(result, "Enum") as EdmEnumObject;
            Assert.AreEqual("Enum1", enumObj.Value);
        }

        [TestMethod]
        public void ToEdmEntityObject_CopiesDynamicValues_CreateEntityObject()
        {
            var type = _model.FindDeclaredType("Test.OpenEntity") as IEdmEntityType;

            var obj = new EdmEntityObject(type);
            obj.TrySetPropertyValue("String", "Test string");

            var converter = new EntityObjectConverter(new TestValueGenerator());
            var jObject = converter.ToDocument(obj, "", type, ConvertOptions.None, _model).Result;

            var result = converter.ToEdmEntityObject(jObject, "", type);
            
            Assert.AreEqual("Test string", GetValue(result, "String"));
        }

        [TestMethod]
        public void ToEdmEntityObject_CopiesCollectionValues_CreateEntityObject()
        {
            var type = _model.FindDeclaredType("Test.JustCollections") as IEdmEntityType;
            var complexType = _model.FindDeclaredType("Test.ComplexType") as IEdmComplexType;
            var enumType = _model.FindDeclaredType("Test.EnumType") as IEdmEnumType;
            var entityType = _model.FindDeclaredType("Test.AllTypesWithDefaults") as IEdmEntityType;

            var obj = new EdmEntityObject(type);
            obj.TrySetPropertyValue("PrimitiveCollection", new List<string> { "Test1", "Test2" });
            obj.TrySetPropertyValue("ComplexCollection", new List<EdmComplexObject> { new EdmComplexObject(complexType), new EdmComplexObject(complexType) });
            obj.TrySetPropertyValue("EnumCollection", new List<EdmEnumObject> { new EdmEnumObject(enumType, "Enum3"), new EdmEnumObject(enumType, "Enum2") });
            obj.TrySetPropertyValue("EntityCollection", new List<EdmEntityObject> { new EdmEntityObject(entityType), new EdmEntityObject(entityType) });


            var converter = new EntityObjectConverter(new TestValueGenerator());
            var jObject = converter.ToDocument(obj, "", type, ConvertOptions.None, _model).Result;

            var result = converter.ToEdmEntityObject(jObject, "", type);

            var primitiveCollection = GetValue(result, "PrimitiveCollection") as List<object>;
            Assert.AreEqual(2, primitiveCollection.Count);
            Assert.AreEqual("Test1", primitiveCollection[0]);
            Assert.AreEqual("Test2", primitiveCollection[1]);

            var complexCollection = GetValue(result,"ComplexCollection") as List<EdmComplexObject>;
            Assert.AreEqual(2, complexCollection.Count);
            Assert.AreEqual(3, complexCollection[0].GetChangedPropertyNames().Count());
            Assert.AreEqual(3, complexCollection[1].GetChangedPropertyNames().Count());

            var enumCollection = GetValue(result,"EnumCollection") as List<EdmEnumObject>;
            Assert.AreEqual(2, enumCollection.Count);
            Assert.AreEqual("Enum3", enumCollection[0].Value);
            Assert.AreEqual("Enum2", enumCollection[1].Value);

            var entityCollection = GetValue(result,"EntityCollection") as List<EdmEntityObject>;
            Assert.AreEqual(2, entityCollection.Count);
            Assert.AreEqual(14, entityCollection[0].GetChangedPropertyNames().Count());
            Assert.AreEqual(14, entityCollection[1].GetChangedPropertyNames().Count());
        }

        private object GetValue(IEdmStructuredObject entity, string propertyName)
        {
            object value;
            entity.TryGetPropertyValue(propertyName, out value);
            return value;
        }

        #endregion
    }
}
