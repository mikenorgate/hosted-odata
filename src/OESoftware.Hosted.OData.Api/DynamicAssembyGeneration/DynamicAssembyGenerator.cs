using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Web.OData;
using System.Web.OData.Formatter;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library.Values;
using Microsoft.OData.Edm.Vocabularies.V1;
using Newtonsoft.Json;
using OESoftware.Hosted.OData.Api.Core;

namespace OESoftware.Hosted.OData.Api.DynamicAssembyGeneration
{
    public class DynamicAssembyGenerator
    {
        private IDictionary<string, AssemblyBuilder> _assemblyBuilders;
        private IDictionary<string, ModuleBuilder> _moduleBuilders;
        private IDictionary<string, TypeBuilder> _typeBuilders;
        private IDictionary<string, EnumBuilder> _enumBuilders;
        private IDictionary<string, Type> _types;

        public void Create(IEdmModel model)
        {
            _assemblyBuilders = model.DeclaredNamespaces.ToDictionary(declaredNamespace => declaredNamespace,
                CreateAssemblyBuilder);
            _moduleBuilders = model.DeclaredNamespaces.ToDictionary(declaredNamespace => declaredNamespace,
                declaredNamespace => CreateModuleBuilder(declaredNamespace, _assemblyBuilders[declaredNamespace]));
            _typeBuilders = new Dictionary<string, TypeBuilder>();
            _enumBuilders = new Dictionary<string, EnumBuilder>();
            _types = new Dictionary<string, Type>();

            foreach (
                var edmSchemaElement in
                    model.SchemaElements.Where(s => s.SchemaElementKind == EdmSchemaElementKind.TypeDefinition))
            {
                if (_moduleBuilders.ContainsKey(edmSchemaElement.Namespace))
                {
                    CreateType(model, edmSchemaElement.FullName(), _moduleBuilders[edmSchemaElement.Namespace]);
                }
            }

            foreach (var assemblyBuilder in _assemblyBuilders.Values)
            {
                assemblyBuilder.Save(assemblyBuilder.GetName().Name + ".dll");
            }

            foreach (var value in _types.Values)
            {
                var edmType = model.FindDeclaredType(value.FullName);
                model.SetAnnotationValue<ClrTypeAnnotation>(edmType, new ClrTypeAnnotation(value));
            }
        }

        private void CreateType(IEdmModel model, string typeName, ModuleBuilder moduleBuilder)
        {
            if (_typeBuilders.ContainsKey(typeName) || _enumBuilders.ContainsKey(typeName))
            {
                return;
            }
            var structuredType = model.FindDeclaredType(typeName) as IEdmStructuredType;
            if (structuredType != null)
            {
                var entityType = structuredType as IEdmEntityType;

                var typeBuilder = CreateTypeBuilder(structuredType, model, moduleBuilder);
                _typeBuilders.Add(typeName, typeBuilder);

                var constructorBuilder = CreateConstructor(typeBuilder);
                ILGenerator il = constructorBuilder.GetILGenerator();
                foreach (var edmProperty in structuredType.DeclaredProperties)
                {
                    CreateProperty(edmProperty, model, typeBuilder, il,
                        entityType != null && entityType.HasDeclaredKeyProperty(edmProperty));
                }

                il.Emit(OpCodes.Ret);

                _types.Add(typeName, typeBuilder.CreateType());
                return;
            }

            var enumType = model.FindDeclaredType(typeName) as IEdmEnumType;
            if (enumType != null)
            {

                var enumBuilder = CreateEnumBuilder(typeName, moduleBuilder);
                _enumBuilders.Add(typeName, enumBuilder);
                foreach (var edmEnumMember in enumType.Members)
                {
                    var value = edmEnumMember.Value as EdmIntegerConstant;
                    if (value != null)
                    {
                        CreateEnumValue(edmEnumMember.Name, Convert.ToInt32(value.Value), enumBuilder);
                    }
                }
                _types.Add(typeName, enumBuilder.CreateType());
                return;
            }
        }

        private AssemblyBuilder CreateAssemblyBuilder(string assemblyName)
        {
            var appDomain = AppDomain.CurrentDomain;
            var assemblyBuilder = appDomain.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndSave);

            return assemblyBuilder;
        }

