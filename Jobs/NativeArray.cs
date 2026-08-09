using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace EngineX.Jobs
{
    public readonly unsafe struct NativeArray<T> : IDisposable, IEnumerable<T> where T : unmanaged
    {
        public readonly int Length;
        
        private readonly IntPtr _ptr;
        private readonly int _offset;
        private readonly bool _view;
        private readonly Allocator _allocator;

        private T* UnsafePointer => (T*)_ptr.ToPointer();

        public T this[int index]
        {
            get => UnsafePointer[_offset + index];
            set => UnsafePointer[_offset + index] = value;
        }

        public NativeArray(int length, Allocator allocator)
        {
            _ptr = Marshal.AllocHGlobal(sizeof(T) * length);
            Length = length;
            _offset = 0;
            _allocator = allocator;
            _view = false;
        }

        private NativeArray(IntPtr ptr, int length, int offset, Allocator allocator)
        {
            _ptr = ptr;
            Length = length;
            _offset = offset;
            _allocator = allocator;
            _view = true;
        }

        public NativeArray<T> GetView()
        {
            return new NativeArray<T>(_ptr, Length, 0, _allocator);
        }

        public NativeArray<T> GetSubArray(int start, int length)
        {
            return new NativeArray<T>(_ptr, length, start, _allocator);
        }

        public ref T GetRef(int index)
        {
            return ref UnsafePointer[index];
        }

        public T[] ToArray()
        {
            if (Length == 0) return Array.Empty<T>();
            var result = new T[Length];
            for (var i = 0; i < Length; i++) result[i] = this[i];
            return result;
        }

        public void Dispose()
        {
            if (_view) return;
            Marshal.FreeHGlobal(_ptr);
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private readonly NativeArray<T> _array;
            private int _index;

            internal Enumerator(NativeArray<T> array)
            {
                _array = array;
                _index = -1;
            }

            public T Current => _array[_index];

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveNext()
            {
                _index++;
                return _index < _array.Length;
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }
}