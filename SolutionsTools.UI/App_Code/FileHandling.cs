using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI.WebControls;

namespace SolutionsTools.UI
{
    public static class FileHandling
    {
        public static List<T> ReadFile<T>(FileUpload fileUpload, char separatorType) where T : class, new() 
        {
            try
            {
                if (fileUpload.HasFile)
                {
                    var myReader = new System.IO.StreamReader(fileUpload.PostedFile.InputStream, Encoding.GetEncoding("iso-8859-1"));

                    string output = myReader.ReadToEnd();

                    var content = ReadContentFile<T>(output, separatorType); 
                    
                    return content;
                }
                else
                {
                    throw new Exception("Selecione um arquivo para ser importado!");
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }
        private static List<T> ReadContentFile<T>(string content, char separatorType) where T : class, new()
        {
            var dt = new DataTable();

            string csvData = content;
            Boolean headerRowHasBeenSkipped = false;

            foreach (string row in csvData.Split('\n'))
            {
                if (!headerRowHasBeenSkipped)
                {
                    dt = CreateColumnsToDataTable(row, dt, separatorType);
                }
                else
                {
                    dt = AddRowsToDataTable(row, dt, separatorType);
                }

                headerRowHasBeenSkipped = true;
            }

            return DataTableToList<T>(dt);
        }
        private static DataTable CreateColumnsToDataTable(string row, DataTable dt, char separatorType)
        {
            foreach (string cell in row.Split(separatorType))
            {
                dt.Columns.Add(new DataColumn(Regex.Replace(cell, "[^0-9a-zA-Z_]+", ""), typeof(string)));
            }

            dt.Columns.Add(new DataColumn("SeqId", typeof(int)));

            return dt;
        }
        private static DataTable AddRowsToDataTable(string row, DataTable dt, char separatorType)
        {
            if (!string.IsNullOrEmpty(row))
            {
                DataRow _row = dt.NewRow();

                var cell = row.Split(separatorType);

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (cell.Length > i)
                    {
                        _row[dt.Columns[i].ColumnName] = cell[i].Replace("\"", "");
                    }
                    else
                    {
                        _row[dt.Columns[i].ColumnName] = (dt.Rows.Count + 1);
                    }
                }

                dt.Rows.Add(_row);
            }

            return dt;
        }
        private static List<T> DataTableToList<T>(this DataTable table) where T : class, new()
        {
            try
            {
                List<T> list = new List<T>();

                foreach (var row in table.AsEnumerable())
                {
                    T obj = new T();

                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        try
                        {
                            PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
                            propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    list.Add(obj);
                }

                return list;
            }
            catch
            {
                return null;
            }
        }

    }
}