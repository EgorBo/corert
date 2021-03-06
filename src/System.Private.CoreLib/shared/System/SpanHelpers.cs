// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.InteropServices;

using Internal.Runtime.CompilerServices;

#if BIT64
using nuint = System.UInt64;
#else
using nuint = System.UInt32;
#endif

namespace System
{
    internal static partial class SpanHelpers
    {
        public static int IndexOfCultureHelper(ReadOnlySpan<char> span, ReadOnlySpan<char> value, CompareInfo compareInfo)
        {
            Debug.Assert(span.Length != 0);
            Debug.Assert(value.Length != 0);

            if (GlobalizationMode.Invariant)
            {
                return CompareInfo.InvariantIndexOf(span, value, ignoreCase: false);
            }

            return compareInfo.IndexOf(span, value, CompareOptions.None);
        }

        public static int IndexOfCultureIgnoreCaseHelper(ReadOnlySpan<char> span, ReadOnlySpan<char> value, CompareInfo compareInfo)
        {
            Debug.Assert(span.Length != 0);
            Debug.Assert(value.Length != 0);

            if (GlobalizationMode.Invariant)
            {
                return CompareInfo.InvariantIndexOf(span, value, ignoreCase: true);
            }

            return compareInfo.IndexOf(span, value, CompareOptions.IgnoreCase);
        }

        public static int IndexOfOrdinalHelper(ReadOnlySpan<char> span, ReadOnlySpan<char> value, bool ignoreCase)
        {
            Debug.Assert(span.Length != 0);
            Debug.Assert(value.Length != 0);

            if (GlobalizationMode.Invariant)
            {
                return CompareInfo.InvariantIndexOf(span, value, ignoreCase);
            }

            return CompareInfo.Invariant.IndexOfOrdinal(span, value, ignoreCase);
        }

        public static bool StartsWithCultureHelper(ReadOnlySpan<char> span, ReadOnlySpan<char> value, CompareInfo compareInfo)
        {
            Debug.Assert(value.Length != 0);

            if (GlobalizationMode.Invariant)
            {
                return span.StartsWith(value);
            }
            if (span.Length == 0)
            {
                return false;
            }
            return compareInfo.IsPrefix(span, value, CompareOptions.None);
        }

        public static bool StartsWithCultureIgnoreCaseHelper(ReadOnlySpan<char> span, ReadOnlySpan<char> value, CompareInfo compareInfo)
        {
            Debug.Assert(value.Length != 0);

            if (GlobalizationMode.Invariant)
            {
                return StartsWithOrdinalIgnoreCaseHelper(span, value);
            }
            if (span.Length == 0)
            {
                return false;
            }
            return compareInfo.IsPrefix(span, value, CompareOptions.IgnoreCase);
        }

        public static bool StartsWithOrdinalIgnoreCaseHelper(ReadOnlySpan<char> span, ReadOnlySpan<char> value)
        {
            Debug.Assert(value.Length != 0);

            if (span.Length < value.Length)
            {
                return false;
            }
            return CompareInfo.CompareOrdinalIgnoreCase(span.Slice(0, value.Length), value) == 0;
        }

        public static bool EndsWithCultureHelper(ReadOnlySpan<char> span, ReadOnlySpan<char> value, CompareInfo compareInfo)
        {
            Debug.Assert(value.Length != 0);

            if (GlobalizationMode.Invariant)
            {
                return span.EndsWith(value);
            }
            if (span.Length == 0)
            {
                return false;
            }
            return compareInfo.IsSuffix(span, value, CompareOptions.None);
        }

        public static bool EndsWithCultureIgnoreCaseHelper(ReadOnlySpan<char> span, ReadOnlySpan<char> value, CompareInfo compareInfo)
        {
            Debug.Assert(value.Length != 0);

            if (GlobalizationMode.Invariant)
            {
                return EndsWithOrdinalIgnoreCaseHelper(span, value);
            }
            if (span.Length == 0)
            {
                return false;
            }
            return compareInfo.IsSuffix(span, value, CompareOptions.IgnoreCase);
        }

