using System;
using System.Collections.Generic;

namespace Aether
{
    public static class NetworkDiagnostics
    {
        public struct DataRecordInfo : IEquatable<DataRecordInfo>
        {
            public int totalSize;
            public int count;

            public DataRecordInfo(int totalSize, int count)
            {
                this.totalSize = totalSize;
                this.count = count;
            }

            public override bool Equals(object obj)
            {
                return obj is DataRecordInfo other && this == other;
            }

            public bool Equals(DataRecordInfo other)
            {
                return this == other;
            }

            public override int GetHashCode()
            {
                return totalSize ^ count;
            }

            public static bool operator ==(DataRecordInfo left, DataRecordInfo right)
            {
                return left.totalSize == right.totalSize && left.count == right.count;
            }

            public static bool operator !=(DataRecordInfo left, DataRecordInfo right)
            {
                return !(left == right);
            }
        }

        public class Record
        {
            private readonly Dictionary<string, DataRecordInfo> m_dictionary;

            public bool IsActive { get; set; }

            public Record(bool isActive)
            {
                IsActive = isActive;
                m_dictionary = new Dictionary<string, DataRecordInfo>();
            }

            /// <summary>
            /// Add if only this record is active
            /// </summary>
            public void TryAdd(string handlerName, int size)
            {
                if (IsActive == false) return;

                if (m_dictionary.ContainsKey(handlerName) == false)
                {
                    m_dictionary[handlerName] = new DataRecordInfo(size, count: 1);
                    return;
                }

                var lastValue = m_dictionary[handlerName];
                lastValue.count++;
                lastValue.totalSize += size;

                m_dictionary[handlerName] = lastValue;
            }

            public DataRecordInfo GetHandlerInfo(string handlerName)
            {
                return m_dictionary[handlerName];
            }

            public void Clear()
            {
                m_dictionary.Clear();
            }
        }

        private static readonly Record s_handledDataRecord = new(isActive: false);
        private static readonly Record s_sentDataRecord = new(isActive: false);

        public static Record HandledDataRecord => s_handledDataRecord;

        public static Record SentDataRecord => s_sentDataRecord;
    }
}
