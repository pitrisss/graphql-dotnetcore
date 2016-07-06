﻿namespace GraphQLCore.Type.Translation
{
    using Exceptions;
    using Scalars;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Utils;

    public class TypeTranslator : ITypeTranslator
    {
        private Dictionary<Type, GraphQLScalarType> bindings;
        private ISchemaObserver schemaObserver;

        public TypeTranslator(ISchemaObserver schemaObserver)
        {
            this.bindings = new Dictionary<Type, GraphQLScalarType>();
            this.schemaObserver = schemaObserver;
            this.RegisterBindings();
            this.RegisterScalarsToSchemeObserver();
        }

        public IObjectTypeTranslator GetObjectTypeTranslatorFor(Type type)
        {
            var schemaType = this.schemaObserver.GetSchemaTypeFor(type);

            return this.GetObjectTypeTranslatorFor(schemaType);
        }

        public IObjectTypeTranslator GetObjectTypeTranslatorFor(GraphQLNullableType type)
        {
            return new ObjectTypeTranslator(type, this, this.schemaObserver);
        }

        public GraphQLScalarType GetType(Type type)
        {
            if (ReflectionUtilities.IsCollection(type))
                return new GraphQLList(this.GetType(ReflectionUtilities.GetCollectionMemberType(type)));

            if (this.bindings.ContainsKey(type))
                return this.bindings[type];

            return this.GetSchemaType(type);
        }

        public Type GetType(GraphQLScalarType type)
        {
            var reflectedType = this.bindings
                .Where(e => type.GetType() == e.Value.GetType())
                .Select(e => e.Key)
                .SingleOrDefault();

            if (reflectedType == null)
            {
                reflectedType = this.schemaObserver.GetTypeFor(type);
            }

            return Nullable.GetUnderlyingType(reflectedType) ?? reflectedType;
        }

        public GraphQLException[] IsValidLiteralValue(GraphQLScalarType inputType, Language.AST.GraphQLValue astValue)
        {
            if (inputType is GraphQLNonNullType)
            {
                if (astValue == null)
                {
                    return new GraphQLException[]
                    {
                        new GraphQLException($"Expected {inputType.Name ?? "non-null"} found null")
                    };
                }

                return this.IsValidLiteralValue(((GraphQLNonNullType)inputType).UnderlyingNullableType, astValue);
            }

            if (astValue == null)
                return new GraphQLException[] { };

            if (astValue.Kind == Language.AST.ASTNodeKind.Variable) //this method is checking only literals
                return new GraphQLException[] { };

            object value = TypeUtilities.GetValue(astValue);

            try
            {
                TypeUtilities.ConvertTo(value, this.GetType(inputType));
            }
            catch (Exception ex)
            {
                return new GraphQLException[] { new GraphQLException($"Expected {inputType.Name ?? "non-null"} found null") };
            }

            return new GraphQLException[] { };
        }

        private GraphQLScalarType GetSchemaType(Type type)
        {
            if (this.IsNullable(type))
                return this.schemaObserver.GetSchemaTypeFor(Nullable.GetUnderlyingType(type));

            if (this.IsValueType(type))
                return new GraphQLNonNullType(this.schemaObserver.GetSchemaTypeFor(type));

            return this.schemaObserver.GetSchemaTypeFor(type);
        }

        private bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        private bool IsValueType(Type type)
        {
            return
                ReflectionUtilities.IsStruct(type) ||
                ReflectionUtilities.IsEnum(type);
        }

        private void RegisterBinding<T>(GraphQLNullableType type)
        {
            this.bindings.Add(typeof(T), type);
        }

        private void RegisterBindings()
        {
            this.RegisterBinding<int?>(new GraphQLInt());
            this.RegisterBinding<float?>(new GraphQLFloat());
            this.RegisterBinding<bool?>(new GraphQLBoolean());
            this.RegisterBinding<string>(new GraphQLString());

            this.RegisterBinding<int>(new GraphQLNonNullType(new GraphQLInt()));
            this.RegisterBinding<float>(new GraphQLNonNullType(new GraphQLFloat()));
            this.RegisterBinding<bool>(new GraphQLNonNullType(new GraphQLBoolean()));
        }

        private void RegisterScalarsToSchemeObserver()
        {
            this.schemaObserver.AddKnownType(new GraphQLInt());
            this.schemaObserver.AddKnownType(new GraphQLFloat());
            this.schemaObserver.AddKnownType(new GraphQLBoolean());
            this.schemaObserver.AddKnownType(new GraphQLString());
        }
    }
}