using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;

namespace ScheduleHelper
{
    public static class StaticSQLiteHelper
    {
        private static string _connectionString;
        static StaticSQLiteHelper()
        {
            _connectionString = "Data Source=schedule.db;Pooling=true;";
        }
        public static DataTable ExecuteQuery(string sql)
        {
            DataTable dt = new DataTable();
            using (SQLiteConnection cn = new SQLiteConnection(_connectionString))
            {
                using (SQLiteDataAdapter da = new SQLiteDataAdapter(sql, cn))
                {
                    da.Fill(dt);
                }
            }
            return dt;
        }

        public static int BatchExecuteNonQuery(List<string> sqlList)
        {
            int rows=0;

            using (SQLiteConnection cn = new SQLiteConnection(_connectionString))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(cn))
                {
                    cn.Open();
                    SQLiteTransaction st= cn.BeginTransaction();
                    foreach (string s in sqlList)
                    {
                        cmd.CommandText = s;
                        rows += cmd.ExecuteNonQuery();
                    }
                    st.Commit();
                    cn.Close();
                }

            }
            return rows;
        
        }

        public static object ExecuteScalar(string sql)
        {
            object result;
            using (SQLiteConnection cn = new SQLiteConnection(_connectionString))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(sql,cn))
                {
                    cn.Open();
                    result = cmd.ExecuteScalar();
                }

            }
            return result;
        }

        public static int ExecuteNonQuery(string sql)
        {
            int rows;
            using (SQLiteConnection cn = new SQLiteConnection(_connectionString))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(sql, cn))
                {
                    cn.Open();
                    rows = cmd.ExecuteNonQuery();
                    cn.Close();
                }

            }
            return rows;
        }

        public static SQLiteDataReader ExecuteReader(string sql)
        {
            SQLiteDataReader reader;
            using (SQLiteConnection cn = new SQLiteConnection(_connectionString))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(sql, cn))
                {
                    cn.Open();
                    reader = cmd.ExecuteReader();
                }
            }
            return reader;
        }

        public static DataTable GetClassSubjectSetting(int classID)
        {
            DataTable dt = new DataTable();
            string sql = string.Format("select c.学科名字,t.教师编号,c.周课时,c.班主任,c.优先级 from teacher t inner join (select 学科名字,教师姓名,周课时,优先级,班主任,副科 from class_course where 班级={0} and 周课时!=0) as c using (教师姓名) order by c.优先级 desc", classID);
            dt = ExecuteQuery(sql);
            return dt;
        }
        public static DataTable GetTeacherInformation()
        {
            DataTable dt = new DataTable();
            string sql = "select 教师编号,教师姓名,学科名字,学科短名字 from teacher;";
            dt = ExecuteQuery(sql);
            return dt;
        }
        /// <summary>
        /// 返回安排了班级的教师列表,第一列为教师编号,第二列为教师姓名
        /// </summary>
        /// <param name="subjectName"></param>
        /// <returns></returns>
        public static DataTable GetSubjectTeacher(string subjectName)
        {
            DataTable t = new DataTable();
            string sql = string.Format("select distinct(t.教师编号),c.教师姓名 from class_course c inner join teacher t using(教师姓名) where c.学科名字='{0}'", subjectName);
            t = ExecuteQuery(sql);
            return t;
        }

        public static DataTable GetSubjectList()
        {
            DataTable dt = new DataTable();
            string sql = "select distinct(学科名字) from class_course;";
            dt = ExecuteQuery(sql);
            return dt;
        
        }


        public static void ClearAndVacuumTable(string tableName)
        {
            string sql = string.Format("delete from {0};", tableName);
            ExecuteNonQuery(sql);
            //sql = string.Format("vacuum full {0};", tableName);
            //ExecuteNonQuery(sql);
        }

        public static DataTable GetTeacherClass()
        {
            //DataTable dt = new DataTable();
            string sql = "select t.教师编号,c.班级 from teacher t inner join(select distinct(教师姓名),班级 from class_course) as c using(教师姓名)";
            DataTable dt = ExecuteQuery(sql);
            return dt;
        }

        public static DataTable GetGradeClassMaster()
        {
            string sql = "select cc.班级,t.教师编号 from class_course cc inner join teacher t on cc.教师姓名=t.教师姓名 where cc.班主任=1";
            DataTable dt = ExecuteQuery(sql);
            return dt;
        }

        public static int GetClassCount()
        {
            string sql = "select count(distinct(班级)) from class_course;";
            return Convert.ToInt32(ExecuteScalar(sql));
        }

        public static void Exchange2Lesson(int currentClassID, int firstDayOfWeek, int firstSection, int secondDayOfWeek, int secondSection)
        {
            int _firstTeacherID, _secondTeacherID;
            //查找第一个教师的ID号
            string sql = string.Format("select teacher_id from schedule where class_id={0} and day_of_week={1} and section={2};", currentClassID, firstDayOfWeek, firstSection);
            _firstTeacherID =Convert.ToInt32(ExecuteScalar(sql));
            //保存修改记录
            sql = string.Format("insert into history(time,sql) values('{0}','update schedule set teacher_id={1} where class_id={2} and day_of_week={3} and section={4};')",
                DateTime.Now.ToLongTimeString(),_firstTeacherID, currentClassID, firstDayOfWeek, firstSection);
            ExecuteNonQuery(sql);
            //查找第二位教师ID
            sql = string.Format("select teacher_id from schedule where class_id={0} and day_of_week={1} and section={2};", currentClassID, secondDayOfWeek, secondSection);
            _secondTeacherID = Convert.ToInt32(ExecuteScalar(sql));
            //保存修改记录
            sql = string.Format("insert into history(time,sql) values('{0}','update schedule set teacher_id={1} where class_id={2} and day_of_week={3} and section={4};')",
                DateTime.Now.ToLongTimeString(),_secondTeacherID, currentClassID, secondDayOfWeek, secondSection);
            ExecuteNonQuery(sql);
            //第一位教师的课换成第二位(将第一位教师的ID换成第二位教师的ID)
            sql = string.Format("update schedule set teacher_id={0} where class_id={1} and day_of_week={2} and section={3};", _secondTeacherID, currentClassID, firstDayOfWeek, firstSection);
            ExecuteNonQuery(sql);
            //第二位教师的课换成第一位教师
            sql = string.Format("update schedule set teacher_id={0} where class_id={1} and day_of_week={2} and section={3};", _firstTeacherID, currentClassID, secondDayOfWeek, secondSection);
            ExecuteNonQuery(sql);
        }
        public static void Exchange2NightLesson(int currentClassID, int firstDayOfWeek, int firstSection, int secondDayOfWeek, int secondSection)
        {
            int _firstTeacherID, _secondTeacherID;
            //查找第一个教师的ID号
            string sql = string.Format("select teacher_id from night where class_id={0} and day_of_week={1} and section={2};", currentClassID, firstDayOfWeek, firstSection);
            _firstTeacherID = Convert.ToInt32(ExecuteScalar(sql));
            //保存修改记录
            sql = string.Format("insert into history(time,sql) values('{0}','update night set teacher_id={1} where class_id={2} and day_of_week={3} and section={4};')",
                DateTime.Now.ToLongTimeString(), _firstTeacherID, currentClassID, firstDayOfWeek, firstSection);
            ExecuteNonQuery(sql);
            //查找第二位教师ID
            sql = string.Format("select teacher_id from night where class_id={0} and day_of_week={1} and section={2};", currentClassID, secondDayOfWeek, secondSection);
            _secondTeacherID = Convert.ToInt32(ExecuteScalar(sql));
            //保存修改记录
            sql = string.Format("insert into history(time,sql) values('{0}','update night set teacher_id={1} where class_id={2} and day_of_week={3} and section={4};')",
                DateTime.Now.ToLongTimeString(), _secondTeacherID, currentClassID, secondDayOfWeek, secondSection);
            ExecuteNonQuery(sql);
            //第一位教师的课换成第二位(将第一位教师的ID换成第二位教师的ID)
            sql = string.Format("update night set teacher_id={0} where class_id={1} and day_of_week={2} and section={3};", _secondTeacherID, currentClassID, firstDayOfWeek, firstSection);
            ExecuteNonQuery(sql);
            //第二位教师的课换成第一位教师
            sql = string.Format("update night set teacher_id={0} where class_id={1} and day_of_week={2} and section={3};", _firstTeacherID, currentClassID, secondDayOfWeek, secondSection);
            ExecuteNonQuery(sql);
        }
        public static void ReforceSetLesson(int classID,int dayOfWeek,int section,int teacherID)
        {
            //查找原来教师的ID
            string sql = string.Format("select teacher_id from schedule where class_id={0} and day_of_week={1} and section={2};", classID, dayOfWeek, section);
            int oldTeacherID = Convert.ToInt32(ExecuteScalar(sql));

            //修改教师ID
            sql = string.Format("update schedule set teacher_id={0} where class_id={1} and day_of_week={2} and section={3}", teacherID, classID, dayOfWeek, section);
            ExecuteNonQuery(sql);

            //插入修改记录
            sql = string.Format("insert into history(time,sql) values('{0}','update schedule set teacher_id={1} where class_id={2} and day_of_week={3} and section={4};')",
            DateTime.Now.ToLongTimeString(), oldTeacherID, classID, dayOfWeek, section);
            ExecuteNonQuery(sql);

        }

        public static void ReforceSetNightLesson(int classID, int dayOfWeek, int section, int teacherID)
        {
            //查找原来教师的ID
            string sql = string.Format("select teacher_id from night where class_id={0} and day_of_week={1} and section={2};", classID, dayOfWeek, section);
            int oldTeacherID = Convert.ToInt32(ExecuteScalar(sql));

            //修改教师ID
            sql = string.Format("update night set teacher_id={0} where class_id={1} and day_of_week={2} and section={3}", teacherID, classID, dayOfWeek, section);
            ExecuteNonQuery(sql);

            //插入修改记录
            sql = string.Format("insert into history(time,sql) values('{0}','update night set teacher_id={1} where class_id={2} and day_of_week={3} and section={4};')",
            DateTime.Now.ToLongTimeString(), oldTeacherID, classID, dayOfWeek, section);
            ExecuteNonQuery(sql);

        }


        //public static DataTable GetRequire()
        //{
        //    string sql = "select type,teacher_id,master,subject_name,class_id,day_of_week,section,predication,weight";

        //}

        public static DataTable GetClassSchedule(int classID)
        {
            DataTable dt = new DataTable();
            string sql = string.Format("select class_id,day_of_week,section,teacher_id,conflict from schedule where class_id={0} order by day_of_week,section; ", classID);
            dt = ExecuteQuery(sql);
            return dt;
        }


        public static DataTable GetSchedule()
        {
            DataTable dt = new DataTable();
            string sql = "select class_id,day_of_week,section,teacher_id from schedule;";
            dt = ExecuteQuery(sql);
            return dt;
        }
        public static DataTable GetNightSchedule()
        {
            DataTable dt = new DataTable();
            string sql = "select class_id,day_of_week,section,teacher_id from night;";
            dt = ExecuteQuery(sql);
            return dt;
        }


        public static DataTable GetInitialSchedule()
        {
            DataTable dt = new DataTable();
            string sql = "select class_id,day_of_week,section,teacher_id from initial_schedule;";
            dt = ExecuteQuery(sql);
            return dt;
        }





    }
}
