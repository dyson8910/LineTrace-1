using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Timers;

namespace MainPCApp_ratest
{
    public partial class Form1 : Form
    {

        string connectionString;
        MongoClient client;
        MongoServer server;
        MongoDatabase db;
        MongoCollection<Records> collection;
        SerialPortProcessor serialPort1send = null;
        SerialPortProcessor serialPort1receive = null;
        SerialPortProcessor serialPort2send = null;
        SerialPortProcessor serialPort2receive = null;
        System.Timers.Timer timer = new System.Timers.Timer();
        BackgroundWorker bw = new BackgroundWorker();
        BackgroundWorker bw2 = new BackgroundWorker();
        int state1 = 0;
        int state2 = 0;
        Records res1 = new Records();
        Records res2 = new Records();
        String receiveddata = "";
        String receiveddata2 = "";
        
        
        public Form1()
        {
            InitializeComponent();
        }

        //起動時の処理
        private void Form1_Load(object sender, EventArgs e)
        {
            button2.Enabled = false;
            button3.Enabled = false;
            textBox3.Enabled = false;
            textBox2.Enabled = false;
            dataGridView1.Enabled = false;
            textBox1.ReadOnly = true;
            dataGridView1.AllowUserToAddRows = false;
            button6.Enabled = false;
            button9.Enabled = true;
            button5.Enabled = true;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            timer.Elapsed += new ElapsedEventHandler(TimerEvent);
            timer.Interval = 500;
            timer.Start();
            bw.DoWork += new DoWorkEventHandler(bw_Dowork);
            bw.WorkerSupportsCancellation = true;
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw2.DoWork += new DoWorkEventHandler(bw2_Dowork);
            bw2.WorkerSupportsCancellation = true;
            bw2.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw2_RunWorkerCompleted);
            Control.CheckForIllegalCrossThreadCalls = false;
        }
        //終了時の処理
        private void Form1_Closing(object sender, FormClosingEventArgs e)
        {
            if (serialPort1send != null)
            {
                serialPort1send.Close();
            }
            if (serialPort1receive != null)
            {
                serialPort1receive.Close();
            }

            if (serialPort2send != null)
            {
                serialPort2send.Close();
            }
            if (serialPort2receive != null)
            {
                serialPort2receive.Close();
            }
            timer.Stop();
        }


        //ボタンクリック処理

