using System.Collections.Generic;

namespace Aether.Test
{
    public class EnumerableEqualityComparer<T> : EqualityComparer<IEnumerable<T>>
    {
        public IEqualityComparer<T> ElementComparer { get; private set; }

        public EnumerableEqualityComparer()
        {
            ElementComparer = EqualityComparer<T>.Default;
        }

        public EnumerableEqualityComparer(IEqualityComparer<T> elementComparer)
        {
            ElementComparer = elementComparer;
        }

        public override bool Equals(IEnumerable<T> x, IEnumerable<T> y)
        {
            if (x == null)
            {
                if (y == null)
                    return true;

                return false;
            }
            else if (y == null)
                return false;

            using IEnumerator<T> enumeratorX = x.GetEnumerator();
            using IEnumerator<T> enumeratorY = y.GetEnumerator();

            bool successMoveX = enumeratorX.MoveNext();
            bool successMoveY = enumeratorY.MoveNext();

            while (successMoveX && successMoveY)
            {
                if (ElementComparer.Equals(enumeratorX.Current, enumeratorY.Current) == false)
                {
                    UnityEngine.Debug.Log($"There are two different elements: {enumeratorX.Current} and {enumeratorY.Current}");
                    return false;
                }

                successMoveX = enumeratorX.MoveNext();
                successMoveY = enumeratorY.MoveNext();
            }

            if (successMoveX == successMoveY)
                return true;

            UnityEngine.Debug.Log("Enumerables have different sizes");
            return false;
        }

        public override int GetHashCode(IEnumerable<T> obj)
        {
            return obj.GetHashCode();
        }
    }
}