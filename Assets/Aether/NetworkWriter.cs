using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if UNITY_EDITOR || UNITY_ANDROID
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Aether
{
    public class NetworkWriter
    {
        private const int c_defaultByteSize = 256;

        public static bool IsSerializable<T>()
            where T : unmanaged
        {
#if UNITY_EDITOR
            if (UnsafeUtility.IsBlittable(typeof(T)) == false)
                return false;
#endif
            StructLayoutAttribute structLayoutAttr = typeof(T).StructLayoutAttribute;

            return structLayoutAttr.Pack != 0 || structLayoutAttr.Value != LayoutKind.Sequential;
        }

        private byte[] m_buffer;

        public int Position { get; private set; }

        public NetworkWriter()
        {
            m_buffer = new byte[c_defaultByteSize];
        }

        public void WriteBytes(ArraySegment<byte> bytes)
        {
            EnsureCapacity(Position + bytes.Count);

            Array.ConstrainedCopy(bytes.Array, bytes.Offset, m_buffer, Position, bytes.Count);

            Position += bytes.Count;
        }

        /// <summary>
        /// Do not copy internal buffer
        /// </summary>
        /// <returns></returns>
        public ArraySegment<byte> ToArraySegment()
        {
            return new ArraySegment<byte>(m_buffer, 0, Position);
        }

        public void Clear()
        {
            Position = 0;
        }

        private void EnsureCapacity(int targetSize)
        {
            int currentLength = m_buffer.Length;

            if (currentLength >= targetSize)
                return;

            int resultSize = Math.Max(targetSize, currentLength * 2);

            Array.Resize(ref m_buffer, resultSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void WriteBlittable<T>(T value)
            where T : unmanaged
        {
            // check if serializable for safety
#if UNITY_EDITOR
            if (IsSerializable<T>() == false)
                ThrowHelper.ArgumentNonSerializableType(typeof(T));
#endif
            // calculate size
            //   sizeof(T) gets the managed size at compile time.
            //   Marshal.SizeOf<T> gets the unmanaged size at runtime (slow).
            // => our 1mio writes benchmark is 6x slower with Marshal.SizeOf<T>
            // => for blittable types, sizeof(T) is even recommended:
            // https://docs.microsoft.com/en-us/dotnet/standard/native-interop/best-practices
            int size = sizeof(T);

            // ensure capacity
            // NOTE that our runtime resizing comes at no extra cost because:
            // 1. 'has space' checks are necessary even for fixed sized writers.
            // 2. all writers will eventually be large enough to stop resizing.
            EnsureCapacity(Position + size);

            // write blittable
            fixed (byte* ptr = &m_buffer[Position])
            {
#if UNITY_ANDROID
                // on some android systems, assigning *(T*)ptr throws a NRE if
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
                T* valueBuffer = stackalloc T[1]{value};
                UnsafeUtility.MemCpy(ptr, valueBuffer, size);
#else
                // cast buffer to T* pointer, then assign value to the area
                *(T*)ptr = value;
#endif
            }

            Position += size;
        }
    }
}
