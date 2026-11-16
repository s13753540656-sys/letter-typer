using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

public sealed class TyperConfig
{
    public int countdownSeconds = 8, intervalMs = 70, testCharacters = 6;
    public string[] titleKeywords = { "批改", "pigai" };
    public string[] browserProcesses = { "msedge", "chrome", "firefox", "brave", "opera", "vivaldi" };
    public void Validate()
    {
        if (countdownSeconds < 3 || countdownSeconds > 60) throw new Exception("倒计时必须为 3–60 秒。");
        if (intervalMs < 30 || intervalMs > 500) throw new Exception("字符间隔必须为 30–500 毫秒。");
        if (testCharacters < 1 || testCharacters > 20) throw new Exception("测试字符数必须为 1–20。");
        ValidateList(titleKeywords); ValidateList(browserProcesses);
    }
    static void ValidateList(string[] values)
    {
        if (values == null || values.Length == 0) throw new Exception("窗口匹配列表不能为空。");
        foreach (string value in values)
            if (String.IsNullOrWhiteSpace(value) || value.Contains("*") || value.Contains("?"))
                throw new Exception("匹配规则必须是非空文本，不能含通配符。");
    }
    public static TyperConfig Load(string root)
    {
        string path = Path.Combine(root, "config.local.json");
        var result = File.Exists(path) ? new JavaScriptSerializer().Deserialize<TyperConfig>(File.ReadAllText(path, Encoding.UTF8)) : new TyperConfig();
        if (result == null) throw new Exception("配置为空。");
        result.Validate(); return result;
    }
}

public sealed class InputSession
{
    public readonly string Text;
    public readonly int TestCount;
    public int Position { get; private set; }
    public bool Tested { get; private set; }
    public bool Finished { get { return Position >= Text.Length; } }
    public bool NeedsTest { get { return !Tested; } }
    public InputSession(string text, int testCount)
    {
        Text = Normalize(text); TestCount = Math.Min(testCount, Text.Length);
        if (testCount < 1) throw new Exception("测试长度必须大于零。");
    }
    public bool SendNext(Action<char> sender)
    {
        if (Finished) return true;
        sender(Text[Position]);
        Position++;
        if (!Tested && Position >= TestCount) { Tested = true; return true; }
        return Finished;
    }
    public void Reset() { Position = 0; Tested = false; }
    public static string Normalize(string text)
    {
        if (text == null) throw new Exception("文本为空。");
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        if (text.Length == 0 || text.Length > 100000) throw new Exception("请选择 1–100000 个字符的文本。");
        foreach (char c in text) Encode(c);
        return text;
    }
    public static string Encode(char c)
    {
        if (c == '\n') return "{ENTER}";
        if (c < 32 || c > 126) throw new Exception("只支持英文 ASCII、普通空格和换行；请检查中文、弯引号、制表符等字符。");
        if (c == '{') return "{{}";
        if (c == '}') return "{}}";
        if ("+^%~()[]".IndexOf(c) >= 0) return "{" + c + "}";
        return c.ToString();
    }
}

