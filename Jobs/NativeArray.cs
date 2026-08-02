using System;
using System.Collections;
using System.Collections.Generic;

namespace EngineX.Jobs
{
    public struct NativeArray<T> : IDisposable, IEquatable<NativeArray<T>>, IEnumerable<T> where T : struct
    {
        internal T[] Buffer;
        internal int Offset;
        internal int Length_;
        internal Allocator Allocator;

        public NativeArray(int length, Allocator allocator)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (allocator == Allocator.Invalid) throw new ArgumentException("Allocator.Invalid not allowed", nameof(allocator));
            Buffer = new T[length];
            Offset = 0;
            Length_ = length;
            Allocator = allocator;
        }

        public int Length => Length_;

        public bool IsCreated => Buffer != null;

        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Length_) throw new IndexOutOfRangeException();
                return Buffer[Offset + index];
            }
            set
            {
                if ((uint)index >= (uint)Length_) throw new IndexOutOfRangeException();
                Buffer[Offset + index] = value;
            }
        }

        public NativeArray<T> GetSubArray(int start, int length)
        {
            if (start < 0 || length < 0 || start + length > Length_) throw new ArgumentOutOfRangeException();
            return new NativeArray<T>
            {
                Buffer = Buffer,
                Offset = Offset + start,
                Length_ = length,
                Allocator = Allocator.None,
            };
        }

        public T[] ToArray()
        {
            if (Length_ == 0) return Array.Empty<T>();
            var result = new T[Length_];
            Array.Copy(Buffer, Offset, result, 0, Length_);
            return result;
        }

        public void Dispose()
        {
            if (Buffer != null && (Allocator == Allocator.Persistent || Allocator == Allocator.TempJob))
            {
                Buffer = null;
                Offset = 0;
                Length_ = 0;
                Allocator = Allocator.Invalid;
            }
        }

        public bool Equals(NativeArray<T> other)
        {
            return ReferenceEquals(Buffer, other.Buffer) && Offset == other.Offset && Length_ == other.Length_;
        }

        public override bool Equals(object obj)
        {
            return obj is NativeArray<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Buffer ?? Array.Empty<T>()),
                Offset,
                Length_);
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
                return _index < _array.Length_;
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }
}