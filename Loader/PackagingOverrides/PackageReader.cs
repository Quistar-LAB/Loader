using ColossalFramework.Packaging;
using Loader.Utils;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;

namespace Loader.PackagingOverrides {
    public sealed unsafe class PackageReader : IDisposable {
        private static UTF8Encoding m_encoding;
        private static Decoder m_decoder;
        private static char[] m_charBuffer;
        private static int m_maxCharsSize;
        private static bool m_active;
        private static bool m_threadActive = false;
        private static BufferPool m_bufPools;
        private static Dictionary<string, Texture2D> m_loadedTextures;
        private static Dictionary<string, Mesh> m_loadedMeshes;
        private static Dictionary<string, Material> m_loadedMaterial;
        private Stream m_stream;
        private readonly BufferPool.Buffer m_leasedBuffer;
        private readonly byte* m_ptrOrigin;
        private byte* m_ptrBuf;

        private static void BufferHandler(object ptr) {
            const int fiveMinutes = 1000 * 60 * 5;
            while (m_active) {
                Thread.Sleep(fiveMinutes);
            }
            m_decoder = null;
            m_encoding = null;
            m_charBuffer = null;
            m_threadActive = false;
            m_loadedTextures = null;
            m_loadedMeshes = null;
            m_loadedMaterial = null;
            m_bufPools.Dispose(true);
            m_bufPools = null;
        }

        public PackageReader(Stream stream) {
            int bufferSize = (int)stream.Length;
            m_active = true;
            if (!m_threadActive) {
                m_threadActive = true;
                ThreadPool.QueueUserWorkItem(BufferHandler);
            }
            if(m_loadedTextures is null) {
                m_loadedTextures = new Dictionary<string, Texture2D>(256);
            }
            if(m_loadedMeshes is null) {
                m_loadedMeshes = new Dictionary<string, Mesh>(256);
            }
            if(m_loadedMaterial is null) {
                m_loadedMaterial = new Dictionary<string, Material>(256);
            }
            BufferPool bufPools = m_bufPools;
            if (bufPools is null) {
                bufPools = new BufferPool();
                m_bufPools = bufPools;
            }
            BufferPool.Buffer leasedBuf = bufPools.LeaseBuffer(bufferSize);
            m_leasedBuffer = leasedBuf;
            byte[] buffer = leasedBuf.m_buffer;
            fixed (byte* ptr = &buffer[0]) {
                m_ptrBuf = ptr;
                m_ptrOrigin = ptr;
            }
            m_stream = stream;
            UTF8Encoding encoding = m_encoding;
            if (encoding is null) {
                encoding = new UTF8Encoding();
                m_encoding = encoding;
                m_decoder = encoding.GetDecoder();
                m_maxCharsSize = encoding.GetMaxCharCount(4096);
                m_charBuffer = new char[m_maxCharsSize];
            }
            int numBytesToRead = bufferSize;
            int offset = 0;
            while (numBytesToRead > 0) {
                int numBytesRead = stream.Read(buffer, offset, numBytesToRead);
                if (numBytesRead == 0) {
                    break;
                }
                offset += numBytesRead;
                numBytesToRead -= numBytesRead;
            }
        }

