using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace L2_login
{
    public class DX_Keyboard
    {
        private Thread dx_keyboard_thread;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12;

        public DX_Keyboard()
        {
            dx_keyboard_thread = new Thread(new ThreadStart(DX_KeyboardEngine));
            dx_keyboard_thread.IsBackground = true;
            dx_keyboard_thread.Start();
        }

        private void DX_KeyboardEngine()
        {
            while (true == true)
            {
                Thread.Sleep(Globals.SLEEP_DirectInputDelay);
                UpdateKeyboard();
            }
        }

        private void UpdateKeyboard()
        {
            bool pressed = false;

            int vkToggle = GetVirtualKeyCode(Globals.DirectInputKey);
            int vkKill = GetVirtualKeyCode(Globals.DirectInputKey2);

            if (vkToggle != 0 && (GetAsyncKeyState(vkToggle) & 0x8000) != 0)
            {
                pressed = true;
                if (Globals.DirectInputLast == false)
                {
                    Globals.l2net_home.Toggle_Botting();
                    Globals.DirectInputLast = true;
                }
            }
            else if (Globals.DirectInputSetup)
            {
                for (int i = 0; i < 256; i++)
                {
                    if ((GetAsyncKeyState(i) & 0x8000) != 0)
                    {
                        Globals.DirectInputSetupValue = ((Keys)i).ToString();
                        try
                        {
                            Globals.DirectInputSetup = false;
                            Globals.setupwindow.label_toggle_key.Text = Globals.DirectInputSetupValue;
                            Globals.setupwindow.button_change_toggle.Enabled = true;
                            Globals.setupwindow.button_change_kill.Enabled = true;
                            Globals.setupwindow.comboBox_voice.Enabled = true;
                            Globals.setupwindow.textBox_l2path.Enabled = true;
                            Globals.setupwindow.textBox_key.Enabled = true;
                            Globals.setupwindow.comboBox_texturemode.Enabled = true;
                            Globals.setupwindow.comboBox_viewrange.Enabled = true;
                        }
                        catch { }
                        break;
                    }
                }
            }

            if (vkKill != 0 && (GetAsyncKeyState(vkKill) & 0x8000) != 0)
            {
                pressed = true;
                if (Globals.DirectInputLast2 == false)
                {
                    Globals.DirectInputLast2 = true;
                    Util.KillThreads();
                    Util.Stop_Connections();
                }
            }
            else if (Globals.DirectInputSetup2)
            {
                for (int i = 0; i < 256; i++)
                {
                    if ((GetAsyncKeyState(i) & 0x8000) != 0)
                    {
                        Globals.DirectInputSetupValue2 = ((Keys)i).ToString();
                        try
                        {
                            Globals.DirectInputSetup2 = false;
                            Globals.setupwindow.label_kill_key.Text = Globals.DirectInputSetupValue2;
                            Globals.setupwindow.button_change_kill.Enabled = true;
                            Globals.setupwindow.button_change_toggle.Enabled = true;
                            Globals.setupwindow.comboBox_voice.Enabled = true;
                            Globals.setupwindow.textBox_l2path.Enabled = true;
                            Globals.setupwindow.textBox_key.Enabled = true;
                            Globals.setupwindow.comboBox_texturemode.Enabled = true;
                            Globals.setupwindow.comboBox_viewrange.Enabled = true;
                        }
                        catch { }
                        break;
                    }
                }
            }

            if (!pressed)
            {
                Globals.DirectInputLast = false;
                Globals.DirectInputLast2 = false;
            }
        }

        private int GetVirtualKeyCode(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
                return 0;

            try
            {
                foreach (Keys k in Enum.GetValues(typeof(Keys)))
                {
                    if (k.ToString() == keyName)
                        return (int)k;
                }
            }
            catch { }

            return 0;
        }
    }
}