        //mongoDB関連
        //DB接続
        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text += String.Format("MongoDBに接続します\r\n");
            try
            {
                connectionString = "mongodb://localhost";
                client = new MongoClient(connectionString);
                server = client.GetServer();
                // DBの参照を取得(存在しなくても必要に応じて自動的に作成される)
                db = server.GetDatabase("Record");
                collection = db.GetCollection<Records>("records");
                textBox1.Text += String.Format("正常に接続が完了しました\r\n");
                textBox1.SelectionStart = textBox1.Text.Length;
                textBox1.ScrollToCaret();
                button1.Enabled = false;
                button2.Enabled = true;
                button3.Enabled = true;
                textBox2.Enabled = true;
                textBox3.Enabled = true;
                comboBox1.Enabled = true;

                dataGridView1.Enabled = true;
            }
            catch (TimeoutException)
            {
                textBox1.Text += String.Format("Error:タイムアウトしました\r\n");
                textBox1.SelectionStart = textBox1.Text.Length;
                textBox1.ScrollToCaret();
            }

        }
        //Query送信
        private void button2_Click(object sender, EventArgs e)
        {
            
            var doc = new Records
            {
                Username = textBox2.Text,
                Type = comboBox1.SelectedItem.ToString(),
                Param = Array.ConvertAll<string, int>(textBox3.Text.Split(','),delegate (string value) {
                        return int.Parse(value);
                }),
                State = "Waiting",
                Rec = new TimeSpan(1,0,0),
                Reserve = DateTime.Now
            };
            if ((doc.Type == "PID" && doc.Param.Length == 4) || (doc.Type == "ON-OFF" && doc.Param.Length == 15))
            {
                int j = 0;
                /*
                decimal i;
                int j = 0;
                foreach (var dat in doc.Param)
                {
                    bool canconv;
                    canconv = decimal.TryParse(dat, out i);
                    if (canconv == false)
                    {
                        j += 1;
                    }
                }
                */
                if (j == 0)
                {
                    collection.Insert<Records>(doc);
                    textBox1.Text += String.Format("データを追加しました:[{0},{1},[{2}],{3},{4},{5}]\r\n", doc.Username, doc.Type, string.Join(",", doc.Param), doc.State, doc.Rec, doc.Reserve);
                }
                else
                {
                    textBox1.Text += String.Format("Error: パラメータは数値で入力してください\r\n");
                }
            }
            else
            {
                textBox1.Text += String.Format("Error: パラメーター数が不適切(PIDは4つ,ON-OFFは5つ),もしくは適切なフィード(\",\")で区切られていません\r\n");
            }
            dataGridViewProcessor();
        }
        //一覧表示
        private void button3_Click_1(object sender, EventArgs e)
        {
            dataGridViewProcessor();
        }

        //Bluetooth関連
        //Dev1との接続
        private void button9_Click(object sender, EventArgs e)
        {
            button6.Enabled = true;
            button9.Enabled = false;
            var task1 = Task.Factory.StartNew(() =>
            {
                try
                {
                    serialPort1send = new SerialPortProcessor
                    {
                        PortName = "COM10",
                        BaudRate = 9600,
                        Parity = Parity.None,
                        DataBits = 8,
                        StopBits = StopBits.One
                    };
                    serialPort1send.DataReceived += DataReceivedCallback;
                    serialPort1send.Start();
                    Boxprint("シリアルポート(COM8)接続に成功しました");
                }
                catch (IOException er)
                {
                    if (er.Source != null)
                    {
                        Boxprint("シリアルポート(COM8)との接続に失敗しました");
                        Boxprint("IOException source:" + er.Source);
                    }
                }
            
                try
                {
                    serialPort1receive = new SerialPortProcessor
                    {
                        PortName = "COM9",
                        BaudRate = 9600,
                        Parity = Parity.None,
                        DataBits = 8,
                        StopBits = StopBits.One
                    };
                    serialPort1receive.DataReceived += DataReceivedCallback;
                    serialPort1receive.Start();
                    Boxprint("シリアルポート(COM9)の接続に成功しました");
                    
                }
                catch (IOException er)
                {
                    if (er.Source != null)
                    {
                        Boxprint("シリアルポート(COM9)との接続に失敗しました");
                        Boxprint("IOException source:" + er.Source);
                    }
                }
            });
        }
        //Dev1 本実行 バックグラウンドワーカーの起動
        private void button10_Click(object sender, EventArgs e)
        {
            //IsBusy - BackgroundWorker が非同期操作を実行中かどうかを示す値を取得します。
            if (!bw.IsBusy && serialPort1receive.IsOpen() == 1 && serialPort1send.IsOpen() == 1)
            {
                //バックグラウンド操作の実行を開始します。
                bw.RunWorkerAsync();
                Boxprint("Dev#1との通信を開始します。");
            }
            else if (serialPort1receive.IsOpen() == 0 || serialPort1send.IsOpen() == 0)
            {
                Boxprint("Dev#1のシリアルポートは開かれていません。");
            }

        }
        //Dev1 バックグラウンドワーカーの停止
        private void button11_Click(object sender, EventArgs e)
        {
            if (bw.IsBusy)
            {
                bw.CancelAsync();
                Boxprint("Device#1のバックグラウンドワーカーを停止しました。");
            }
            else
            {
                Boxprint("Error: Device#1のバックグラウンドワーカーはすでに停止しています。");
            }

        }
        //Dev1の切断
        private void button6_Click(object sender, EventArgs e)
        {

            if (serialPort1send != null)
            {
                serialPort1send.Close();
            }
            if (serialPort1receive != null)
            {
                serialPort1receive.Close();
            }
            button6.Enabled = false;
            button9.Enabled = true;
            button5.Enabled = true;
            Boxprint("Device#1との接続を切りました");

        }
        //Dev2との接続
        private void button8_Click(object sender, EventArgs e)
        {
            button7.Enabled = true;
            button8.Enabled = false;

            Task task2 = Task.Factory.StartNew(() =>
            {
                try
                {
                    serialPort2send = new SerialPortProcessor
                    {
                        PortName = "COM5",
                        BaudRate = 9600,
                        Parity = Parity.None,
                        DataBits = 8,
                        StopBits = StopBits.One
                    };
                    serialPort2send.DataReceived += DataReceivedCallback2;
                    serialPort2send.Start();
                    Boxprint("シリアルポート(COM5)の接続に成功しました");
                }
                catch (IOException er)
                {
                    if (er.Source != null)
                    {
                        Boxprint("シリアルポート(COM10)との接続に失敗しました");
                        Boxprint("IOException source:" + er.Source);
                    }
                }
                try
                {
                    serialPort2receive = new SerialPortProcessor
                    {
                        PortName = "COM6",
                        BaudRate = 9600,
                        Parity = Parity.None,
                        DataBits = 8,
                        StopBits = StopBits.One
                    };
                    serialPort2receive.DataReceived += DataReceivedCallback2;
                    serialPort2receive.Start();
                    Boxprint("シリアルポート(COM6)の接続に成功しました");
                }
                catch (IOException er)
                {
                    if (er.Source != null)
                    {
                        Boxprint("シリアルポート(COM11)との接続に失敗しました");
                        Boxprint("IOException source:" + er.Source);
                    }
                }
                
            });
        }

        //Dev2 本実行 バックグラウンドワーカーの起動
        private void button5_Click(object sender, EventArgs e)
        {
            if (serialPort2send == null)
            {
                Boxprint("send null");
            }
            else if (serialPort2receive == null)
            {
                Boxprint("receive null");
            }
            //IsBusy - BackgroundWorker が非同期操作を実行中かどうかを示す値を取得します。
            else if (!bw2.IsBusy && serialPort2receive.IsOpen() == 1 && serialPort2send.IsOpen() == 1)
            {
                //バックグラウンド操作の実行を開始します。
                bw2.RunWorkerAsync();
                Boxprint("Dev#2との通信を開始します。");
            }
            else if (serialPort2receive.IsOpen() == 0 || serialPort2send.IsOpen() == 0)
            {
                Boxprint("Dev#2のシリアルポートは開かれていません。");
            }
        }
        //Dev2 バックグラウンドワーカーの停止
        private void button12_Click(object sender, EventArgs e)
        {
            if (bw2.IsBusy)
            {
                bw2.CancelAsync();
                Boxprint("Device#2のバックグラウンドワーカーを停止しました。");
            }
            else
            {
                Boxprint("Error: Device#2のバックグラウンドワーカーはすでに停止しています。");
            }
            

        }
        //Dev2の切断
        private void button7_Click(object sender, EventArgs e)
        {

            if (serialPort2send != null)
            {
                serialPort2send.Close();
            }
            if (serialPort2receive != null)
            {
                serialPort2receive.Close();
            }
            
            button7.Enabled = false;
            button8.Enabled = true;
            button5.Enabled = true;
            
            Boxprint("Device#2との接続を切りました");

        }
        

        //内部処理関連
        //データグリッドビューのクリック応答(主に削除ボタン)
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            //"Button"列ならば、ボタンがクリックされた
            if (dgv.Columns[e.ColumnIndex].Name == "Delete")
            {
                textBox1.Text += String.Format("{0}を削除します\r\n", dgv.Rows[e.RowIndex].Cells[1].Value);
                collection.Remove(Query.EQ("_id", ObjectId.Parse(dgv.Rows[e.RowIndex].Cells[1].Value.ToString())));
                dataGridViewProcessor();
            }
            if (dgv.Columns[e.ColumnIndex].Name == "edisable")
            {
                textBox1.Text += String.Format("{0}の有効無効を変更します\r\n", dgv.Rows[e.RowIndex].Cells[1].Value);
                var q = (from t in collection.AsQueryable()
                        where t.Id == ObjectId.Parse(dgv.Rows[e.RowIndex].Cells[1].Value.ToString())
                        select t).First();
                if(q.State == "Success" || q.State == "Waiting")
                {
                    q.State = "Halt";
                }
                else if(q.State == "Halt")
                {
                    q.State = "Success";
                }
                collection.Save(q);
                dataGridViewProcessor();
            }

        }

        //データ受信コールバック
        public void DataReceivedCallback(byte[] data)
        {
            receiveddata += System.Text.Encoding.ASCII.GetString(data);
        }
        public void DataReceivedCallback2(byte[] data)
        {
            receiveddata2 += System.Text.Encoding.ASCII.GetString(data);
        }

        //データグリッドビューの更新（とりあえず全表示）
        public void dataGridViewUpdate(int v)
        {
            dataGridView1.Rows.Clear();
            TimeSpan offset = new TimeSpan(9, 0, 0);
            if (v == 0)
            {
                var q = from t in collection.AsQueryable()
                        select t;
                textBox1.Text += String.Format("一覧を表示します\r\n");
                foreach (var ent in q)
                {
                    dataGridView1.Rows.Add();
                    int idx = dataGridView1.Rows.Count - 1;
                    dataGridView1.Rows[idx].Cells[0].Value = ent.Reserve + offset;
                    dataGridView1.Rows[idx].Cells[1].Value = ent.Id;
                    dataGridView1.Rows[idx].Cells[2].Value = ent.Username;
                    dataGridView1.Rows[idx].Cells[3].Value = ent.Type;
                    dataGridView1.Rows[idx].Cells[4].Value = ent.State;
                    dataGridView1.Rows[idx].Cells[5].Value = ent.Rec;
                }
            }
            if (v == 1)
            {
                var q = from t in collection.AsQueryable()
                        where t.State == "Waiting"
                        select t;
                textBox1.Text += String.Format("一覧を表示します\r\n");
                foreach (var ent in q)
                {
                    dataGridView1.Rows.Add();
                    int idx = dataGridView1.Rows.Count - 1;
                    dataGridView1.Rows[idx].Cells[0].Value = ent.Reserve + offset;
                    dataGridView1.Rows[idx].Cells[1].Value = ent.Id;
                    dataGridView1.Rows[idx].Cells[2].Value = ent.Username;
                    dataGridView1.Rows[idx].Cells[3].Value = ent.Type;
                    dataGridView1.Rows[idx].Cells[4].Value = ent.State;
                    dataGridView1.Rows[idx].Cells[5].Value = ent.Rec;
                }
            }
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();

        }
        public void dataGridViewUpdateintime(String obj,int v)
        {
            dataGridView1.Rows.Clear();
            DateTime dt1 = DateTime.Parse(obj);
            TimeSpan ts = new TimeSpan(1, 0, 0);
            DateTime dt2 = dt1 + ts;
            TimeSpan ds = new TimeSpan(1,0, 0, 0);
            TimeSpan zero = new TimeSpan(0, 0, 0);
            TimeSpan offset = new TimeSpan(9, 0, 0);
            Boxprint(dt1.ToString());
            Boxprint(dt2.ToString());
            if (dt1.TimeOfDay == zero)
            {
                if (v == 0)
                {
                    var q = from t in collection.AsQueryable()
                            where t.Reserve >= dt1 & t.Reserve <= (dt1+ds)
                            select t;
                    Boxprint(dt1.ToString());
                    textBox1.Text += String.Format("一覧を表示します\r\n");
                    foreach (var ent in q)
                    {
                        dataGridView1.Rows.Add();
                        int idx = dataGridView1.Rows.Count - 1;
                        dataGridView1.Rows[idx].Cells[0].Value = ent.Reserve + offset;
                        dataGridView1.Rows[idx].Cells[1].Value = ent.Id;
                        dataGridView1.Rows[idx].Cells[2].Value = ent.Username;
                        dataGridView1.Rows[idx].Cells[3].Value = ent.Type;
                        dataGridView1.Rows[idx].Cells[4].Value = ent.State;
                        dataGridView1.Rows[idx].Cells[5].Value = ent.Rec;
                    }
                }
                if (v == 1)
                {
                    var q = from t in collection.AsQueryable()
                            where t.Reserve >= dt1 & t.Reserve <= (dt1 + ds) & t.State == "Waiting"
                            select t;
                    textBox1.Text += String.Format("一覧を表示します\r\n");
                    foreach (var ent in q)
                    {
                        dataGridView1.Rows.Add();
                        int idx = dataGridView1.Rows.Count - 1;
                        dataGridView1.Rows[idx].Cells[0].Value = ent.Reserve + offset;
                        dataGridView1.Rows[idx].Cells[1].Value = ent.Id;
                        dataGridView1.Rows[idx].Cells[2].Value = ent.Username;
                        dataGridView1.Rows[idx].Cells[3].Value = ent.Type;
                        dataGridView1.Rows[idx].Cells[4].Value = ent.State;
                        dataGridView1.Rows[idx].Cells[5].Value = ent.Rec;
                    }
                }

            }
            else {
                if (v == 0)
                {
                    var q = from t in collection.AsQueryable()
                            where t.Reserve >= dt1 & t.Reserve <= dt2
                            select t;
                    textBox1.Text += String.Format("一覧を表示します\r\n");
                    foreach (var ent in q)
                    {
                        dataGridView1.Rows.Add();
                        int idx = dataGridView1.Rows.Count - 1;
                        dataGridView1.Rows[idx].Cells[0].Value = ent.Reserve + offset;
                        dataGridView1.Rows[idx].Cells[1].Value = ent.Id;
                        dataGridView1.Rows[idx].Cells[2].Value = ent.Username;
                        dataGridView1.Rows[idx].Cells[3].Value = ent.Type;
                        dataGridView1.Rows[idx].Cells[4].Value = ent.State;
                        dataGridView1.Rows[idx].Cells[5].Value = ent.Rec;
                    }
                }
                if (v == 1)
                {
                    var q = from t in collection.AsQueryable()
                            where t.Reserve >= dt1 & t.Reserve <= dt2 & t.State == "Waiting"
                            select t;
                    textBox1.Text += String.Format("一覧を表示します\r\n");
                    foreach (var ent in q)
                    {
                        dataGridView1.Rows.Add();
                        int idx = dataGridView1.Rows.Count - 1;
                        dataGridView1.Rows[idx].Cells[0].Value = ent.Reserve + offset;
                        dataGridView1.Rows[idx].Cells[1].Value = ent.Id;
                        dataGridView1.Rows[idx].Cells[2].Value = ent.Username;
                        dataGridView1.Rows[idx].Cells[3].Value = ent.Type;
                        dataGridView1.Rows[idx].Cells[4].Value = ent.State;
                        dataGridView1.Rows[idx].Cells[5].Value = ent.Rec;
                    }
                }
            }
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();

        }
        public void dataGridViewProcessor()
        {
            var objtime = comboBox2.SelectedItem.ToString();
            var objstate = comboBox3.SelectedItem.ToString();
            if (objtime == "全て")
            {
                if (objstate == "全て")
                {
                    dataGridViewUpdate(0);
                }
                else if(objstate == "未実行のみ")
                {
                    dataGridViewUpdate(1);
                }
            }
            else
            {
                if (objstate == "全て")
                {
                    dataGridViewUpdateintime(objtime, 0);
                }
                else if(objstate == "未実行のみ")
                {
                    dataGridViewUpdateintime(objtime, 1);

                }
            }

        }

        //テキストボックスへの表示
        public void Boxprint(string text)
        {
            textBox1.Text += String.Format(text + "\r\n");
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();
        }
        
        //ポーリング処理
        public void TimerEvent(object sender, ElapsedEventArgs e)
        {
            try {
                if (serialPort1send.IsOpen() + serialPort1receive.IsOpen() == 2)
                {
                    label7.Text = "接続中";
                    button6.Enabled = true;
                    button9.Enabled = false;

                }
                else if (serialPort1send.IsOpen() + serialPort1receive.IsOpen() == 1)
                {
                    label7.Text = "異常接続";
                    button6.Enabled = true;
                    button9.Enabled = false;

                }
                else if (serialPort1send.IsOpen() + serialPort1receive.IsOpen() == 0)
                {
                    label7.Text = "未接続1";
                    button6.Enabled = false;
                    button9.Enabled = true;

                }
            }
            catch (NullReferenceException)
            {
                label7.Text = "未接続2";
                button6.Enabled = false;
                button9.Enabled = true;
            }
            try
            {
                if (serialPort2send.IsOpen() + serialPort2receive.IsOpen() == 2)
                {
                    label8.Text = "接続中";
                    button7.Enabled = true;
                    button8.Enabled = false;

                }
                else if (serialPort2send.IsOpen() + serialPort2receive.IsOpen() == 1)
                {
                    label8.Text = "異常接続";
                    button7.Enabled = true;
                    button8.Enabled = false;

                }
                else if (serialPort2send.IsOpen() + serialPort2receive.IsOpen() == 0)
                {
                    label8.Text = "未接続1";
                    button7.Enabled = false;
                    button8.Enabled = true;
                }
            }
            catch (NullReferenceException)
            {
                label8.Text = "未接続2";
                button7.Enabled = false;
                button8.Enabled = true;

            }
            if (state1 == 0)
            {
                label2.Text = "待機中。";
                if (res1.State == "Success")
                {
                    label2.Text += " 前回の記録:" + res1.Username + "さん " + res1.Rec.ToString("c");
                }
                else if(res1.State == "Halt")
                {
                    label2.Text += " 前回の記録:" + res1.Username + "さん 失敗";
                }
            }
            else if(state1 == 1)
            {
                label2.Text = "データ送信中。スタックする場合はリセットしてください。";
            }
            else if (state1 == 2)
            {
                label2.Text = "データ返信待ち。スタックする場合はリセットしてください。";
            }
            else if (state1 == 3)
            {
                label2.Text = "実行中。";
                if (res1.State == "Executingby#1")
                {
                    label2.Text += " 現在実行中:" + res1.Username + "さん " + (res1.Reserve + new TimeSpan(9, 0, 0));
                }
            }
            if (state2 == 0)
            {
                label9.Text = "待機中。";
                if (res2.State == "Success")
                {
                    label9.Text += " 前回の記録:" + res2.Username + "さん " + res2.Rec.ToString("c");
                }
                else if (res2.State == "Halt")
                {
                    label9.Text += " 前回の記録:" + res2.Username + "さん 失敗";
                }
            }
            else if (state2 == 1)
            {
                label9.Text = "データ送信中。スタックする場合はリセットしてください。";
            }
            else if (state2 == 2)
            {
                label9.Text = "データ返信待ち。スタックする場合はリセットしてください。";
            }
            else if (state2 == 3)
            {
                label9.Text = "実行中。";
                if (res2.State == "Executingby#2")
                {
                    label9.Text += " 現在実行中:" + res2.Username + "さん " + (res2.Reserve+new TimeSpan(9,0,0));
                }
            }
        }

        //bwのタスク
        void bw_Dowork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            String text = "";
            String buf = "";
            state1 = 0;
            //cancelかbreakがかからない限りジョブは継続されます
            while (!e.Cancel)
            {
                //state1 0 => 準備完了待ち
                if (state1 == 0)
                {
                    Boxprint("state1 == 0");
                    while (true)
                    {
                        Thread.Sleep(2000);
                        if (bw.CancellationPending)
                        {
                            e.Cancel = true;
                            break;
                        }
                        if (receiveddata != "")
                        {
                            Thread.Sleep(1000);
                            buf = receiveddata;
                            receiveddata = "";
                            Boxprint(buf);
                            if (buf == "Ready")
                            {
                                Boxprint("Ready");
                                state1 = 1;
                                break;
                            }
                            else
                            {
                                Boxprint("無効なメッセージが受信されました。接続を再確認して再度スイッチを入れてください。");
                                break;
                            }
                        }
                    }
                }
                // state 1 => DBとの情報のやり取り、送信
                if (state1 == 1)
                {
                    Boxprint("state1 == 1");
                    try
                    {
                        Boxprint("dev_try1");
                        var q = (from t in collection.AsQueryable()
                                 where t.State == "Waiting"
                                 orderby t.Reserve
                                 select t).Take(1).Count();
                        if (q < 1)
                        {
                            Boxprint("Error(at #1): 待機中のデータがありません。バックグラウンド作業を終了します。");
                            break;
                        }
                    }
                    catch (TimeoutException)
                    {
                        Boxprint("Error(at #1): MongoDBへの要求がタイムアウトしました。サーバとの接続を確認してください");
                        break;
                    }

                    try
                    {
                        Boxprint("dev_try1-2");
                        var q = (from t in collection.AsQueryable()
                                 where t.State == "Waiting"
                                 orderby t.Reserve
                                 select t).First();
                        text = q.Type + " " + String.Join(" ", q.Param);
                        byte[] buffer = System.Text.Encoding.ASCII.GetBytes(text);
                        Boxprint("before send");
                        serialPort1send.WriteData(buffer);
                        Boxprint("(at #1): バッファにデータを書き込みました " + text);
                        q.State = "Executingby#1";
                        collection.Save(q);
                        res1.State = q.State;
                        res1.Username = q.Username;
                        res1.Reserve = q.Reserve;
                        res1.Rec = q.Rec;
                        dataGridViewProcessor();
                    }
                    catch (InvalidOperationException er)
                    {
                        if (er.Source != null)
                        {
                            Boxprint("Error(at #1): 送信エラー:送信ポートは閉じられています");
                            Boxprint("IOException source:" + er.Source);
                            break;
                        }

                    }
                    state1 = 2;

                }
                // state 2 => 受信成功待ち、タイムアウトか不明な信号が来た場合は終了
                if (state1 == 2)
                {
                    Boxprint("dev1_state1 == 2");

                    while (true)
                    {
                        Thread.Sleep(2000);

                        if (bw.CancellationPending)
                        {
                            e.Cancel = true;
                            Boxprint("Device#1 : バックグラウンドワーカーを停止しました。");
                            var q = (from t in collection.AsQueryable()
                                     where t.State == "Executingby#1"
                                     orderby t.Reserve
                                     select t).First();
                            q.State = "Waiting";
                            collection.Save(q);
                            state1 = 0;
                            dataGridViewProcessor();
                            break;
                        }
                        if (receiveddata != "")
                        {
                            Thread.Sleep(1000);
                            buf = receiveddata;
                            receiveddata = "";
                            Boxprint(buf);
                            if (buf == "Success")
                            {
                                state1 = 3;
                                break;
                            }
                            else
                            {
                                Boxprint("Error(at #1): 無効なメッセージが受信されました。接続を再確認して再度スイッチを入れてください。");
                                var q = (from t in collection.AsQueryable()
                                         where t.State == "Executingby#1"
                                         orderby t.Reserve
                                         select t).First();
                                q.State = "Waiting";
                                collection.Save(q);
                                state1 = 0;
                                res1.Reserve = q.Reserve;
                                res1.Username = q.Username;
                                res1.State = q.State;
                                res1.Rec = q.Rec;
                                dataGridViewProcessor();
                                break;
                            }
                        }
                    }
                }
                if (state1 == 3)
                {
                    while (true)
                    {
                        Thread.Sleep(2000);

                        if (bw.CancellationPending)
                        {
                            e.Cancel = true;
                            state1 = 0;
                            break;
                        }
                        if (receiveddata != "")
                        {
                            TimeSpan ts = new TimeSpan();
                            Thread.Sleep(1000);
                            buf = receiveddata;
                            receiveddata = "";
                            Boxprint("Dev1:" + res1.Username + "さんの記録:" + buf);
                            try
                            {
                                ts = TimeSpan.Parse(buf);
                            }

                            catch (FormatException)
                            {
                                if (buf == "Halt")
                                {
                                    Boxprint("(at #1): 停止しました。");
                                    var p = (from t in collection.AsQueryable()
                                             where t.State == "Executingby#1"
                                             orderby t.Reserve
                                             select t).First();
                                    p.State = "Halt";
                                    collection.Save(p);
                                    state1 = 0;
                                    res1.State = p.State;
                                    res1.Username = p.Username;
                                    res1.Reserve = p.Reserve;
                                    res1.Rec = p.Rec;
                                    dataGridViewProcessor();
                                    break;

                                }
                                else {
                                    Boxprint("Error(at #1): 無効な形式のリザルトが送られてきました。確認が可能ならば手動で修正をした上でバックグラウンドワーカーを立ち上げ直してください。");
                                    e.Cancel = true;
                                    state1 = 0;
                                    break;
                                }
                            }
                            var q = (from t in collection.AsQueryable()
                                     where t.State == "Executingby#1"
                                     orderby t.Reserve
                                     select t).First();
                            q.State = "Success";
                            q.Rec = ts;
                            collection.Save(q);
                            state1 = 0;
                            res1.State = q.State;
                            res1.Username = q.Username;
                            res1.Rec = q.Rec;
                            res1.Reserve = q.Reserve;

                            dataGridViewProcessor();
                            break;
                        }
                    }

                }
                if (bw.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }

            }
        }
        void bw2_Dowork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw2 = sender as BackgroundWorker;
            String text = "";
            String buf = "";
            state2 = 0;
            //cancelかbreakがかからない限りジョブは継続されます
            while (!e.Cancel)
            {
                //state 0 => 準備完了待ち
                if (state2 == 0)
                {
                    Boxprint("state2 == 0");
                    while (true)
                    {
                        Thread.Sleep(2000);

                        if (bw2.CancellationPending)
                        {
                            e.Cancel = true;
                            Boxprint("Device#2 : バックグラウンドワーカーを停止しました。");
                            break;
                        }
                        if (receiveddata2 != "")
                        {
                            Thread.Sleep(1000);
                            buf = receiveddata2;
                            receiveddata2 = "";
                            Boxprint(buf);
                            if (buf == "Ready")
                            {
                                state2 = 1;
                                break;
                            }
                            else
                            {
                                Boxprint("Error(at #2): 無効なメッセージが受信されました。接続を再確認して再度スイッチを入れてください。");
                                break;
                            }
                        }
                    }
                }
                // state 1 => DBとの情報のやり取り、送信
                if (state2 == 1)
                {
                    Boxprint("state2 == 1");
                    try
                    {
                        Boxprint("dev2_try1-1");
                        var q = (from t in collection.AsQueryable()
                                 where t.State == "Waiting"
                                 orderby t.Reserve
                                 select t).Take(1).Count();
                        if (q < 1)
                        {
                            Boxprint("Error(at #2): 待機中のデータがありません。バックグラウンド作業を終了します。");
                            state2 = 0;
                            break;
                        }
                    }
                    catch (TimeoutException)
                    {
                        Boxprint("Error(at #2): MongoDBへの要求がタイムアウトしました。サーバとの接続を確認してください");
                        state2 = 0;
                        break;
                    }

                    try
                    {
                        Boxprint("dev2_try1-2");
                        var q = (from t in collection.AsQueryable()
                                 where t.State == "Waiting"
                                 orderby t.Reserve
                                 select t).First();
                        text = q.Type + " " + String.Join(" ", q.Param);
                        byte[] buffer = System.Text.Encoding.ASCII.GetBytes(text);
                        serialPort2send.WriteData(buffer);
                        Boxprint("(at #2): バッファにデータを書き込みました " + text);
                        q.State = "Executingby#2";
                        collection.Save(q);
                        res2.State = q.State;
                        res2.Username = q.Username;
                        res2.Rec = q.Rec;
                        res2.Reserve = q.Reserve;
                        dataGridViewProcessor();
                    }
                    catch (InvalidOperationException er)
                    {
                        if (er.Source != null)
                        {
                            Boxprint("Error(at #2): 送信エラー:送信ポートは閉じられています");
                            Boxprint("IOException source:" + er.Source);
                            break;
                        }
                    }
                    state2 = 2;
                }
                // state 2 => 受信成功待ち、タイムアウトか不明な信号が来た場合は終了
                if (state2 == 2)
                {
                    Boxprint("state2 == 2");
                    while (true)
                    {
                        Thread.Sleep(2000);

                        if (bw.CancellationPending)
                        {
                            e.Cancel = true;
                            Boxprint("Device#2 : バックグラウンドワーカーを停止しました。");
                            var q = (from t in collection.AsQueryable()
                                     where t.State == "Executingby#2"
                                     orderby t.Reserve
                                     select t).First();
                            q.State = "Waiting";
                            collection.Save(q);
                            state2 = 0;
                            dataGridViewProcessor();
                            break;
                        }
                        if (receiveddata2 != "")
                        {
                            Thread.Sleep(1000);
                            buf = receiveddata2;
                            receiveddata2 = "";
                            Boxprint(buf);
                            if (buf == "Success")
                            {
                                state2 = 3;
                                break;
                            }
                            else
                            {
                                Boxprint("Error(at #2): 無効なメッセージが受信されました。接続を再確認して再度スイッチを入れてください。");
                                var q = (from t in collection.AsQueryable()
                                         where t.State == "Executingby#2"
                                         orderby t.Reserve
                                         select t).First();
                                q.State = "Waiting";
                                collection.Save(q);
                                dataGridViewProcessor();
                                state2 = 0;
                                break;
                            }
                        }
                    }
                }
                if (state2 == 3)
                {
                    while (true)
                    {
                        Thread.Sleep(2000);

                        if (bw.CancellationPending)
                        {
                            e.Cancel = true;
                            Boxprint("Device#2 : バックグラウンドワーカーを停止しました。");
                            var q = (from t in collection.AsQueryable()
                                     where t.State == "Executingby#2"
                                     orderby t.Reserve
                                     select t).First();
                            q.State = "Waiting";
                            collection.Save(q);
                            state2 = 0;
                            res2.State = q.State;
                            res2.Username = q.Username;
                            res2.Rec = q.Rec;
                            res2.Reserve = q.Reserve;
                            Boxprint("Dev2:" + res2.Username + "さんの記録:" + buf);
                            dataGridViewProcessor();
                            break;
                        }
                        if (receiveddata2 != "")
                        {
                            TimeSpan ts = new TimeSpan();
                            Thread.Sleep(1000);
                            buf = receiveddata2;
                            receiveddata2 = "";
                            Boxprint(buf);
                            try
                            {
                                ts = TimeSpan.Parse(buf);
                            }

                            catch (FormatException)
                            {
                                if (buf == "Halt")
                                {
                                    Boxprint("(at #2): 停止しました。");
                                    var p = (from t in collection.AsQueryable()
                                             where t.State == "Executingby#2"
                                             orderby t.Reserve
                                             select t).First();
                                    p.State = "Halt";
                                    collection.Save(p);
                                    state2 = 0;
                                    res2 = p;
                                    res2.State = p.State;
                                    res2.Username = p.Username;
                                    res2.Rec = p.Rec;
                                    res2.Reserve = p.Reserve;
                                    dataGridViewProcessor();
                                    break;

                                }
                                else {
                                    Boxprint("Error(at #2): 無効な形式のリザルトが送られてきました。確認が可能ならば手動で修正をした上でバックグラウンドワーカーを立ち上げ直してください。");
                                    e.Cancel = true;
                                    state2 = 0;
                                    break;
                                }
                            }
                            var q = (from t in collection.AsQueryable()
                                     where t.State == "Executingby#2"
                                     orderby t.Reserve
                                     select t).First();
                            q.State = "Success";
                            q.Rec = ts;
                            collection.Save(q);
                            state2 = 0;
                            res2.State = q.State;
                            res2.Username = q.Username;
                            res2.Rec = q.Rec;
                            res2.Reserve = q.Reserve;
                            dataGridViewProcessor();
                            break;
                        }
                    }

                }
                if (bw.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }

            }
        }

        //bw完了時の処理
        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string msg = string.Empty;

            if (e.Cancelled)
            {
                msg = "Cancelled1";
            }
            else
            {
                msg = "Done1";
            }

            Boxprint(msg);

         }
        void bw2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string msg = string.Empty;

            if (e.Cancelled)
            {
                msg = "Cancelled2";
            }
            else
            {
                msg = "Done2";
            }

            Boxprint(msg);

        }

    }

    public class SerialPortProcessor
    {
        private SerialPort myPort = null;
        private Thread receiveThread = null;

        public String PortName { get; set; }
        public int BaudRate { get; set; }
        public Parity Parity { get; set; }
        public int DataBits { get; set; }
        public StopBits StopBits { get; set; }

        public SerialPortProcessor()
        {
        }

        public void Start()
        {
            myPort = new SerialPort(
                 PortName, BaudRate, Parity, DataBits, StopBits);
            myPort.Open();
            receiveThread = new Thread(SerialPortProcessor.ReceiveWork);
            receiveThread.Start(this);
        }

        public static void ReceiveWork(object target)
        {
            SerialPortProcessor my = target as SerialPortProcessor;
            my.ReceiveData();
        }

        public void WriteData(byte[] buffer)
        {
            myPort.Write(buffer, 0, buffer.Length);
        }

        public delegate void DataReceivedHandler(byte[] data);
        public event DataReceivedHandler DataReceived;

        public void ReceiveData()
        {
            if (myPort == null)
            {
                return;
            }
            do
            {
                try
                {
                    int rbyte = myPort.BytesToRead;
                    byte[] buffer = new byte[rbyte];
                    int read = 0;
                    while (read < rbyte)
                    {
                        int length = myPort.Read(buffer, read, rbyte - read);
                        read += length;
                    }
                    if (rbyte > 0)
                    {
                        DataReceived(buffer);
                    }
                }
                catch (IOException ex)
                {
                    MessageBox.Show("Error: COMポートを確認してください");
            }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show("Error: COMポートを確認してください");
                }
            } while (myPort.IsOpen);
        }

        public void Close()
        {
            if (receiveThread != null && myPort != null)
            {
                myPort.Close();
                receiveThread.Join();
            }
        }
        public int IsOpen()
        {
            if(myPort == null)
            {
                return 0;
            }
            if (myPort.IsOpen)
            {
                return 1;
            }
            else
                return 0;

        }
    }
    public class Records
    {
        public ObjectId Id { get; set; }
        public string Username { get; set; }
        public string Type { get; set; }
        public string State { get; set; }
        public int[] Param { get; set; }
        public TimeSpan Rec { get; set; }
        public DateTime Reserve { get; set; }


    }
}
