using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheHelper.Helpers {
    #region ----- SerializationHelper -----
    public static class SerializationHelper {
        #region --- constants ---
        internal static string _SerializationDelimiter_ = "|";
        #endregion --- constants ---

        #region --- Serialization ---
        /// <summary>
        /// Serializes the object using binary formatter.
        /// </summary>
        /// <param name="obj">Object to be serialized. It must be serializable.</param>
        /// <returns>Base64 string representation of the binary serialization of the object.</returns>
        public static string Object_SerializeBinary(object obj) {
            string resStr = string.Empty;
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            System.IO.MemoryStream _ms = new System.IO.MemoryStream(1024);
            try {
                formatter.Serialize(_ms, obj);
                byte[] byteArray = new byte[_ms.Length];
                _ms.Position = 0;
                int res = _ms.Read(byteArray, 0, (int)_ms.Length);
                if (res != -1)
                    resStr = System.Convert.ToBase64String(byteArray);
            }
            catch (Exception e) {
                throw new Exception(string.Format("SerializationHelper.Object_SerializeBinary:: Failed to serialize {0} object. Reason: {1}", obj.GetType().ToString(), e.Message), e);
            }
            finally {
                _ms.Close();
            }
            return resStr;
        } // Object_SerializeBinary

        /// <summary>
        /// Serializes the object using binary formatter.
        /// </summary>
        /// <param name="obj">Object to be serialized. It must be serializable.</param>
        /// <returns>Byte array representation of the binary serialization of the object.</returns>
        public static byte[] Object_SerializeBinaryArray(object obj) {
            byte[] byteArray;
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            System.IO.MemoryStream _ms = new System.IO.MemoryStream(1024);
            try {
                formatter.Serialize(_ms, obj);
                byteArray = new byte[_ms.Length];
                _ms.Position = 0;
                int res = _ms.Read(byteArray, 0, (int)_ms.Length);
            }
            catch (Exception e) {
                throw new Exception(string.Format("SerializationHelper.Object_SerializeBinaryArray:: Failed to serialize {0} object. Reason: {1}", obj.GetType().ToString(), e.Message), e);
            }
            finally {
                _ms.Close();
            }
            return byteArray;
        } // Object_SerializeBinaryArray

        /// <summary>
        /// Deserializes the object using binary formatter.
        /// </summary>
        /// <param name="serializedStr">Base64 string representation of the binary serialization of the object.</param>
        /// <returns>Deserialized object.</returns>
        public static object Object_DeSerializeBinary(string serializedStr) {
            object obj = new object();
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            System.IO.MemoryStream _ms = new System.IO.MemoryStream(1024);
            try {
                _ms.Write(System.Convert.FromBase64String(serializedStr), 0, System.Convert.FromBase64String(serializedStr).GetLength(0));
                _ms.Position = 0;
                obj = formatter.Deserialize(_ms);
            }
            catch (Exception e) {
                throw new Exception(string.Format("SerializationHelper.Object_DeSerializeBinary:: Failed to deserialize object. Reason: {0}", e.Message), e);
            }
            finally {
                _ms.Close();
            }
            return obj;
        } // Object_DeSerializeBinary

        /// <summary>
        /// Deserializes the object using binary formatter.
        /// </summary>
        /// <param name="serializedArray">Byte array representation of the binary serialization of the object.</param>
        /// <returns>Deserialized object.</returns>
        public static object Object_DeSerializeBinary(byte[] serializedArray) {
            object obj = new object();
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            System.IO.MemoryStream _ms = new System.IO.MemoryStream(1024);
            try {
                _ms.Write(serializedArray, 0, serializedArray.Length);
                _ms.Position = 0;
                obj = formatter.Deserialize(_ms);
            }
            catch (Exception e) {
                throw new Exception(string.Format("SerializationHelper.Object_DeSerializeBinary:: Failed to deserialize object. Reason: {0}", e.Message), e);
            }
            finally {
                _ms.Close();
            }
            return obj;
        } // Object_DeSerializeBinary
        #endregion --- Serialization ---
    }
    #endregion ----- SerializationHelper -----
}