        public object ReadUnityType(Type type, Package package = null) {
            if (type.IsEnum) {
                Type underlyingType = Enum.GetUnderlyingType(type);
                if (underlyingType == typeof(int)) {
                    return ReadInt32();
                }
                if (underlyingType == typeof(byte)) {
                    return ReadByte();
                }
                if (underlyingType == typeof(sbyte)) {
                    return ReadSByte();
                }
                if (underlyingType == typeof(short)) {
                    return ReadInt16();
                }
                if (underlyingType == typeof(ushort)) {
                    return ReadUInt16();
                }
                if (underlyingType == typeof(uint)) {
                    return ReadUInt32();
                }
                if (underlyingType == typeof(long)) {
                    return ReadInt64();
                }
                if (underlyingType == typeof(ulong)) {
                    return ReadUInt64();
                }
                throw new MissingMethodException("Dunno what to do with " + type.Name + "(Did you forget to add this type to ReadUnityType()?)");
            } else {
                if (type == typeof(bool)) {
                    return ReadBoolean();
                }
                if (type == typeof(byte)) {
                    return ReadByte();
                }
                if (type == typeof(int)) {
                    return ReadInt32();
                }
                if (type == typeof(uint)) {
                    return ReadUInt32();
                }
                if (type == typeof(ulong)) {
                    return ReadUInt64();
                }
                if (type == typeof(float)) {
                    return ReadSingle();
                }
                if (type == typeof(string)) {
                    return ReadString();
                }
                if (type == typeof(bool[])) {
                    return ReadBooleanArray();
                }
                if (type == typeof(byte[])) {
                    return ReadByteArray();
                }
                if (type == typeof(int[])) {
                    return ReadInt32Array();
                }
                if (type == typeof(float[])) {
                    return ReadFloatArray();
                }
                if (type == typeof(string[])) {
                    return ReadStringArray();
                }
                if (type == typeof(DateTime)) {
                    return ReadDateTime();
                }
                if (type == typeof(Package.Asset)) {
                    return ReadAsset(package);
                }
                if (type == typeof(Vector2)) {
                    return ReadVector2();
                }
                if (type == typeof(Vector3)) {
                    return ReadVector3();
                }
                if (type == typeof(Vector4)) {
                    return ReadVector4();
                }
                if (type == typeof(Color)) {
                    return ReadColor();
                }
                if (type == typeof(Matrix4x4)) {
                    return ReadMatrix4x4();
                }
                if (type == typeof(Quaternion)) {
                    return ReadQuaternion();
                }
                if (type == typeof(Vector2[])) {
                    return ReadVector2Array();
                }
                if (type == typeof(Vector3[])) {
                    return ReadVector3Array();
                }
                if (type == typeof(Vector4[])) {
                    return ReadVector4Array();
                }
                if (type == typeof(Color[])) {
                    return ReadColorArray();
                }
                if (type == typeof(Matrix4x4[])) {
                    return ReadMatrix4x4Array();
                }
                if (type == typeof(Quaternion[])) {
                    return ReadQuaternionArray();
                }
                throw new MissingMethodException("Dunno what to do with " + type.Name + "(Did you forget to add this type to ReadUnityType()?)");
            }
        }

        public Texture2D ReadTexture(Package preferredPackage = null) {
            Dictionary<string, Texture2D> loadedTextures = m_loadedTextures;
            string checksum = ReadString();
            if(LoaderSettings.ShareTextures && loadedTextures.TryGetValue(checksum, out Texture2D texture)) {
                return texture;
            }
            Package.Asset asset = null;
            if (!(preferredPackage is null)) {
                asset = preferredPackage.FindByChecksum(checksum);
            }
            if(asset is null) {
                asset = PackageManager.FindAssetByChecksum(checksum);
            }
            texture = asset.Instantiate<Texture2D>();
            loadedTextures.Add(checksum, texture);
            return texture;
        }

        public Mesh ReadMesh(Package preferredPackage = null) {
            Dictionary<string, Mesh> loadedMeshes = m_loadedMeshes;
            string checksum = ReadString();
            if(LoaderSettings.ShareMeshes && loadedMeshes.TryGetValue(checksum, out Mesh mesh)) {
                return mesh;
            }
            Package.Asset asset = null;
            if (!(preferredPackage is null)) {
                asset = preferredPackage.FindByChecksum(checksum);
            }
            if (asset is null) {
                asset = PackageManager.FindAssetByChecksum(checksum);
            }
            mesh = asset.Instantiate<Mesh>();
            loadedMeshes.Add(checksum, mesh);
            return mesh;
        }

        public Material ReadMaterial(Package preferredPackage = null) {
            Dictionary<string, Material> loadedMaterial = m_loadedMaterial;
            string checksum = ReadString();
            if (LoaderSettings.ShareMaterials && loadedMaterial.TryGetValue(checksum, out Material material)) {
                return material;
            }
            Package.Asset asset = null;
            if (!(preferredPackage is null)) {
                asset = preferredPackage.FindByChecksum(checksum);
            }
            if (asset is null) {
                asset = PackageManager.FindAssetByChecksum(checksum);
            }
            material = asset.Instantiate<Material>();
            loadedMaterial.Add(checksum, material);
            return material;
        }

        public Package.Asset ReadAsset(Package preferredPackage = null) {
            string checksum = ReadString();
            if (!(preferredPackage is null)) {
                Package.Asset asset = preferredPackage.FindByChecksum(checksum);
                if (!(asset is null)) {
                    return asset;
                }
            }
            return PackageManager.FindAssetByChecksum(checksum);
        }