        public static bool EndsWithOrdinalIgnoreCaseHelper(ReadOnlySpan<char> span, ReadOnlySpan<char> value)
        {
            Debug.Assert(value.Length != 0);

            if (span.Length < value.Length)
            {
                return false;
            }
            return (CompareInfo.CompareOrdinalIgnoreCase(span.Slice(span.Length - value.Length), value) == 0);
        }

        public static unsafe void ClearWithoutReferences(ref byte b, nuint byteLength)
        {
            if (byteLength == 0)
                return;

#if CORECLR && (AMD64 || ARM64)
            if (byteLength > 4096)
                goto PInvoke;
            Unsafe.InitBlockUnaligned(ref b, 0, (uint)byteLength);
            return;
#else
            // TODO: Optimize other platforms to be on par with AMD64 CoreCLR
            // Note: It's important that this switch handles lengths at least up to 22.
            // See notes below near the main loop for why.

            // The switch will be very fast since it can be implemented using a jump
            // table in assembly. See http://stackoverflow.com/a/449297/4077294 for more info.

            switch (byteLength)
            {
                case 1:
                    b = 0;
                    return;
                case 2:
                    Unsafe.As<byte, short>(ref b) = 0;
                    return;
                case 3:
                    Unsafe.As<byte, short>(ref b) = 0;
                    Unsafe.Add<byte>(ref b, 2) = 0;
                    return;
                case 4:
                    Unsafe.As<byte, int>(ref b) = 0;
                    return;
                case 5:
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.Add<byte>(ref b, 4) = 0;
                    return;
                case 6:
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    return;
                case 7:
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.Add<byte>(ref b, 6) = 0;
                    return;
                case 8:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    return;
                case 9:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    Unsafe.Add<byte>(ref b, 8) = 0;
                    return;
                case 10:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    return;
                case 11:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.Add<byte>(ref b, 10) = 0;
                    return;
                case 12:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    return;
                case 13:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.Add<byte>(ref b, 12) = 0;
                    return;
                case 14:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
                    return;
                case 15:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
                    Unsafe.Add<byte>(ref b, 14) = 0;
                    return;
                case 16:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
#endif
                    return;
                case 17:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
#endif
                    Unsafe.Add<byte>(ref b, 16) = 0;
                    return;
                case 18:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    return;
                case 19:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    Unsafe.Add<byte>(ref b, 18) = 0;
                    return;
                case 20:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    return;
                case 21:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    Unsafe.Add<byte>(ref b, 20) = 0;
                    return;
                case 22:
#if BIT64
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 20)) = 0;
                    return;
            }

            // P/Invoke into the native version for large lengths
            if (byteLength >= 512) goto PInvoke;

            nuint i = 0; // byte offset at which we're copying

            if ((Unsafe.As<byte, int>(ref b) & 3) != 0)
            {
                if ((Unsafe.As<byte, int>(ref b) & 1) != 0)
                {
                    Unsafe.AddByteOffset<byte>(ref b, i) = 0;
                    i += 1;
                    if ((Unsafe.As<byte, int>(ref b) & 2) != 0)
                        goto IntAligned;
                }
                Unsafe.As<byte, short>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                i += 2;
            }

            IntAligned:

            // On 64-bit IntPtr.Size == 8, so we want to advance to the next 8-aligned address. If
            // (int)b % 8 is 0, 5, 6, or 7, we will already have advanced by 0, 3, 2, or 1
            // bytes to the next aligned address (respectively), so do nothing. On the other hand,
            // if it is 1, 2, 3, or 4 we will want to copy-and-advance another 4 bytes until
            // we're aligned.
            // The thing 1, 2, 3, and 4 have in common that the others don't is that if you
            // subtract one from them, their 3rd lsb will not be set. Hence, the below check.

            if (((Unsafe.As<byte, int>(ref b) - 1) & 4) == 0)
            {
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                i += 4;
            }

