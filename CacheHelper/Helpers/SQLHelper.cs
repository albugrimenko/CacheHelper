using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace CacheHelper.Helpers {
    #region ----- SQLHelper -----
    public static class SQLHelper {

        public enum CallType { 
            Put = 0,
            Get
        }

        static string _ConString_ = "";
        static bool _IsCacheSQLAllowed_ = false;

        static SqlCommand[] _cmd_ = new SqlCommand[2];

        #region --- Constructors ---
        static SQLHelper() {
            _ConString_ = StaticHelper.ConString("CacheSqlServer");
            _IsCacheSQLAllowed_ = StaticHelper.GetConfigAttrAsBool("CacheSql.IsAllowed", _IsCacheSQLAllowed_);

            if (_IsCacheSQLAllowed_ && !string.IsNullOrWhiteSpace(_ConString_)) {
                // initiate commands
                int ttlMin = StaticHelper.GetConfigAttrAsInt("CacheSql.TTLMin", 20);

                lock (_cmd_) {
                    // Put (0)
                    SqlCommand cmd = new SqlCommand("Object_Put");
                    cmd.CommandTimeout = Helpers.StaticHelper._SQLCommandTimeout_; 
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@ObjType", SqlDbType.VarChar);
                    cmd.Parameters.Add("@ObjKey", SqlDbType.VarChar);
                    cmd.Parameters.Add("@TTL_min", SqlDbType.Int);
                    cmd.Parameters["@TTL_min"].Value = ttlMin > 0 ? ttlMin : 20;
                    cmd.Parameters.Add("@ObjBody", SqlDbType.VarBinary);
                    _cmd_[(int)CallType.Put] = cmd;

                    // Get (1)
                    cmd = new SqlCommand("Object_Get");
                    cmd.CommandTimeout = Helpers.StaticHelper._SQLCommandTimeout_; 
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@ObjType", SqlDbType.VarChar);
                    cmd.Parameters.Add("@ObjKey", SqlDbType.VarChar);
                    _cmd_[(int)CallType.Get] = cmd;
                }
            }
        }
        #endregion --- Constructors ---

        public static void ObjectPut(string objType, string objKey, object objValue) {
            // do nothing if not allowed or not configured.
            if (_cmd_[0] == null)
                return;

            SqlConnection connection = null;
            try {
                connection = new SqlConnection(_ConString_);
                connection.Open();

                SqlCommand cmd = _cmd_[(int)CallType.Put].Clone();
                //lock (_cmd_) {
                //    cmd = _cmd_[0].Clone();
                //}
                if (cmd == null)
                    throw new Exception("cannot instantiate cmd.");

                cmd.Parameters["@ObjType"].Value = objType;
                cmd.Parameters["@ObjKey"].Value = objKey;
                cmd.Parameters["@ObjBody"].Value = SerializationHelper.Object_SerializeBinaryArray(objValue);

                cmd.Connection = connection;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) {
                StaticHelper.log.Error("SQLHelper.ObjectPut:: " + ex.Message, ex);
            }
            finally {
                if (connection != null && connection.State != ConnectionState.Broken && connection.State != ConnectionState.Closed) {
                    connection.Close();
                    connection.Dispose();
                }
            }
        } // ObjectPut

        public static object ObjectGet(string objType, string objKey) {
            // do nothing if not allowed or not configured.
            if (_cmd_[1] == null)
                return null;

            object res = null;
            SqlConnection connection = null;
            try {
                connection = new SqlConnection(_ConString_);
                connection.Open();
                SqlCommand cmd = _cmd_[(int)CallType.Get].Clone();
                //lock (_cmd_) {
                //    cmd = _cmd_[1].Clone();
                //}
                if (cmd == null)
                    throw new Exception("cannot instantiate cmd.");

                cmd.Parameters["@ObjType"].Value = objType;
                cmd.Parameters["@ObjKey"].Value = objKey;

                cmd.Connection = connection;
                SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                // returns either nothing or
                //  ExpDate, ObjBody
                if (dr != null && dr.HasRows && dr.Read()) {
                    //DateTime expdate = Convert.ToDateTime(dr["ExpDate"].ToString()); // not currently used
                    byte[] objSer = (byte[]) dr["ObjBody"];
                    res = SerializationHelper.Object_DeSerializeBinary(objSer);
                }

            }
            catch (Exception ex) {
                StaticHelper.log.Error("SQLHelper.ObjectGet:: " + ex.Message, ex);
            }
            finally {
                if (connection != null && connection.State != ConnectionState.Broken && connection.State != ConnectionState.Closed) {
                    connection.Close();
                    connection.Dispose();
                }
            }
            return res;
        } // ObjectGet
    }
    #endregion ----- SQLHelper -----
}
