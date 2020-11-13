// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Squidex.Assets
{
    public static class Guard
    {
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotEmpty<TType>(IReadOnlyCollection<TType>? target, string parameterName)
        {
            NotNull(target, parameterName);

            if (target != null && target.Count == 0)
            {
                throw new ArgumentException("Collection does not contain an item.", parameterName);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GreaterThan<TValue>(TValue target, TValue lower, string parameterName) where TValue : IComparable
        {
            if (target.CompareTo(lower) <= 0)
            {
                throw new ArgumentException($"Value must be greater than {lower}", parameterName);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GreaterEquals<TValue>(TValue target, TValue lower, string parameterName) where TValue : IComparable
        {
            if (target.CompareTo(lower) < 0)
            {
                throw new ArgumentException($"Value must be greater or equal to {lower}", parameterName);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotEmpty(Guid target, string parameterName)
        {
            if (target == Guid.Empty)
            {
                throw new ArgumentException("Value cannot be empty.", parameterName);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull(object? target, string parameterName)
        {
            if (target == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotDefault<T>(T target, string parameterName)
        {
            if (Equals(target, default(T)!))
            {
                throw new ArgumentException("Value cannot be an the default value.", parameterName);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNullOrEmpty(string? target, string parameterName)
        {
            NotNull(target, parameterName);

            if (string.IsNullOrWhiteSpace(target))
            {
                throw new ArgumentException("String parameter cannot be null or empty and cannot contain only blanks.", parameterName);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidFileName(string? target, string parameterName)
        {
            NotNullOrEmpty(target, parameterName);

            if (target.Intersect(Path.GetInvalidFileNameChars()).Any())
            {
                throw new ArgumentException("Value contains an invalid character.", parameterName);
            }
        }
    }
}