        private ModuleBuilder CreateModuleBuilder(string assemblyName, AssemblyBuilder assemblyBuilder)
        {
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName, assemblyName + ".dll");
            return moduleBuilder;
        }

        private TypeBuilder CreateTypeBuilder(IEdmStructuredType type, IEdmModel model, ModuleBuilder moduleBuilder)
        {
            var baseType = typeof(DynamicEntityBase);
            if (type.BaseType != null)
            {
                var propertyType = EdmLibHelpers.GetClrType(type.BaseType, model);
                if (propertyType == null)
                {
                    var schemaElement = model.FindDeclaredType(type.BaseType.FullTypeName());
                    CreateType(model, schemaElement.FullName(), _moduleBuilders[schemaElement.Namespace]);
                    baseType = _types[schemaElement.FullName()];
                }
            }

            return moduleBuilder.DefineType(type.FullTypeName(), TypeAttributes.Class | TypeAttributes.Public, baseType);
        }

        private EnumBuilder CreateEnumBuilder(string typeName, ModuleBuilder moduleBuilder)
        {
            return moduleBuilder.DefineEnum(typeName, TypeAttributes.Public, typeof(int));
        }

        private void CreateEnumValue(string name, int value, EnumBuilder enumBuilder)
        {
            enumBuilder.DefineLiteral(name, value);
        }

        private void CreateProperty(IEdmProperty edmProperty, IEdmModel model, TypeBuilder typeBuilder, ILGenerator constructorIlGenerator, bool isKey)
        {
            var propertyType = GetPropertyType(edmProperty, model);

            var propertyName = edmProperty.Name;

            FieldBuilder fFirst;

            PropertyBuilder pFirst = CreateProperty(propertyName, propertyType, typeBuilder, out fFirst);
            SetDefaultValue(edmProperty, model, fFirst, constructorIlGenerator);

            if (isKey)
            {
                SetPropertyCustomAttribute(pFirst, typeof(KeyAttribute));
            }

            if (edmProperty.VocabularyAnnotations(model).Any(v => v.Term.FullName() == CoreVocabularyConstants.Computed))
            {
                SetPropertyCustomAttribute(pFirst, typeof(ComputedAttribute));
            }

            if (edmProperty.PropertyKind == EdmPropertyKind.Navigation)
            {
                var navigationProperty = (IEdmNavigationProperty)edmProperty;

                SetPropertyCustomAttribute(pFirst, typeof(JsonIgnoreAttribute));

                if (navigationProperty.Partner != null)
                {
                    SetNavigationPartnerAttribute(pFirst, navigationProperty.Partner.Name);
                }

                if (navigationProperty.ReferentialConstraint != null)
                {
                    foreach (var propertyPair in navigationProperty.ReferentialConstraint.PropertyPairs)
                    {
                        SetReferentialConstraintAttribute(pFirst, propertyPair.DependentProperty.Name,
                            propertyPair.PrincipalProperty.Name);
                    }

                }
                else
                {
                    if (edmProperty.Type.IsCollection())
                    {
                        CreateProperty(propertyName + "Ids", typeof(IList<string>), typeBuilder, out fFirst);
                        var listType = typeof(List<string>);
                        constructorIlGenerator.Emit(OpCodes.Ldarg_0);
                        constructorIlGenerator.Emit(OpCodes.Newobj, listType.GetConstructor(new Type[0]));
                        constructorIlGenerator.Emit(OpCodes.Stfld, fFirst);
                    }
                    else
                    {
                        CreateProperty(propertyName + "Id", typeof(string), typeBuilder, out fFirst);
                    }
                }
            }
        }

        private static PropertyBuilder CreateProperty(string propertyName, Type propertyType, TypeBuilder typeBuilder, out FieldBuilder fieldBuilder)
        {
            fieldBuilder = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder pFirst = typeBuilder.DefineProperty(propertyName,
                    PropertyAttributes.HasDefault, propertyType, null);

            //Getter
            MethodBuilder mFirstGet = typeBuilder.DefineMethod("get_" + propertyName, MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator firstGetIL = mFirstGet.GetILGenerator();

            firstGetIL.Emit(OpCodes.Ldarg_0);
            firstGetIL.Emit(OpCodes.Ldfld, fieldBuilder);
            firstGetIL.Emit(OpCodes.Ret);

            //Setter
            MethodBuilder mFirstSet = typeBuilder.DefineMethod("set_" + propertyName, MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.HideBySig, null, new Type[] { propertyType });

            ILGenerator firstSetIL = mFirstSet.GetILGenerator();

            firstSetIL.Emit(OpCodes.Ldarg_0);
            firstSetIL.Emit(OpCodes.Ldarg_1);
            firstSetIL.Emit(OpCodes.Stfld, fieldBuilder);
            firstSetIL.Emit(OpCodes.Ret);

            pFirst.SetGetMethod(mFirstGet);
            pFirst.SetSetMethod(mFirstSet);

            return pFirst;
        }

        private static void SetPropertyCustomAttribute(PropertyBuilder propertyBuilder, Type attributeType)
        {
            var attributeBuilder = new CustomAttributeBuilder(attributeType.GetConstructor(new Type[0]),
                new object[0]);
            propertyBuilder.SetCustomAttribute(attributeBuilder);
        }

        private static void SetNavigationPartnerAttribute(PropertyBuilder propertyBuilder, string partnerProperty)
        {
            var attributeBuilder = new CustomAttributeBuilder(typeof(NavigationPartnerAttribute).GetConstructor(new Type[] { typeof(string) }),
                new object[] { partnerProperty });
            propertyBuilder.SetCustomAttribute(attributeBuilder);
        }

        private static void SetReferentialConstraintAttribute(PropertyBuilder propertyBuilder, string dependantProperty, string principalProperty)
        {
            var attributeBuilder = new CustomAttributeBuilder(typeof(ReferentialConstraintAttribute).GetConstructor(new Type[] { typeof(string), typeof(string) }),
                new object[] { dependantProperty, principalProperty });
            propertyBuilder.SetCustomAttribute(attributeBuilder);
        }

        private Type GetPropertyType(IEdmProperty edmProperty, IEdmModel model)
        {
            var edmPropertyType = GetEdmType(edmProperty.Type);
            var propertyType = EdmLibHelpers.GetClrType(edmPropertyType, model);
            if (propertyType == null)
            {
                var schemaElement = model.FindDeclaredType(edmPropertyType.Definition.FullTypeName());
                CreateType(model, schemaElement.FullName(), _moduleBuilders[schemaElement.Namespace]);
                if (_typeBuilders.ContainsKey(schemaElement.FullName()))
                {
                    propertyType = _typeBuilders[schemaElement.FullName()];
                }
                else if (_enumBuilders.ContainsKey(schemaElement.FullName()))
                {
                    propertyType = _enumBuilders[schemaElement.FullName()];
                }
            }

            if (edmProperty.Type.IsCollection())
            {
                var listType = typeof(IList<>);
                propertyType = listType.MakeGenericType(propertyType);
            }
            return propertyType;
        }

        private IEdmTypeReference GetEdmType(IEdmTypeReference type)
        {
            if (type.IsCollection())
            {
                var collectionType = type.AsCollection();
                return collectionType.CollectionDefinition().ElementType;
            }
            else
            {
                return type; ;
            }
        }

        private void SetDefaultValue(IEdmProperty property, IEdmModel model, FieldBuilder builder, ILGenerator constructorIlGenerator)
        {
            if (!property.Type.IsNullable)
            {
                var propertyType = GetPropertyType(property, model);
                if (property.Type.IsComplex())
                {
                    constructorIlGenerator.Emit(OpCodes.Ldarg_0);
                    constructorIlGenerator.Emit(OpCodes.Newobj, propertyType.GetConstructor(new Type[0]));
                    constructorIlGenerator.Emit(OpCodes.Stfld, builder);
                }
                else if (property.Type.IsCollection())
                {
                    var listType = typeof(List<>);
                    var collectionType = listType.MakeGenericType(propertyType.GetGenericArguments()[0]);
                    constructorIlGenerator.Emit(OpCodes.Ldarg_0);
                    constructorIlGenerator.Emit(OpCodes.Newobj, collectionType.GetConstructor(new Type[0]));
                    constructorIlGenerator.Emit(OpCodes.Stfld, builder);
                }
            }
        }

        private ConstructorBuilder CreateConstructor(TypeBuilder typeBuilder)
        {
            ConstructorBuilder constructor = typeBuilder.DefineConstructor(
                                MethodAttributes.Public |
                                MethodAttributes.SpecialName |
                                MethodAttributes.RTSpecialName,
                                CallingConventions.Standard,
                                new Type[0]);

            return constructor;
        }
    }
}
