using System;
using System.IO;
using System.Runtime.CompilerServices;
#if UNITY_EDITOR || UNITY_ANDROID
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Aether
{
    public class NetworkReader
    {
        private readonly ArraySegment<byte> m_buffer;

        public int Position { get; private set; }

        public int Length => m_buffer.Count;

        public int Remaining => Length - Position;

        public NetworkReader(ArraySegment<byte> data)
        {
            m_buffer = data;
        }

        public ArraySegment<byte> ReadBytes(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (Position + count > m_buffer.Count)
                throw new EndOfStreamException(nameof(ReadBytes));

            ArraySegment<byte> result = m_buffer.Slice(Position, count);

            Position += count;

            return result;
        }

        /// <summary>
        /// Do not copy internal buffer
        /// </summary>
        /// <returns></returns>
        public ArraySegment<byte> ToArraySegment()
        {
            return m_buffer.Slice(Position);
        }

        // ReadBlittable<T> from DOTSNET
        // this is extremely fast, but only works for blittable types.
        // => private to make sure nobody accidentally uses it for non-blittable
        //
        // Note:
        //   ReadBlittable assumes same endianness for server & client.
        //   All Unity 2018+ platforms are little endian.
        //
        // This is not safe to expose to random structs.
        //   * StructLayout.Sequential is the default, which is safe.
        //     if the struct contains a reference type, it is converted to Auto.
        //     but since all structs here are unmanaged blittable, it's safe.
        //     see also: https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.layoutkind?view=netframework-4.8#system-runtime-interopservices-layoutkind-sequential
        //   * StructLayout.Pack depends on CPU word size.
        //     this may be different 4 or 8 on some ARM systems, etc.
        //     this is not safe, and would cause bytes/shorts etc. to be padded.
        //     see also: https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.structlayoutattribute.pack?view=net-6.0
        //   * If we force pack all to '1', they would have no padding which is
        //     great for bandwidth. but on some android systems, CPU can't read
        //     unaligned memory.
        //   * The only option would be to force explicit layout with multiples
        //     of word size.
        //
        // Note: inlining ReadBlittable is enough. don't inline ReadInt etc.
        //       we don't want ReadBlittable to be copied in place everywhere.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe T ReadBlittable<T>()
            where T : unmanaged
        {
            // check if serializable for safety
#if UNITY_EDITOR
            if (NetworkWriter.IsSerializable<T>() == false)
                ThrowHelper.ArgumentNonSerializableType(typeof(T));
#endif

            // calculate size
            //   sizeof(T) gets the managed size at compile time.
            //   Marshal.SizeOf<T> gets the unmanaged size at runtime (slow).
            // => for blittable types, sizeof(T) is even recommended:
            // https://docs.microsoft.com/en-us/dotnet/standard/native-interop/best-practices
            int size = sizeof(T);

            // enough data to read?
            if (Position + size > m_buffer.Count)
            {
                throw new EndOfStreamException($"{nameof(ReadBlittable)}<{typeof(T)}>");
            }

            // read blittable
            T value;
            fixed (byte* ptr = &m_buffer.Array[m_buffer.Offset + Position])
            {
#if UNITY_ANDROID
                // on some android systems, reading *(T*)ptr throws a NRE if
                // the ptr isn't aligned (i.e. if Position is 1,2,3,5, etc.).
                // here we have to use memcpy.
                //
                // => we can't get a pointer of a struct in C# without
                //    marshalling allocations
                // => instead, we stack allocate an array of type T and use that
                // => stackalloc avoids GC and is very fast. it only works for
                //    value types, but all blittable types are anyway.
                //
                // this way, we can still support blittable reads on android.
                // see also: https://github.com/vis2k/Mirror/issues/3044
                // (solution discovered by AIIO, FakeByte, mischa)
                T* valueBuffer = stackalloc T[1];
                UnsafeUtility.MemCpy(valueBuffer, ptr, size);
                value = valueBuffer[0];
#else
                // cast buffer to a T* pointer and then read from it.
                value = *(T*)ptr;
#endif
            }

            Position += size;
            return value;
        }
    }
}
