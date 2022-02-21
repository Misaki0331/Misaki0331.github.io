public partial class MainForm : Form
    {        
        GetFile nt;//これがスレ1の実装文
        string StatusMessage;//ステータスメッセージ
        string CurrentURL;//現在ダウンロード中のURL
        bool[] IsRunningThread;//スレッド状態管理
        bool IsFinished; //終了したかどうか
        public MainForm(string[] args)
        {
            try
            {
                InitializeComponent();
                IsRunningThread = new bool[100];
                nt = new GetFile();
                AllThreadReady();
            }
        }    
        void ThreadRun00() { ThreadRun(0); }
        void ThreadRun01() { ThreadRun(1); }
        void ThreadRun02() { ThreadRun(2); }
        void AllThreadReady()
        {
            Thread t;
            t = new Thread(new ThreadStart(ThreadRun00));t.Start();
            t = new Thread(new ThreadStart(ThreadRun01));t.Start();
            t = new Thread(new ThreadStart(ThreadRun02));t.Start();
        }
        void ThreadRun(int No)
        {
            Console.WriteLine($"Thread {No} 初期化完了!");
            if (No < 0) throw new IndexOutOfRangeException("\"Thread Number\"が0未満です。0以上を指定してください。");
            if (No > 100) throw new IndexOutOfRangeException("\"Thread Number\"が100を超えています。0～100を指定してください。");
            string threadURL;
            string threadPath;
            while (!ThreadExitFlg)
            {
                IsRunningThread[No - 1] = false;
                while (RunningThreads >= No)
                {
                    try
                    {
                        IsRunningThread[No] = false;
                        string URL;
                        switch (GetFormData.Type)//GetFormDataはスレッド用に構築したFormsのReadOnly構造体
                        {
                            case "Temp":
                                URL = "https://file." + GetFormData.Domain + "/Temp";
                                break;
                            case "Humi":
                                URL = "https://file." + GetFormData.Domain + "/Temp/Humi";
                                break;
                            case "hpa":
                                URL = "https://file." + GetFormData.Domain + "/Temp/hpa";
                                break;
                        }
                        DownloadData workData = Thread_GetDate(); 
                        threadURL = URL + workData.URL;
                        threadPath = workData.SaveAs;
                        CurrentURL = threadURL;
                        IsRunningThread[No] = true;
                        Download(threadURL, threadPath);
                        IsRunningThread[No] = false;
                        if (CurrentDate > EndDate)
                            IsFinished = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Thread {No} にて{ex}発生");
                    }
                }
                Thread.Sleep(100);//スレッドの設定数の範囲外であればスリープする。
            }
        }
	    struct DownloadData
        {
            public string URL;//ドメインを除くパス
            public string SaveAs;
        };
        private DownloadData Thread_GetDate()
        {
            DownloadData _D = new DownloadData();
            lock (lockObject)
            {
                DateTime CurrentDateTime = currentTime;//Formの指定されたcurrentTimeから取得
		        _D.URL = $"{CurrentDateTime.ToString("yyyyMMddHHmmss")}.csv";
                _D.SaveAs = $"{SettingSaveFolder.Text}/{CurrentDateTime.ToString("yyyyMMddHHmmss")}.csv";//フォームの指定場所に保存
                currentTime = CurrentDateTime.AddSeconds(1);
            }
            return _D;
        }
        private void Download(string URL, string path)
        {
            try
            {
                byte[] file = null;
                string tag = nt.GetSample(URL);
                if (tag == null && !nt.GetRetryFlg())
                    return;
                while (nt.GetRetryFlg())
                {
                    if (ThreadExitFlg) return;//Formが閉じた場合ThreadExitFlgが有効になる。
                    StatusMessage = nt.GetLastErrorStatus();
                    tag = nt.GetSample(URL);
                }
                if (tag == "\"XXXXXX-XXXXX-XXXXXXXXXXXXX\""||tag==null)//タグが存在しないもしくは一致する場合は無視する
                    return;
                file = nt.Get(URL); //存在する場合はコンテンツを含めたファイルをダウンロードする
                while (nt.GetRetryFlg())//エラーの場合の処理
                {
                    if (ThreadExitFlg) return;
                    StatusMessage = nt.GetLastErrorStatus();
                    file = nt.Get(URL);
                }
                WriteBinaryToFile(path, file);//pathにバイナリ配列fileをダイレクト保存
                PicturePath = path;//FormのPictureBoxに書く
                StatusMessage = "成功";
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                StatusMessage = ex.Message;
            }
            return;
        }
    }
