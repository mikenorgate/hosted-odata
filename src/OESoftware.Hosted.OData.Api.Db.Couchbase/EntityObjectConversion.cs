// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies.V1;
using Newtonsoft.Json.Linq;
using OESoftware.Hosted.OData.Api.Core;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase
{
    /// <summary>
    ///     Conversion Options
    /// </summary>
    [Flags]
    public enum ConvertOptions
    {
        /// <summary>
        ///     No special behaviour, all properties will be copied and default values used for any missing
        /// </summary>
        None = 0,

        /// <summary>
        ///     Copy only the properties that have been set
        /// </summary>
        CopyOnlySet = 1,

        /// <summary>
        ///     Compute values for properties marked as computed
        /// </summary>
        ComputeValues = 2
    }

    /// <summary>
    ///     Conversion between <see cref="EdmEntityObject" /> and <see cref="JObject" />
    /// </summary>
    public class EntityObjectConverter
    {
        private readonly IValueGenerator _valueGenerator;

        /// <summary>
        ///     Default constructor
        /// </summary>
        /// <param name="valueGenerator">
        ///     <see cref="IValueGenerator" />
        /// </param>
        public EntityObjectConverter(IValueGenerator valueGenerator)
        {
            _valueGenerator = valueGenerator;
        }

        #region ToDocument

        /// <summary>
        ///     Convert an <see cref="EdmEntityObject" /> to a <see cref="JObject" />
        /// </summary>
        /// <param name="entity">The <see cref="EdmEntityObject" /> to convert</param>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="entityType">The <see cref="IEdmEntityType" /> of entity</param>
        /// <param name="options">
        ///     <see cref="ConvertOptions" />
        /// </param>
        /// <param name="model">The <see cref="IEdmModel" /> containing the type</param>
        /// <returns>A <see cref="JObject" /> containing all the properties from entity</returns>
        public async Task<JObject> ToDocument(EdmEntityObject entity, string tenantId, IEdmEntityType entityType,
            ConvertOptions options, IEdmModel model)
        {
            Contract.Requires(model != null);
            var properties =
                entityType.DeclaredProperties.Where(
                    p =>
                        (entityType.NavigationProperties() == null || !entityType.NavigationProperties()
                            .Any(
                                n =>
                                    n.ReferentialConstraint.PropertyPairs.Any(
                                        r =>
                                            r.DependentProperty.Name.Equals(p.Name,
                                                StringComparison.InvariantCultureIgnoreCase)))));

            if ((options & ConvertOptions.CopyOnlySet) == ConvertOptions.CopyOnlySet)
            {
                var changedProperties = entity.GetChangedPropertyNames();
                properties = properties.Where(p => changedProperties.Contains(p.Name));
            }

            return
                await
                    ToDocumentInternal(entity, tenantId, entityType, properties.Cast<IEdmStructuralProperty>().ToList(),
                        options, model);
        }

        /// <summary>
        ///     Convert a <see cref="EdmComplexObject" /> to a <see cref="JObject" />
        /// </summary>
        /// <param name="entity">The <see cref="EdmComplexObject" /> to convert</param>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="entityType">The <see cref="IEdmComplexType" /> of entity</param>
        /// <param name="options">
        ///     <see cref="ConvertOptions" />
        /// </param>
        /// <param name="model">The <see cref="IEdmModel" /> containing the type</param>
        /// <returns>A <see cref="JObject" /> containing all the properties from entity</returns>
        private async Task<JObject> ToDocument(EdmComplexObject entity, string tenantId, IEdmComplexType entityType,
            ConvertOptions options, IEdmModel model)
        {
            Contract.Requires(model != null);

            var properties =
                entityType.DeclaredProperties;

            if ((options & ConvertOptions.CopyOnlySet) == ConvertOptions.CopyOnlySet)
            {
                var changedProperties = entity.GetChangedPropertyNames();
                properties = properties.Where(p => changedProperties.Contains(p.Name));
            }

            return
                await
                    ToDocumentInternal(entity, tenantId, entityType, properties.Cast<IEdmStructuralProperty>().ToList(),
                        options, model);
        }

        /// <summary>
        ///     Convert a <see cref="EdmStructuredObject" /> to a <see cref="JObject" />
        /// </summary>
        /// <param name="entity">The <see cref="EdmStructuredObject" /> to convert</param>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="entityType">The <see cref="IEdmStructuredType" /> of entity</param>
        /// <param name="properties">A <see cref="List{IEdmProperty}" /> containing the properties to copy</param>
        /// <param name="options">
        ///     <see cref="ConvertOptions" />
        /// </param>
        /// <param name="model">The <see cref="IEdmModel" /> containing the type</param>
        /// <returns>A <see cref="JObject" /> containing all the properties from entity</returns>
        private async Task<JObject> ToDocumentInternal(EdmStructuredObject entity, string tenantId,
            IEdmStructuredType entityType, IList<IEdmStructuralProperty> properties, ConvertOptions options,
            IEdmModel model)
        {
            Contract.Requires(model != null);

            var result = new JObject();
            foreach (var edmProperty in properties.Where(
                k =>
                    k.VocabularyAnnotations(model).All(v => v.Term.FullName() != CoreVocabularyConstants.Computed)))
            {
                await CopyPropertyValue(entity, tenantId, options, model, edmProperty, result);
            }

            if ((options & ConvertOptions.ComputeValues) == ConvertOptions.ComputeValues)
            {
                await ComputeProperties(entity, tenantId, entityType, model, result);
            }

            CopyDynamicProperties(entity, entityType, result);

            return result;
        }

        /// <summary>
        ///     Copies a value from a property in result
        /// </summary>
        /// <param name="entity">The <see cref="EdmStructuredObject" /> to convert</param>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="options">
        ///     <see cref="ConvertOptions" />
        /// </param>
        /// <param name="model">The <see cref="IEdmModel" /> containing the type</param>
        /// <param name="edmProperty">The <see cref="IEdmStructuralProperty" /> to copy</param>
        /// <param name="result">The <see cref="JObject" /> to copy the value to</param>
        /// <returns>void</returns>
        private async Task CopyPropertyValue(EdmStructuredObject entity, string tenantId, ConvertOptions options,
            IEdmModel model,
            IEdmStructuralProperty edmProperty, JObject result)
        {
            var value = GetPropertyValue(entity, edmProperty);
            var complex = value as EdmComplexObject;
            if (complex != null)
            {
                var obj =
                    await
                        ToDocument(complex, tenantId, complex.GetEdmType().Definition as IEdmComplexType, options, model);
                result.Add(edmProperty.Name, obj);
            }
            else if (edmProperty.Type.IsCollection())
            {
                var array = await CollectionToJArray(tenantId, options, model, value as IEnumerable);
                result.Add(edmProperty.Name, array);
            }
            else
            {
                result.Add(edmProperty.Name, value == null ? null : JToken.FromObject(value));
            }
        }

        /// <summary>
        ///     Get a value from a property
        /// </summary>
        /// <param name="entity">The <see cref="EdmStructuredObject" /> to get the value from</param>
        /// <param name="property">The <see cref="IEdmStructuralProperty" /> to get</param>
        /// <returns>
        ///     Gets the value from the entity
        ///     If the value is not set the gets default value
        /// </returns>
        private static object GetPropertyValue(EdmStructuredObject entity, IEdmStructuralProperty property)
        {
            var setProperties = entity.GetChangedPropertyNames();

            object value;
            try
            {
                if (!setProperties.Contains(property.Name) &&
                    !string.IsNullOrEmpty(property.DefaultValueString))
                {
                    value = EdmTypeToClrType.ParseDefaultString(property.Type, property.DefaultValueString);
                }
                else
                {
                    entity.TryGetPropertyValue(property.Name, out value);
                }
            }
            catch (MissingMethodException) //Thrown when TryGetPropertyValue cannot create default
            {
                value = EdmTypeToClrType.Default(property.Type);
            }
            catch (InvalidOperationException) //Thrown when TryGetPropertyValue cannot find clr type
            {
                value = EdmTypeToClrType.Default(property.Type);
            }
            return value;
        }

        /// <summary>
        ///     Copy dynamic properties from entity
        /// </summary>
        /// <param name="entity">The <see cref="IDelta" /> to get the properties from</param>
        /// <param name="entityType">The <see cref="IEdmStructuredType" /> of entity</param>
        /// <param name="result">The <see cref="JObject" /> to copy the values to</param>
        /// <exception cref="ApplicationException">Thrown if entity has dynamic properties but entityType is not open</exception>
        private static void CopyDynamicProperties(IDelta entity, IEdmStructuredType entityType, JObject result)
        {
            var dynamicProperties =
                entity.GetChangedPropertyNames()
                    .Where(
                        e =>
                            !entityType.DeclaredProperties.Any(
                                d => d.Name.Equals(e, StringComparison.InvariantCultureIgnoreCase))).ToList();

            foreach (var dynamicMemberName in dynamicProperties)
            {
                if (!entityType.IsOpen)
                {
                    throw new ApplicationException("Dynamic properties not supported on this type");
                }
                object value;
                entity.TryGetPropertyValue(dynamicMemberName, out value);
                result.Add(dynamicMemberName, JToken.FromObject(value));
            }
        }

        /// <summary>
        ///     Compute all properties marked as computed
        /// </summary>
        /// <param name="entity">The <see cref="IDelta" /> to set the computed properties on</param>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="entityType">The <see cref="IEdmStructuredType" /> of entity</param>
        /// <param name="model">The <see cref="IEdmModel" /> containing the type</param>
        /// <param name="result">The <see cref="JObject" /> to copy the values to</param>
        /// <returns>void</returns>
        private async Task ComputeProperties(IDelta entity, string tenantId, IEdmStructuredType entityType,
            IEdmModel model, JObject result)
        {
            var tasks =
                (from key in
                    entityType.DeclaredProperties.Where(
                        k =>
                            k.VocabularyAnnotations(model)
                                .Any(v => v.Term.FullName() == CoreVocabularyConstants.Computed))
                    let key1 = key
                    select
                        _valueGenerator.CreateKey(tenantId, key.Name, key.Type.Definition, entityType)
                            .ContinueWith(task =>
                            {
                                entity.TrySetPropertyValue(key1.Name, task.Result);
                                result[key1.Name] = JToken.FromObject(task.Result);
                            })).ToList
                    ();
            await Task.WhenAll(tasks);
        }

        /// <summary>
        ///     Convert a collection to a JArray
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="options">
        ///     <see cref="ConvertOptions" />
        /// </param>
        /// <param name="model">The <see cref="IEdmModel" /> containing the type</param>
        /// <param name="value"><see cref="IEnumerable" /> of items to convert</param>
        /// <returns>
        ///     <see cref="JArray" />
        /// </returns>
        private async Task<JArray> CollectionToJArray(string tenantId, ConvertOptions options, IEdmModel model,
            IEnumerable value)
        {
            var array = new JArray();
            foreach (var val in value)
            {
                if (val is EdmComplexObject)
                {
                    var complex = val as EdmComplexObject;
                    var obj =
                        await
                            ToDocument(complex, tenantId, complex.GetEdmType().Definition as IEdmComplexType, options,
                                model);
                    array.Add(obj);
                }
                else if (val is EdmEntityObject)
                {
                    var entityObj = val as EdmEntityObject;
                    var obj =
                        await
                            ToDocument(entityObj, tenantId, entityObj.GetEdmType().Definition as IEdmEntityType,
                                options, model);
                    array.Add(obj);
                }
                else
                {
                    array.Add(val == null ? null : JToken.FromObject(val));
                }
            }
            return array;
        }

        #endregion

        #region ToEdmEntityObject

        /// <summary>
        ///     Convert a <see cref="JObject" /> to a <see cref="EdmEntityObject" />
        /// </summary>
        /// <param name="entity">The <see cref="JObject" /> to convert</param>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="entityType">The <see cref="IEdmEntityType" /> of entity</param>
        /// <returns>
        ///     <see cref="EdmEntityObject" />
        /// </returns>
        public EdmEntityObject ToEdmEntityObject(JObject entity, string tenantId, IEdmEntityType entityType)
        {
            var result = new EdmEntityObject(entityType);
            var properties =
                entityType.DeclaredProperties.Where(
                    p =>
                        (entityType.NavigationProperties() == null || !entityType.NavigationProperties()
                            .Any(
                                n =>
                                    n.ReferentialConstraint.PropertyPairs.Any(
                                        r =>
                                            r.DependentProperty.Name.Equals(p.Name,
                                                StringComparison.InvariantCultureIgnoreCase))))).ToList();

            ToEdmEntityObjectInternal(entity, tenantId, entityType, properties, result);

            return result;
        }

        /// <summary>
        ///     Convert a <see cref="JObject" /> to a <see cref="EdmComplexObject" />
        /// </summary>
        /// <param name="entity">The <see cref="JObject" /> to convert</param>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="entityType">The <see cref="IEdmComplexType" /> of entity</param>
        /// <returns>
        ///     <see cref="EdmEntityObject" />
        /// </returns>
        private EdmComplexObject ToEdmEntityObject(JObject entity, string tenantId, IEdmComplexType entityType)
        {
            var result = new EdmComplexObject(entityType);
            var properties =
                entityType.DeclaredProperties.ToList();

            ToEdmEntityObjectInternal(entity, tenantId, entityType, properties, result);

            return result;
        }

        /// <summary>
        ///     Copy all properties from the <see cref="JObject" /> to result
        /// </summary>
        /// <typeparam name="TResult">The type of result</typeparam>
        /// <typeparam name="TEntityType">The type of entityType</typeparam>
        /// <param name="entity">The <see cref="JObject" /> to convert</param>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="entityType">The type of entity</param>
        /// <param name="properties"><see cref="IEnumerable{IEdmProperty}" /> to copy</param>
        /// <param name="result">The item to put the properties into</param>
        private void ToEdmEntityObjectInternal<TResult, TEntityType>(JObject entity, string tenantId,
            TEntityType entityType, IEnumerable<IEdmProperty> properties, TResult result)
            where TResult : EdmStructuredObject where TEntityType : IEdmStructuredType
        {
            foreach (var edmProperty in properties)
            {
                ProcessProperty(entity, tenantId, result, edmProperty);
            }

            ReadDynamicProperties(entity, entityType, result);
        }

        /// <summary>
        ///     Copy a property into result
        /// </summary>
        /// <typeparam name="TResult">The type of result</typeparam>
        /// <param name="entity">The <see cref="JObject" /> to convert</param>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="result">The item to put the property into</param>
        /// <param name="edmProperty">The <see cref="IEdmProperty" /> to get the value of</param>
        [SuppressMessage("ReSharper", "CanBeReplacedWithTryCastAndCheckForNull")]
        private void ProcessProperty<TResult>(JObject entity, string tenantId, TResult result, IEdmProperty edmProperty)
            where TResult : EdmStructuredObject
        {
            var property = edmProperty;

            JToken value;
            if (!entity.TryGetValue(property.Name, out value)) return;

            if (property.Type.Definition is IEdmComplexType)
            {
                var obj = ToEdmEntityObject(value as JObject, tenantId, (IEdmComplexType) property.Type.Definition);
                result.TrySetPropertyValue(property.Name, obj);
            }
            else if (property.Type.Definition is IEdmEnumType)
            {
                var enumValue = value as JObject;
                var obj = new EdmEnumObject((IEdmEnumType) property.Type.Definition, enumValue?["Value"].Value<string>());
                result.TrySetPropertyValue(property.Name, obj);
            }
            else if (property.Type.IsCollection())
            {
                JArrayToEntityCollection(tenantId, result, property, value as JArray);
            }
            else
            {
                result.TrySetPropertyValue(property.Name,
                    value.ToObject(EdmTypeToClrType.Parse(property.Type.Definition)));
            }
        }

        /// <summary>
        ///     Read any dynamic properties for the <see cref="JObject" />
        /// </summary>
        /// <typeparam name="TResult">The type of result</typeparam>
        /// <typeparam name="TEntityType">The type of entityType</typeparam>
        /// <param name="entity">The <see cref="JObject" /> to convert</param>
        /// <param name="entityType">The type of entity</param>
        /// <param name="result">The item to put the properties into</param>
        /// <exception cref="ApplicationException">Thrown if entity has dynamic properties but entityType is not open</exception>
        private static void ReadDynamicProperties<TResult, TEntityType>(JObject entity, TEntityType entityType,
            TResult result) where TResult : EdmStructuredObject
            where TEntityType : IEdmStructuredType
        {
            var dynamicProperties =
                entity.Properties()
                    .Where(
                        e =>
                            !entityType.DeclaredProperties.Any(
                                d => d.Name.Equals(e.Name, StringComparison.InvariantCultureIgnoreCase))).ToList();
            foreach (var dynamicMemberName in dynamicProperties)
            {
                if (!entityType.IsOpen)
                {
                    throw new ApplicationException("Dynamic properties not supported on this type");
                }
                JToken value;
                if (entity.TryGetValue(dynamicMemberName.Name, out value))
                {
                    result.TrySetPropertyValue(dynamicMemberName.Name, value.Value<string>());
                }
            }
        }

        /// <summary>
        ///     Copy a <see cref="JArray" /> to result
        /// </summary>
        /// <typeparam name="TResult">The type of result</typeparam>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="result">The item to put the property into</param>
        /// <param name="property">The <see cref="IEdmProperty" /> to get the value of</param>
        /// <param name="array">The arrary of values</param>
        private void JArrayToEntityCollection<TResult>(string tenantId, TResult result, IEdmProperty property,
            JArray array)
            where TResult : EdmStructuredObject
        {
            var elementType = property.Type.AsCollection().ElementType().Definition;
            if (elementType is IEdmComplexType)
            {
                var listOfValues =
                    array.Select(
                        arrayValue => ToEdmEntityObject(arrayValue as JObject, tenantId, elementType as IEdmComplexType))
                        .ToList();
                result.TrySetPropertyValue(property.Name, listOfValues);
            }
            else if (elementType is IEdmEntityType)
            {
                var listOfValues =
                    array.Select(
                        arrayValue => ToEdmEntityObject(arrayValue as JObject, tenantId, elementType as IEdmEntityType))
                        .ToList();
                result.TrySetPropertyValue(property.Name, listOfValues);
            }
            else if (elementType is IEdmEnumType)
            {
                var listOfValues =
                    array.Select(
                        arrayValue =>
                            new EdmEnumObject(elementType as IEdmEnumType, arrayValue["Value"].Value<string>()))
                        .ToList();
                result.TrySetPropertyValue(property.Name, listOfValues);
            }
            else
            {
                var listOfValues =
                    array.Select(arrayValue => arrayValue.ToObject(EdmTypeToClrType.Parse(elementType))).ToList();
                result.TrySetPropertyValue(property.Name, listOfValues);
            }
        }

        #endregion
    }
}