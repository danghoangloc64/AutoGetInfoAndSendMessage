using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Xml;
using Telegram.Bot;

namespace AutoGetInfoAndSendMessage
{
    public partial class MainForm : Form
    {
        private System.Threading.Timer timerWork;
        private FileSystemWatcher watcher = null;
        private const string DataFileName = "data";
        private const string HomeURL = "https://bankomat.sc/";
        private bool isRunning = false;
        private bool isSendMessageError = false;
        private string torDomain;
        private int tempTime;
        private int mainTime;
        public MainForm()
        {
            InitializeComponent();
        }

        private void SetEnableControl(bool enable)
        {
            txtBotTelegramToken.Enabled = enable;
            txtTime.Enabled = enable;
            txtUserName.Enabled = enable;
            txtPassword.Enabled = enable;
            btnStart.Enabled = enable;
            btnStop.Enabled = !enable;
            isRunning = !enable;
        }

        private void RunWork()
        {
            try
            {
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("--window-position=-32000,-32000");
                options.AddArgument("headless");
                options.BinaryLocation = Environment.CurrentDirectory + "\\GoogleChromePortable\\App\\Chrome-bin\\chrome.exe";
                ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;
                var driver = new ChromeDriver(service, options);
                try
                {

                    driver.Navigate().GoToUrl(HomeURL);
                    IWebElement txtUser = driver.FindElement(By.XPath("//input[@placeholder='login, case-sensitive']"));
                    txtUser.Click();
                    txtUser.Click();
                    Thread.Sleep(100);
                    txtUser.SendKeys(txtUserName.Text);
                    Thread.Sleep(200);

                    IWebElement txtPass = driver.FindElement(By.XPath("//input[@placeholder='enter password here']"));
                    txtPass.Click();
                    txtPass.Click();
                    Thread.Sleep(100);
                    txtPass.SendKeys(txtPassword.Text);
                    Thread.Sleep(200);

                    IJavaScriptExecutor executorUseData = driver;
                    IWebElement btnLogin = driver.FindElement(By.XPath("//input[@title='click to login']"));
                    executorUseData.ExecuteScript("arguments[0].click()", btnLogin);

                    Thread.Sleep(2000);


                    var elementFonts = driver.FindElements(By.XPath("//font[@color='darkred']"));
                    foreach (var elementFont in elementFonts)
                    {
                        if (elementFont.Text.Length > 31)
                        {
                            torDomain = elementFont.Text;
                        }
                    }

                    string currentURL = driver.Url;
                    string shop3URL = currentURL.Replace("&pg=readme", "").Replace("php?sid", "php?pg=shop3&sid");
                    driver.Navigate().GoToUrl(shop3URL);

                    try
                    {
                        IWebElement btnCheck = driver.FindElement(By.XPath("//a[text()='CLOSE IT NOW']"));
                        if (btnCheck != null)
                        {
                            IWebElement txtTor = driver.FindElement(By.XPath("//input[@class='dizinp']"));
                            txtTor.Click();
                            txtTor.Click();
                            Thread.Sleep(100);
                            txtTor.SendKeys(torDomain);
                            executorUseData.ExecuteScript("arguments[0].click()", btnCheck);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("DHLOC::" + ex.Message);
                    }


                    var selectCountry = driver.FindElement(By.XPath("//select[@id='selcnt']"));
                    var selectElement = new SelectElement(selectCountry);
                    var allText = selectElement.Options.Select(x => x.Text);

                    string[] oldData = null;
                    if (File.Exists(DataFileName))
                    {
                        oldData = File.ReadAllLines(DataFileName);
                    }

                    File.WriteAllLines(DataFileName, allText);
                    string[] newData = File.ReadAllLines(DataFileName);



                    string oldAU = null;
                    if (oldData != null)
                    {
                        oldAU = oldData.FirstOrDefault(x => x.Contains("AU - "));
                    }

                    string newAU = null;
                    if (newData != null)
                    {
                        newAU = newData.FirstOrDefault(x => x.Contains("AU - "));
                    }

                    if (!string.IsNullOrEmpty(newAU))
                    {
                        if (string.IsNullOrEmpty(oldAU))
                        {
                            SendMessageTelegram(string.Join(Environment.NewLine, allText));
                        }
                        else
                        {
                            string oldNum = oldAU.Split('-').Last().Trim();
                            string newNum = newAU.Split('-').Last().Trim();
                            if (oldNum != newNum)
                            {
                                SendMessageTelegram(string.Join(Environment.NewLine, allText));
                            }
                        }
                    }





                    string fileLog = DateTime.Now.ToString("ddMMyyyy");
                    string logPath = Path.Combine("Log", fileLog);
                    //File.AppendAllText(logPath, "text content" + Environment.NewLine);
                    var logData = allText.Select(x => DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy: ") + x);
                    File.AppendAllText(logPath, string.Join(Environment.NewLine, logData) + Environment.NewLine);



                    driver.Close();
                    driver.Quit();
                    mainTime = tempTime;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("DHLOC::" + ex.Message);
                    mainTime = 1 * 60 * 1000;

                    string contentError = string.Empty;
                    try
                    {
                        IWebElement error = driver.FindElement(By.XPath("//table[@class='tdauth']"));
                        if (error != null)
                        {
                            contentError = error.Text;
                        }
                    }
                    catch (Exception ex2)
                    {
                        Debug.WriteLine("DHLOC::" + ex2.Message);
                    }

                    string strError = "Lần check này có lỗi xảy ra. Vui lòng đợi. Lỗi: " + (string.IsNullOrEmpty(contentError) ? "Không xác định" : contentError);


                    if (isSendMessageError)
                    {
                        SendMessageTelegram(strError);
                    }

                    strError = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy: ") + "Lần check này có lỗi xảy ra. Vui lòng đợi. Lỗi: " + (string.IsNullOrEmpty(contentError) ? "Không xác định" : contentError);

                    string fileLog = DateTime.Now.ToString("ddMMyyyy");
                    string logPath = Path.Combine("Log", fileLog);
                    File.AppendAllText(logPath, strError + Environment.NewLine);
                    try
                    {
                        driver.Close();
                    }
                    catch (Exception ex2)
                    {
                        Debug.WriteLine("DHLOC::" + ex2.Message);
                    }

                    try
                    {
                        driver.Quit();
                    }
                    catch (Exception ex2)
                    {
                        Debug.WriteLine("DHLOC::" + ex2.Message);

                    }
                }
                finally
                {
                    try
                    {
                        driver.Close();
                    }
                    catch (Exception ex2)
                    {
                        Debug.WriteLine("DHLOC::" + ex2.Message);

                    }

                    try
                    {
                        driver.Quit();
                    }
                    catch (Exception ex2)
                    {
                        Debug.WriteLine("DHLOC::" + ex2.Message);
                    }
                    //timerWork.Change(mainTime, Timeout.Infinite);
                    //timerWork.Interval = mainTime;
                }
            }
            catch (Exception ex2)
            {
                Debug.WriteLine("DHLOC::" + ex2.Message);
            }
        }


        private void btnStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBotTelegramToken.Text))
            {
                MessageBox.Show(this, "Vui long dien day du thong tin", "Co loi xay ra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtTime.Text))
            {
                MessageBox.Show(this, "Vui long dien day du thong tin", "Co loi xay ra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtUserName.Text))
            {
                MessageBox.Show(this, "Vui long dien day du thong tin", "Co loi xay ra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show(this, "Vui long dien day du thong tin", "Co loi xay ra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Properties.Settings.Default.BotTelegramToken = txtBotTelegramToken.Text;
            Properties.Settings.Default.Save();
            SetEnableControl(false);

            mainTime = int.Parse(txtTime.Text) * 60 * 1000;
            tempTime = mainTime;

            timerWork = new System.Threading.Timer(timerWork_Tick, null, 1 * 1000, Timeout.Infinite);

            //timerWork = new System.Threading.Timer(timerWork_Tick)
            //timerWork.toc = mainTime;
            //timerWork.Enabled = true;
            //timerWork_Tick(null, EventArgs.Empty);

        }

        private void timerWork_Tick(object state)
        {
            timerWork.Dispose();
         //   SendMessageTelegram("Bắt đầu chạy");
            // timerWork.Dispose();
            // timerWork.Change(mainTime, Timeout.Infinite);
            RunWork();
            timerWork = new System.Threading.Timer(timerWork_Tick, null, mainTime, Timeout.Infinite);
            //  timerWork = new System.Threading.Timer(timerWork_Tick, null, mainTime, Timeout.Infinite);
        }

        //private void timerWork_Tick(object state)
        //{
        //    throw new NotImplementedException();
        //}

        private async void SendMessageTelegram(string message)
        {
            try
            {
                var bot = new TelegramBotClient(txtBotTelegramToken.Text);
                var update = await bot.GetUpdatesAsync();
                var Ids = update.Select(x => x.Message.Chat.Id).Distinct();
                foreach (var id in Ids)
                {
                    File.AppendAllText("chatdata", id + Environment.NewLine);
                }
                if (File.Exists("chatdata"))
                {
                    var chatIDs = File.ReadAllLines("chatdata").Distinct();
                    File.WriteAllLines("chatdata", chatIDs);
                    foreach (var id in chatIDs)
                    {
                        await bot.SendTextMessageAsync(id, message);
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("DHLOC::" + ex.Message);
            }
        }

        private void txtTime_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void btnCheckSendMessage_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBotTelegramToken.Text))
            {
                MessageBox.Show(this, "Vui long dien day du thong tin", "Co loi xay ra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            SendMessageTelegram("Test send message.");
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            SetEnableControl(true);
            timerWork.Dispose();
            // timerWork.Enabled = false;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            txtBotTelegramToken.Text = Properties.Settings.Default.BotTelegramToken;
        }

        private void richTextBoxLog_TextChanged(object sender, EventArgs e)
        {
            var rtb = sender as RichTextBox;
            rtb.SelectionStart = rtb.Text.Length;
            rtb.ScrollToCaret();
        }

        public void CreateFileWatcher()
        {
            watcher = new FileSystemWatcher();
            watcher.Path = Path.Combine(Environment.CurrentDirectory, "Log");
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = "*";

            watcher.Changed += new FileSystemEventHandler(OnChanged);

            watcher.EnableRaisingEvents = true;
        }

        // Define the event handlers.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            Thread.Sleep(2000);
            richTextBoxLog.Invoke(new Action(() =>
            {
                richTextBoxLog.Text = File.ReadAllText(e.FullPath);
            }));
        }

        private void checkBoxLog_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLog.Checked)
            {
                CreateFileWatcher();
                richTextBoxLog.Text = "Sẽ hiện log ở lần check sau.";
            }
            else
            {
                watcher.Dispose();
                richTextBoxLog.Clear();
            }
        }

        private void checkBoxSendError_CheckedChanged(object sender, EventArgs e)
        {
            isSendMessageError = checkBoxSendError.Checked;
        }

        //private void timerWork_Tick(object sender, EventArgs e)
        //{
        //    try

        //    {
        //        new Thread(() =>
        //        {
        //            RunWork();
        //        }).Start();
        //    }
        //    catch
        //    {

        //    }
        //    //await Task.Run(() =>);
        //}
    }
}