            nuint end = byteLength - 16;
            byteLength -= i; // lower 4 bits of byteLength represent how many bytes are left *after* the unrolled loop

            // We know due to the above switch-case that this loop will always run 1 iteration; max
            // bytes we clear before checking is 23 (7 to align the pointers, 16 for 1 iteration) so
            // the switch handles lengths 0-22.
            Debug.Assert(end >= 7 && i <= end);

            // This is separated out into a different variable, so the i + 16 addition can be
            // performed at the start of the pipeline and the loop condition does not have
            // a dependency on the writes.
            nuint counter;

            do
            {
                counter = i + 16;

                // This loop looks very costly since there appear to be a bunch of temporary values
                // being created with the adds, but the jit (for x86 anyways) will convert each of
                // these to use memory addressing operands.

                // So the only cost is a bit of code size, which is made up for by the fact that
                // we save on writes to b.

#if BIT64
                Unsafe.As<byte, long>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                Unsafe.As<byte, long>(ref Unsafe.AddByteOffset<byte>(ref b, i + 8)) = 0;
#else
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i + 4)) = 0;
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i + 8)) = 0;
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i + 12)) = 0;
#endif

                i = counter;

                // See notes above for why this wasn't used instead
                // i += 16;
            }
            while (counter <= end);

            if ((byteLength & 8) != 0)
            {
#if BIT64
                Unsafe.As<byte, long>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
#else
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i + 4)) = 0;
#endif
                i += 8;
            }
            if ((byteLength & 4) != 0)
            {
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                i += 4;
            }
            if ((byteLength & 2) != 0)
            {
                Unsafe.As<byte, short>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                i += 2;
            }
            if ((byteLength & 1) != 0)
            {
                Unsafe.AddByteOffset<byte>(ref b, i) = 0;
                // We're not using i after this, so not needed
                // i += 1;
            }

            return;
#endif

        PInvoke:
            RuntimeImports.RhZeroMemory(ref b, byteLength);
        }

        public static unsafe void ClearWithReferences(ref IntPtr ip, nuint pointerSizeLength)
        {
            if (pointerSizeLength == 0)
                return;

            // TODO: Perhaps do switch casing to improve small size perf

            nuint i = 0;
            nuint n = 0;
            while ((n = i + 8) <= (pointerSizeLength))
            {
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 0) * (nuint)sizeof(IntPtr)) = default(IntPtr);
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 1) * (nuint)sizeof(IntPtr)) = default(IntPtr);
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 2) * (nuint)sizeof(IntPtr)) = default(IntPtr);
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 3) * (nuint)sizeof(IntPtr)) = default(IntPtr);
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 4) * (nuint)sizeof(IntPtr)) = default(IntPtr);
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 5) * (nuint)sizeof(IntPtr)) = default(IntPtr);
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 6) * (nuint)sizeof(IntPtr)) = default(IntPtr);
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 7) * (nuint)sizeof(IntPtr)) = default(IntPtr);
                i = n;
            }
            if ((n = i + 4) <= (pointerSizeLength))
            {
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 0) * (nuint)sizeof(IntPtr)) = default(IntPtr);
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 1) * (nuint)sizeof(IntPtr)) = default(IntPtr);
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 2) * (nuint)sizeof(IntPtr)) = default(IntPtr);
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 3) * (nuint)sizeof(IntPtr)) = default(IntPtr);
                i = n;
            }
            if ((n = i + 2) <= (pointerSizeLength))
            {
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 0) * (nuint)sizeof(IntPtr)) = default(IntPtr);
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 1) * (nuint)sizeof(IntPtr)) = default(IntPtr);
                i = n;
            }
            if ((i + 1) <= (pointerSizeLength))
            {
                Unsafe.AddByteOffset<IntPtr>(ref ip, (i + 0) * (nuint)sizeof(IntPtr)) = default(IntPtr);
            }
        }
    }
}
