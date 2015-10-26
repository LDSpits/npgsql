// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Npgsql;
using Npgsql.BackendMessages;
using Npgsql.TypeHandlers.NumericHandlers;
using NpgsqlTypes;

// ReSharper disable once CheckNamespace
namespace Microsoft.Data.Entity.Storage.Internal
{
    // TODO: Provider-specific types?
    // TODO: BIT(1) vs. BIT(N)
    // TODO: Enums? Ranges?
    // TODO: Arrays? But this would conflict with navigation...
    public class NpgsqlTypeMapper : RelationalTypeMapper
    {
        readonly Dictionary<string, RelationalTypeMapping> _simpleNameMappings;
        readonly Dictionary<Type, RelationalTypeMapping> _simpleMappings;

        public NpgsqlTypeMapper()
        {
            // Reflect over Npgsql's type mappings and generate EF7 type mappings from them

            _simpleNameMappings = TypeHandlerRegistry.HandlerTypes.Values
                .Where(tam => tam.Mapping.NpgsqlDbType.HasValue)
                .ToDictionary(
                    tam => tam.Mapping.PgName,
                    tam => (RelationalTypeMapping)new NpgsqlTypeMapping(tam.Mapping.PgName, GetTypeHandlerTypeArgument(tam.HandlerType), tam.Mapping.NpgsqlDbType.Value)
                );

            _simpleMappings = TypeHandlerRegistry.HandlerTypes.Values
                .Select(tam => tam.Mapping)
                .Where(m => m.NpgsqlDbType.HasValue)
                .SelectMany(m => m.Types, (m, t) => (RelationalTypeMapping)new NpgsqlTypeMapping(m.PgName, t, m.NpgsqlDbType.Value))
                .ToDictionary(m => m.ClrType, m => m);
        }

        protected override string GetColumnType(IProperty property) => property.Npgsql().ColumnType;

        protected override IReadOnlyDictionary<Type, RelationalTypeMapping> SimpleMappings
            => _simpleMappings;

        protected override IReadOnlyDictionary<string, RelationalTypeMapping> SimpleNameMappings
            => _simpleNameMappings;

        static Type GetTypeHandlerTypeArgument(Type handler)
        {
            while (!handler.IsGenericType || handler.GetGenericTypeDefinition() != typeof(TypeHandler<>))
            {
                handler = handler.BaseType;
                if (handler == null)
                {
                    throw new Exception("Npgsql type handler doesn't inherit from TypeHandler<>?");
                }
            }

            return handler.GetGenericArguments()[0];
        }
    }
}