public sealed class LetterTyper : Form
{
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint id);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
    readonly TyperConfig config;
    InputSession session;
    readonly TextBox preview = new TextBox();
    readonly Label status = new Label(), progress = new Label(), expected = new Label();
    readonly Button load = new Button(), test = new Button(), run = new Button(), stop = new Button(), reset = new Button();
    readonly CheckBox confirmed = new CheckBox();
    readonly NumericUpDown interval = new NumericUpDown();
    readonly Timer timer = new Timer();
    DateTime deadline;
    IntPtr target;
    string targetTitle;
    bool active, sending, inTick;

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            string root = args.Length > 0 ? args[0] : AppDomain.CurrentDomain.BaseDirectory;
            if (Array.IndexOf(args, "--self-check") >= 0) return ProjectChecks.Run();
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            var form = new LetterTyper(TyperConfig.Load(root), File.ReadAllText(Path.Combine(root, "examples", "sample.txt"), new UTF8Encoding(false, true)));
            int shot = Array.IndexOf(args, "--screenshot");
            if (shot >= 0)
            {
                if (shot + 1 >= args.Length) throw new Exception("Missing screenshot path.");
                form.Shown += delegate {
                    form.BeginInvoke(new Action(delegate {
                        form.Refresh();
                        using (var bitmap = new Bitmap(form.Width, form.Height))
                        {
                            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                            bitmap.Save(args[shot + 1], System.Drawing.Imaging.ImageFormat.Png);
                        }
                        form.Close();
                    }));
                };
            }
            Application.Run(form); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }
    public LetterTyper(TyperConfig settings, string text)
    {
        config = settings; config.Validate(); session = new InputSession(text, config.testCharacters);
        Text = "LetterTyper · 本机逐字输入"; ClientSize = new Size(760, 650);
        FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen; AutoScaleMode = AutoScaleMode.None;
        Font = new Font("Microsoft YaHei UI", 10); BackColor = Color.FromArgb(247, 249, 252);
        Controls.Add(new Label { Left = 20, Top = 15, Width = 720, Height = 34, Text = "LetterTyper 1.0.0  |  本机逐字输入", Font = new Font(Font.FontFamily, 16, FontStyle.Bold) });
        Controls.Add(new Label { Left = 20, Top = 60, Width = 720, Height = 88,
            Text = "1. 选择自己的 UTF-8 英文文本；默认仅加载公开演示内容。\r\n2. 切换英文输入法，点击测试；倒计时内进入目标网页正文框。\r\n3. 确认测试结果，再继续输入。按 Esc 或切换窗口可暂停。" });
        load.Text = "选择本机文本 (.txt)"; load.SetBounds(20, 155, 220, 36); Controls.Add(load);
        Controls.Add(new Label { Left = 260, Top = 162, Width = 480, Height = 25, Text = "不联网 · 不读取登录凭据 · 不自动提交" });
        preview.SetBounds(20, 205, 720, 210); preview.Multiline = true; preview.ReadOnly = true;
        preview.ScrollBars = ScrollBars.Vertical; preview.BackColor = Color.White; Controls.Add(preview);
        expected.SetBounds(20, 428, 720, 30); Controls.Add(expected);
        Controls.Add(new Label { Left = 20, Top = 468, Width = 195, Text = "每个字符间隔（毫秒）" });
        interval.SetBounds(220, 464, 85, 28); interval.Minimum = 30; interval.Maximum = 500; interval.Value = config.intervalMs; Controls.Add(interval);
        confirmed.Text = "我已确认网页测试文字正确"; confirmed.SetBounds(335, 464, 400, 30); confirmed.Enabled = false; Controls.Add(confirmed);
        test.SetBounds(20, 510, 175, 38); run.Text = "继续输入"; run.SetBounds(210, 510, 155, 38);
        stop.Text = "停止 / Esc"; stop.SetBounds(380, 510, 155, 38); reset.Text = "重置进度"; reset.SetBounds(550, 510, 190, 38);
        Controls.AddRange(new Control[] { test, run, stop, reset });
        status.SetBounds(20, 563, 720, 42); progress.SetBounds(20, 614, 720, 26); Controls.Add(status); Controls.Add(progress);
        load.Click += LoadFile; test.Click += delegate { Begin(); }; run.Click += delegate { Begin(); };
        stop.Click += delegate { Pause("已暂停。请把网页光标放在已输入文字末尾后继续。"); };
        reset.Click += delegate {
            if (MessageBox.Show("重置不会清空网页。请先自行清空网页正文，避免重复输入。", "重置进度", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
            session.Reset(); confirmed.Checked = false; RefreshText();
        };
        confirmed.CheckedChanged += delegate { SetButtons(); };
        timer.Tick += Tick; FormClosing += delegate { timer.Stop(); }; FormClosed += delegate { timer.Dispose(); };
        RefreshText();
    }
    void LoadFile(object sender, EventArgs args)
    {
        using (var dialog = new OpenFileDialog { Filter = "UTF-8 text (*.txt)|*.txt", Title = "选择本机英文文本", CheckFileExists = true, RestoreDirectory = true })
        {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                if (new FileInfo(dialog.FileName).Length > 400000) throw new Exception("文件过大，请限制在 100000 个字符以内。");
                var next = new InputSession(File.ReadAllText(dialog.FileName, new UTF8Encoding(false, true)), config.testCharacters);
                session = next; confirmed.Checked = false; RefreshText();
            }
            catch (Exception error) { MessageBox.Show(error.Message, "未加载：请检查文本格式"); }
        }
    }
    void RefreshText()
    {
        preview.Text = session.Text.Replace("\n", "\r\n");
        string prefix = session.Text.Substring(0, session.TestCount).Replace(" ", "␠").Replace("\n", "↵");
        expected.Text = "测试应显示：“" + prefix + "”（␠ 表示空格，↵ 表示换行）";
        test.Text = "测试前 " + session.TestCount + " 个字符";
        status.Text = "就绪。仅匹配配置中的浏览器进程和窗口标题；请确认光标位置。";
        UpdateProgress(); SetButtons();
    }
    void UpdateProgress() { progress.Text = "已发送 " + session.Position + " / " + session.Text.Length + " 个字符（含空格、换行；不代表网页已接收）。"; }
    void SetButtons()
    {
        test.Enabled = !active && session.NeedsTest;
        run.Enabled = !active && !session.NeedsTest && confirmed.Checked && !session.Finished;
        load.Enabled = reset.Enabled = interval.Enabled = !active;
        confirmed.Enabled = !active && !session.NeedsTest; stop.Enabled = active;
    }
    void Begin()
    {
        if (session.Finished || (!session.NeedsTest && !confirmed.Checked)) return;
        active = true; sending = false; target = IntPtr.Zero; targetTitle = null;
        deadline = DateTime.UtcNow.AddSeconds(config.countdownSeconds);
        SetButtons(); timer.Interval = 50; timer.Start();
        status.Text = "倒计时开始：请切换到目标网页正文框，并松开所有快捷键。";
    }
    void Pause(string message) { timer.Stop(); active = false; sending = false; SetButtons(); status.Text = message; }
    static bool IsDown(int key) { return (GetAsyncKeyState(key) & 0x8000) != 0; }
    static string WindowTitle(IntPtr window)
    {
        var text = new StringBuilder(2048); GetWindowText(window, text, text.Capacity); return text.ToString();
    }
    bool MatchesTarget(IntPtr window)
    {
        uint id; GetWindowThreadProcessId(window, out id);
        try
        {
            using (var process = Process.GetProcessById((int)id))
            {
                bool browser = false;
                foreach (string name in config.browserProcesses) if (String.Equals(name, process.ProcessName, StringComparison.OrdinalIgnoreCase)) browser = true;
                if (!browser) return false;
            }
            string title = WindowTitle(window);
            foreach (string keyword in config.titleKeywords) if (title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
        catch { return false; }
    }
    void Tick(object sender, EventArgs args)
    {
        if (inTick || !active) return;
        inTick = true;
        try
        {
            if (IsDown(0x1B)) { Pause("Esc 已暂停。"); return; }
            if (!sending)
            {
                double remaining = (deadline - DateTime.UtcNow).TotalSeconds;
                if (remaining > 0) { status.Text = "倒计时 " + Math.Ceiling(remaining) + " 秒：请点击目标网页的正文框。"; return; }
                target = GetForegroundWindow();
                if (!MatchesTarget(target)) { Pause("前台窗口不匹配。请检查浏览器、页面标题和配置。"); return; }
                targetTitle = WindowTitle(target); sending = true; timer.Interval = (int)interval.Value;
            }
            if (GetForegroundWindow() != target || WindowTitle(target) != targetTitle || !MatchesTarget(target))
            { Pause("窗口或标题发生变化，已暂停。"); return; }
            if (IsDown(0x10) || IsDown(0x11) || IsDown(0x12) || IsDown(0x5B) || IsDown(0x5C))
            { Pause("检测到 Shift / Ctrl / Alt / Win 按住，已暂停。"); return; }
            bool boundary = session.SendNext(delegate(char c) { SendKeys.SendWait(InputSession.Encode(c)); });
            UpdateProgress();
            if (boundary) Pause(session.Finished ? "已发送全部文字。请检查网页内容，再自行提交。" : "测试结束。请检查网页，并勾选测试确认后继续。");
            else status.Text = "正在逐字输入。按 Esc 或切换窗口可暂停。";
        }
        catch { Pause("发送失败，已暂停。请核对网页内容和光标后再决定是否继续。"); }
        finally { inTick = false; }
    }
}

public static class ProjectChecks
{
    static int count;
    static void Assert(bool condition, string name) { if (!condition) throw new Exception("FAIL: " + name); count++; }
    public static int Run()
    {
        try
        {
            count = 0;
            Assert(InputSession.Normalize("a\r\nb\rc\n") == "a\nb\nc\n", "newline normalization and preservation");
            for (int c = 32; c <= 126; c++) Assert(InputSession.Encode((char)c).Length > 0, "ASCII mapping " + c);
            Assert(InputSession.Encode('{') == "{{}" && InputSession.Encode('}') == "{}}", "brace escaping");
            foreach (char c in "+^%~()[]") Assert(InputSession.Encode(c) == "{" + c + "}", "special escaping");
            foreach (string invalid in new[] { "", "中文", "tab\t", "curly ’", "\0" })
            { bool rejected = false; try { InputSession.Normalize(invalid); } catch { rejected = true; } Assert(rejected, "invalid text rejected"); }
            var output = new StringBuilder(); var session = new InputSession("Hello world!", 6);
            for (int i = 0; i < 3; i++) session.SendNext(delegate(char c) { output.Append(c); });
            Assert(session.NeedsTest && session.Position == 3, "partial test resume");
            for (int i = 0; i < 3; i++) session.SendNext(delegate(char c) { output.Append(c); });
            Assert(session.Tested && output.ToString() == "Hello ", "test boundary");
            while (!session.Finished) session.SendNext(delegate(char c) { output.Append(c); });
            Assert(output.ToString() == "Hello world!", "continue without duplication");
            session.Reset(); bool failed = false;
            try { session.SendNext(delegate(char c) { throw new Exception("simulated failure"); }); } catch { failed = true; }
            Assert(failed && session.Position == 0 && !session.Tested, "failed send preserves position");
            var shortSession = new InputSession("Hi", 6); shortSession.SendNext(delegate(char c) {}); shortSession.SendNext(delegate(char c) {});
            Assert(shortSession.Finished && shortSession.Tested, "short input completion");
            new TyperConfig().Validate(); count++;
            var bad = new TyperConfig { countdownSeconds = 0 }; bool rejectedConfig = false;
            try { bad.Validate(); } catch { rejectedConfig = true; } Assert(rejectedConfig, "config bounds");
            bad = new TyperConfig { titleKeywords = new[] { "*" } }; rejectedConfig = false;
            try { bad.Validate(); } catch { rejectedConfig = true; } Assert(rejectedConfig, "wildcard rejected");
            Console.WriteLine("PASS: " + count + " checks. No keyboard events sent."); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }
}
