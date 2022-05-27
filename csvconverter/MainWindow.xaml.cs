using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace csvconverter
{
    public partial class MainWindow : Window
    {
        private string[] header;
        private string[,] body;
        private bool[] _IsNull;
        private string[] tipusok;
        private List<string> tablenames = new List<string>();
        private List<string> columnnames = new List<string>();
        public MainWindow()
        {
            InitializeComponent();
        }
        public void fileload(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();
            openFileDlg.Filter = "csv files (*.csv)|*.csv|txt files (*.txt)|*.txt";
            Nullable<bool> result = openFileDlg.ShowDialog();
            if (result == true)
            {
                filesearch.Content = openFileDlg.SafeFileName;
                string[] csvcontent = System.IO.File.ReadAllLines(openFileDlg.FileName, Encoding.GetEncoding("iso-8859-1"));
                header = csvcontent[0].Split(';');
                _IsNull = new bool[header.Length];
                for (int i = 0; i < header.Length; i++)
                { _IsNull[i] = false; }
                tipusok = new string[header.Length];
                for (int i = 0; i < header.Length; i++)
                { tipusok[i] = "string"; }
                body = new string[csvcontent.Length, header.Length];
                for (int i = 1; i < csvcontent.Length; i++)
                {
                    string[] row = csvcontent[i].Split(';');
                    for (int j = 0; j < row.Length; j++)
                    {
                        body[i - 1, j] = row[j];
                    }
                }
                for (int j = 0; j < header.Length; j++)
                {
                    bool IsString = false;
                    for (int i = 0; i < csvcontent.Length - 1; i++)
                    {
                        if (body[i, j] == "")
                        {
                            _IsNull[j] = true;
                        }
                        else
                        {
                            try
                            {
                                Convert.ToInt32(body[i, j]);
                                tipusok[j] = "int";
                            }
                            catch
                            {
                                if (body[i, j] == "i" || body[i, j] == "h" || body[i, j] == "true" || body[i, j] == "false")
                                {
                                    tipusok[j] = "bool";
                                }
                                else
                                {
                                    tipusok[j] = "string";
                                    IsString = true;
                                }
                            }
                        }
                    }
                    if (IsString)
                    {
                        tipusok[j] = "string";
                    }
                }
                DataTable dt = new DataTable();
                for (int i = 0; i < header.Length; i++)
                {
                    DataColumn column = new DataColumn();
                    column.ColumnName = header[i];
                    switch (tipusok[i])
                    {
                        case "int": column.DataType = typeof(int); break;
                        case "string": column.DataType = typeof(string); break;
                        case "bool": column.DataType = typeof(bool); break;
                    }
                    dt.Columns.Add(column);
                }
                for (int i = 0; i < csvcontent.Length - 1; i++)
                {
                    DataRow row = dt.NewRow();
                    for (int j = 0; j < header.Length; j++)
                    {
                        switch (tipusok[j])
                        {
                            case "int": if (body[i, j] == "") { row[header[j]] = 0; } else { row[header[j]] = Convert.ToInt32(body[i, j]); }; break;
                            case "string": row[header[j]] = body[i, j]; break;
                            case "bool":
                                if (body[i, j] == "i" || body[i, j] == "true")
                                {
                                    row[header[j]] = true;
                                }
                                else
                                {
                                    row[header[j]] = false;
                                }
                                break;
                        }
                    }
                    dt.Rows.Add(row);
                }
                data.ItemsSource = dt.DefaultView;
            }
        }
        private void cdelete(object sender, EventArgs e)
        {
            if (data.Columns.Count > 0 && data.SelectedCells != null && data.CurrentCell.Column.DisplayIndex >= 0 && data.CurrentCell.Column.DisplayIndex < data.Columns.Count)
            {
                data.Columns.Remove(data.Columns[data.CurrentCell.Column.DisplayIndex]);

            }

        }
        private void rdelete(object sender, EventArgs e)
        {
            if (data.Items.Count > 0 && data.SelectedCells != null && data.SelectedIndex >= 0 && data.SelectedIndex < data.Items.Count)
            {



            }
        }
        private void Push(object sender, EventArgs e)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            if (File.Exists("converter.txt"))
            {
                try
                {
                    using (StreamReader sr = new StreamReader("converter.txt"))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            dict.Add(line.Split(';')[0], line.Split(';')[1]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            MySqlConnection con = new MySqlConnection(ConnectionString.Text);
            MySqlCommand cmdbe = con.CreateCommand();
            cmdbe.CommandText = "select TABLE_NAME,Column_name from information_schema.columns where table_schema = '" + ConnectionString.Text.Split(';')[3].Split('=')[1] + "' order by table_name,ordinal_position";
            try
            {
                con.Open();
                MySqlDataReader rdr = cmdbe.ExecuteReader();
                while (rdr.Read())
                {
                    tablenames.Add(rdr.GetString(0));
                    columnnames.Add(rdr.GetString(1));
                }
                con.Close();
            }
            catch (Exception ex)
            {
                con.Close();
                MessageBox.Show(ex.Message);
            }
            MySqlCommand cmd = con.CreateCommand();
            DataView dv = data.ItemsSource as DataView;
            DataTable tb = dv.Table;
            foreach (DataColumn col in tb.Columns)
            {
                if (dict.ContainsKey(col.ColumnName))
                {
                    col.ColumnName = dict[col.ColumnName];
                }
            }
            List<string> tablecolumn = new List<string>();
            for (int j = 0; j < tb.Columns.Count; j++)
            {
                bool _isexist = false;
                for (int i = 0; i < tablenames.Count(); i++)
                {
                    if (tb.Columns[j].ColumnName == columnnames[i] && _isexist == false)
                    {
                        tablecolumn.Add(tablenames[i]);
                        _isexist = true;
                    }
                }
                if (_isexist == false)
                {
                    tb.Columns.Remove(tb.Columns[j]);
                    j--;
                }
            }
            int startindex = 0;
            int endindex = 0;
            while (endindex < tb.Columns.Count + 1)
            {
                if (endindex < tb.Columns.Count && tablecolumn[startindex] == tablecolumn[endindex])
                {
                    endindex++;
                }
                else
                {
                    for (int j = 0; j < tb.Rows.Count; j++)
                    {
                        string parancs = "INSERT INTO " + tablecolumn[startindex] + "(";
                        for (int i = startindex; i < endindex; i++)
                        {
                            if (i < endindex - 1)
                            {
                                parancs += tb.Columns[i].ColumnName + ",";
                            }
                            else
                            {
                                parancs += tb.Columns[i].ColumnName + ") VALUES (";
                            }
                        }
                        for (int i = startindex; i < endindex; i++)
                        {
                            if (i < endindex - 1)
                            {
                                if (tb.Rows[j].ItemArray[i] == null || tb.Rows[j].ItemArray[i] == "")
                                {
                                    parancs += "null,";
                                }
                                else
                                {
                                    if (tb.Columns[i].DataType == typeof(string))
                                    {
                                        parancs += "'" + tb.Rows[j].ItemArray[i].ToString().Replace("'", "") + "',";
                                    }
                                    else
                                    {
                                        parancs += tb.Rows[j].ItemArray[i].ToString().Replace("'", "") + ",";
                                    }
                                }
                            }
                            else
                            {
                                if (tb.Rows[j].ItemArray[i] == null || tb.Rows[j].ItemArray[i] == "")
                                {
                                    parancs += "null);";
                                }
                                else
                                {
                                    if (tb.Columns[i].DataType == typeof(string))
                                    {
                                        parancs += "'" + tb.Rows[j].ItemArray[i].ToString().Replace("'", "") + "');";
                                    }
                                    else
                                    {
                                        parancs += tb.Rows[j].ItemArray[i].ToString().Replace("'", "") + ");";
                                    }
                                }
                            }
                        }
                        cmd.CommandText = parancs;
                        try
                        {
                            con.Open();
                            cmd.ExecuteNonQuery();
                            con.Close();
                        }
                        catch (Exception ex)
                        {
                            con.Close();
                            MessageBox.Show(ex.Message);
                        }
                    }
                    startindex = endindex;
                    endindex++;
                }
            }
            MessageBox.Show("Upload Completed");
        }
    }
}