        public DateTime ReadDateTime() => DateTime.Parse(ReadString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        public Vector2 ReadVector2() {
            Vector2* vptr = (Vector2*)m_ptrBuf;
            m_ptrBuf = (byte*)(vptr + 1);
            return *vptr;
        }

        public Vector3 ReadVector3() {
            Vector3* vptr = (Vector3*)m_ptrBuf;
            m_ptrBuf = (byte*)(vptr + 1);
            return *vptr;
        }

        public Vector4 ReadVector4() {
            Vector4* vptr = (Vector4*)m_ptrBuf;
            m_ptrBuf = (byte*)(vptr + 1);
            return *vptr;
        }

        public Color ReadColor() {
            Color* ptr = (Color*)m_ptrBuf;
            m_ptrBuf = (byte*)(ptr + 1);
            return *ptr;
        }

        public Quaternion ReadQuaternion() {
            Quaternion* ptr = (Quaternion*)m_ptrBuf;
            m_ptrBuf = (byte*)(ptr + 1);
            return *ptr;
        }

        public BoneWeight ReadBoneWeight() {
            BoneWeight* ptr = (BoneWeight*)m_ptrBuf;
            m_ptrBuf = (byte*)(ptr + 1);
            return *ptr;
        }

        public Matrix4x4 ReadMatrix4x4() {
            Matrix4x4* mptr = (Matrix4x4*)m_ptrBuf;
            m_ptrBuf = (byte*)(mptr + 1);
            return *mptr;
        }

        public Vector3[] ReadVector3Array() {
            int* iptr = (int*)m_ptrBuf;
            int len = *iptr++;
            Vector3* vptr = (Vector3*)iptr;
            Vector3[] array = new Vector3[len];
            for (int i = 0; i < array.Length; i++) {
                array[i] = *vptr++;
            }
            m_ptrBuf = (byte*)vptr;
            return array;
        }

        public Vector2[] ReadVector2Array() {
            int* iptr = (int*)m_ptrBuf;
            int len = *iptr++;
            Vector2* vptr = (Vector2*)iptr;
            Vector2[] array = new Vector2[len];
            for (int i = 0; i < array.Length; i++) {
                array[i] = *vptr++;
            }
            m_ptrBuf = (byte*)vptr;
            return array;
        }

        public Vector4[] ReadVector4Array() {
            int* iptr = (int*)m_ptrBuf;
            int len = *iptr++;
            Vector4* vptr = (Vector4*)iptr;
            Vector4[] array = new Vector4[len];
            for (int i = 0; i < array.Length; i++) {
                array[i] = *vptr++;
            }
            m_ptrBuf = (byte*)vptr;
            return array;
        }

        public Color[] ReadColorArray() {
            int* iptr = (int*)m_ptrBuf;
            int len = *iptr++;
            Color* cptr = (Color*)iptr;
            Color[] array = new Color[len];
            for (int i = 0; i < array.Length; i++) {
                array[i] = *cptr++;
            }
            m_ptrBuf = (byte*)cptr;
            return array;
        }

        public Matrix4x4[] ReadMatrix4x4Array() {
            int* iptr = (int*)m_ptrBuf;
            int len = *iptr++;
            Matrix4x4* mptr = (Matrix4x4*)iptr;
            Matrix4x4[] array = new Matrix4x4[len];
            for (int i = 0; i < array.Length; i++) {
                array[i] = *mptr++;
            }
            m_ptrBuf = (byte*)mptr;
            return array;
        }

        public Quaternion[] ReadQuaternionArray() {
            int* iptr = (int*)m_ptrBuf;
            int len = *iptr++;
            Quaternion* qptr = (Quaternion*)iptr;
            Quaternion[] array = new Quaternion[len];
            for (int i = 0; i < array.Length; i++) {
                array[i] = *qptr++;
            }
            m_ptrBuf = (byte*)qptr;
            return array;
        }

        public BoneWeight[] ReadBoneWeightsArray() {
            int* iptr = (int*)m_ptrBuf;
            int len = *iptr++;
            BoneWeight* bptr = (BoneWeight*)iptr;
            BoneWeight[] array = new BoneWeight[len];
            for (int i = 0; i < array.Length; i++) {
                array[i] = *bptr++;
            }
            m_ptrBuf = (byte*)bptr;
            return array;
        }

        public string[] ReadStringArray() {
            int num = ReadInt32();
            string[] array = new string[num];
            for (int i = 0; i < array.Length; i++) {
                array[i] = ReadString();
            }
            return array;
        }

        public float[] ReadFloatArray() {
            float* ptr = (float*)m_ptrBuf;
            int len = *(int*)ptr++;
            float[] array = new float[len];
            for (int i = 0; i < array.Length; i++) {
                array[i] = *ptr++;
            }
            m_ptrBuf = (byte*)ptr;
            return array;
        }

        public bool[] ReadBooleanArray() {
            int num = ReadInt32();
            bool[] array = new bool[num];
            for (int i = 0; i < array.Length; i++) {
                array[i] = ReadBoolean();
            }
            return array;
        }

        public int[] ReadInt32Array() {
            int* ptr = (int*)m_ptrBuf;
            int len = *ptr++;
            int[] array = new int[len];
            for (int i = 0; i < array.Length; i++) {
                array[i] = *ptr++;
            }
            m_ptrBuf = (byte*)ptr;
            return array;
        }

        public BuildingInfo.TrafficLights[] ReadBuilidngLights() {
            BuildingInfo.TrafficLights* ptr = (BuildingInfo.TrafficLights*)m_ptrBuf;
            int len = *(int*)ptr++;
            BuildingInfo.TrafficLights[] array = new BuildingInfo.TrafficLights[len];
            for (int i = 0; i < array.Length; i++) {
                array[i] = *ptr++;
            }
            m_ptrBuf = (byte*)ptr;
            return array;
        }

        public byte ReadByte() => *m_ptrBuf++;

        public sbyte ReadSByte() => (sbyte)*m_ptrBuf++;

        public byte[] ReadBytes(int count) {
            byte[] array = new byte[count];
            int offset = (int)(m_ptrBuf - m_ptrOrigin);
            Buffer.BlockCopy(m_leasedBuffer.m_buffer, offset, array, 0, count);
            m_ptrBuf += count;
            return array;
        }

        public byte[] ReadByteArray() {
            int len = ReadInt32();
            byte[] array = new byte[len];
            int offset = (int)(m_ptrBuf - m_ptrOrigin);
            Buffer.BlockCopy(m_leasedBuffer.m_buffer, offset, array, 0, len);
            m_ptrBuf += len;
            return array;
        }

        public ushort ReadUInt16() {
            ushort* ptr = (ushort*)m_ptrBuf;
            m_ptrBuf = (byte*)(ptr + 1);
            return *ptr;
        }

        public short ReadInt16() {
            short* ptr = (short*)m_ptrBuf;
            m_ptrBuf = (byte*)(ptr + 1);
            return *ptr;
        }

        public int ReadInt32() {
            int* ptr = (int*)m_ptrBuf;
            m_ptrBuf = (byte*)(ptr + 1);
            return *ptr;
        }

        public uint ReadUInt32() {
            uint* ptr = (uint*)m_ptrBuf;
            m_ptrBuf = (byte*)(ptr + 1);
            return *ptr;
        }

        public long ReadInt64() {
            long* ptr = (long*)m_ptrBuf;
            m_ptrBuf = (byte*)(ptr + 1);
            return *ptr;
        }

        public ulong ReadUInt64() {
            ulong* ptr = (ulong*)m_ptrBuf;
            m_ptrBuf = (byte*)(ptr + 1);
            return *ptr;
        }

        public bool ReadBoolean() => *m_ptrBuf++ != 0;

        public unsafe float ReadSingle() {
            float* ptr = (float*)m_ptrBuf;
            m_ptrBuf = (byte*)(ptr + 1);
            return *ptr;
        }

        public string ReadString() {
            byte* ptr = m_ptrBuf;
            int bufLen = 0;
            int shift = 0;
            while(shift != 35) {
                byte b = *ptr++;
                bufLen |= (b & 0x7f) << shift;
                shift += 7;
                if((b & 0x80) == 0) {
                    if(bufLen < 0) {
                        throw new IOException(string.Format("IO_InvalidStringLen_Len {0}", new object[] {
                            bufLen
                        }));
                    }
                    if (bufLen == 0) {
                        m_ptrBuf = ptr;
                        return string.Empty;
                    }
                    int chars = m_decoder.GetChars(m_leasedBuffer.m_buffer, (int)(ptr - m_ptrOrigin), bufLen, m_charBuffer, 0);
                    m_ptrBuf = ptr + bufLen;
                    return new string(m_charBuffer, 0, chars);
                }
            }
            throw new FormatException("Error reading string data: Bad 7Bit Int32 Length");
        }

        private static int ParseChecksum(string checksum) {
            int result = 0;
            for(int i = 0; i < checksum.Length; i++) {
                result = result * 10 + (checksum[i] - '0');
            }
            return result;
        }

        private void Dispose(bool disposing) {
            if (disposing) {
                Stream stream = m_stream;
                m_stream = null;
                if (!(stream is null)) {
                    stream.Close();
                }
            }
            m_active = false;
            m_leasedBuffer.m_available = true;
        }

        void IDisposable.Dispose() {
            Dispose(true);
        }
    }
}
