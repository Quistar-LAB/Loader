using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Loader.Utils {
    internal sealed class BufferPool {
        internal sealed class Buffer {
            internal readonly int m_size;
            [FixedAddressValueType]
            internal readonly byte[] m_buffer;
            internal bool m_available;
            internal Buffer(int size) {
                m_size = size;
                m_buffer = new byte[size];
                m_available = true;
            }
            internal bool IsAvailable(int size) => m_available && m_size >= size;
            internal Buffer Grab() {
                m_available = false;
                return this;
            }
        }
        private readonly List<Buffer> m_buffers;
        internal BufferPool() => m_buffers = new List<Buffer>();

        internal Buffer LeaseBuffer(int size) {
            Buffer buffer;
            List<Buffer> buffers = m_buffers;
            int len = buffers.Count;
            for (int i = 0; i < len; i++) {
                if (buffers[i].IsAvailable(size)) {
                    return buffers[i].Grab();
                }
            }
            buffer = new Buffer(size) {
                m_available = false
            };
            m_buffers.Add(buffer);
            return buffer;
        }

        internal void RelinquishBuffer(Buffer buffer) {
            buffer.m_available = true;
        }

        internal void Dispose(bool disposing) {
            if (disposing) {
                m_buffers.Clear();
            }
        }
    }
}
