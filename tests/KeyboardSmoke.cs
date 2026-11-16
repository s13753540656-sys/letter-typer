// Real SendKeys test confined to a temporary textbox in this process.
// It checks foreground/focus before EVERY send; it never targets a browser.
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
public static class KeyboardSmoke
{
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    public static string Run()
    {
        Application.EnableVisualStyles();
        using (var form = new Form { Text = "LetterTyper local keyboard verification", ClientSize = new Size(500, 160), StartPosition = FormStartPosition.CenterScreen })
        {
            var box = new TextBox { Multiline = true, Dock = DockStyle.Fill };
            form.Controls.Add(box);
            string expected = "Az 09! +^%~()[]{}\nSecond line.";
            string result = "FAIL: no verification performed";
            var timer = new Timer { Interval = 60 }; int index = 0;
            form.Shown += delegate { form.Activate(); box.Focus(); timer.Start(); };
            timer.Tick += delegate {
                timer.Stop();
                try {
                    if (GetForegroundWindow() != form.Handle || !box.Focused) throw new Exception("Test window lost focus; no further keys sent.");
                    if (index < expected.Length) {
                        SendKeys.SendWait(InputSession.Encode(expected[index])); index++; timer.Start();
                    } else {
                        result = box.Text.Replace("\r\n", "\n") == expected ? "PASS: local textbox received all 30 characters including escaped symbols and newline." : "FAIL: local textbox content mismatch";
                        form.Close();
                    }
                } catch (Exception error) { result = "FAIL: " + error.Message; form.Close(); }
            };
            form.FormClosed += delegate { timer.Stop(); };
            Application.Run(form); timer.Dispose(); return result;
        }
    }
}
