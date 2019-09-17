using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CMYKtoRGB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public struct Log
        {
            public int ProcessedRGB;
            public int? ProcessedThumb;
            public int SkippedRGB;
            public int? SkippedThumb;
            public int ErrorRGB;
            public int? ErrorThumb;
            public DateTime? StartTime;
            public DateTime? EndTime;
            public Log(
                int processedRGB = 0,
                int? processedThumb = null,
                int skippedRGB = 0,
                int? skippedThumb = null,
                int errorRGB = 0,
                int? errorThumb = null,
                DateTime? startTime = null,
                DateTime? endTime = null)
            {
                ProcessedRGB = processedRGB;
                ProcessedThumb = processedThumb;
                SkippedRGB = skippedRGB;
                SkippedThumb = skippedThumb;
                ErrorRGB = errorRGB;
                ErrorThumb = errorThumb;
                StartTime = startTime;
                EndTime = endTime;
            }
        }

        private FileInfo[] m_imageFilesToProcess;
        private Log m_currentLog;
        private string m_chosenFolderPath;
        // Image conversion will be run in background as not to block the UI thread
        private BackgroundWorker m_bw;
        public MainWindow()
        {
            InitializeComponent();
            ResetLog();
        }

        private string LogCountUIText(int rgbCount, int? thumbCount)
        {
            string result = string.Format("{0}", rgbCount);
            if (thumbCount != null)
            {
                result = string.Format("{0} | {1}", result, thumbCount);
            }
            return result;
        }

        private string LogTimeUIText(DateTime? dt) {
            string result = string.Empty;
            if(dt != null)
            {
               result = dt.Value.ToString("HH時mm分ss秒");
            }
            return result;
        }

        private void UpdateLogCountUI()
        {
            lblTotal.Content = m_imageFilesToProcess.Length;
            btnStart.IsEnabled = m_imageFilesToProcess.Length > 0;

            lblProcessed.Content = LogCountUIText(m_currentLog.ProcessedRGB, m_currentLog.ProcessedThumb);
            lblSkip.Content = LogCountUIText(m_currentLog.SkippedRGB, m_currentLog.SkippedThumb);
            lblError.Content = LogCountUIText(m_currentLog.ErrorRGB, m_currentLog.ErrorThumb);
        }

        private void UpdateLogTimeUI()
        {
            lblStartTime.Content = LogTimeUIText(m_currentLog.StartTime);
            lblEndTime.Content = LogTimeUIText(m_currentLog.EndTime);
        }

        private void ResetLog()
        {
            m_currentLog = new Log();
            m_imageFilesToProcess = new FileInfo[0];
            UpdateLogCountUI();
            UpdateLogTimeUI();
        }

        private void ToggleStartStopButtons(bool showStart = true)
        {
            if (showStart)
            {
                btnStart.Visibility = Visibility.Visible;
                btnStop.Visibility = Visibility.Collapsed;
                btnStop.IsEnabled = false;
                btnChooseFolder.IsEnabled = true;
                chkThumbnail.IsChecked = true;
            }
            else
            {
                btnStart.Visibility = Visibility.Collapsed;
                btnStop.IsEnabled = true;
                btnStop.Visibility = Visibility.Visible;
                btnChooseFolder.IsEnabled = false;
                chkThumbnail.IsChecked = false;
            }
        }



        private void BtnChooseFolder_Click(object sender, RoutedEventArgs e)
        {
            var chosenFolder = ImageTools.ChooseFolder(out m_chosenFolderPath);
            lblFolderPath.Content = m_chosenFolderPath;
            if (chosenFolder != null)
            {
                ResetLog();
                m_imageFilesToProcess = chosenFolder;
            }

            UpdateLogCountUI();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if(chkThumbnail.IsChecked == true)
            {
                m_currentLog.ProcessedThumb = 0;
                m_currentLog.SkippedThumb = 0;
                m_currentLog.ErrorThumb = 0;
            }
            ToggleStartStopButtons(false);
            m_currentLog.StartTime = DateTime.Now;

            m_bw = new BackgroundWorker();
            m_bw.WorkerSupportsCancellation = true;
            m_bw.DoWork += M_bw_DoWork;
            m_bw.RunWorkerAsync(m_imageFilesToProcess);
            m_bw.RunWorkerCompleted += M_bw_RunWorkerCompleted;
        }

        private void M_bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var bw = (BackgroundWorker)sender;
            bw.Dispose();
            bw = null;
            ToggleStartStopButtons();
            m_currentLog.EndTime = DateTime.Now;
            UpdateLogTimeUI();
            /*
            Process.Start(CalendarImageHelpers.AbsoluteOutputBaseFolderPath);
            if (Convert.ToInt32(lblError.Content) > 0)
            {
                Process.Start(CalendarImageCSVData.AbsoluteCSVOutput_ErrorFolderPath);
            }
            */
        }

        private void M_bw_DoWork(object sender, DoWorkEventArgs e)
        {
            var imageFilesToProcess = (FileInfo[])e.Argument;
            int errorCount = 0;
            int processedCount = 0;

            Parallel.For(0, imageFilesToProcess.Length, (i, loopState) =>
            {
                if (m_bw.CancellationPending)
                {
                    e.Cancel = true;
                    loopState.Stop();
                    return;
                }
                var convertResult = ImageTools.ConvertToRGB(imageFilesToProcess[i], true, false);
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() => UpdateLogCountUI()));
            });
        }

        private void LogResult(ImageTools.ConvertResult convertResult)
        {
            if (convertResult.generatedRGB == true)
            {
                m_currentLog.ProcessedRGB++;
            }
            else if (convertResult.generatedRGB == false)
            {
                m_currentLog.SkippedRGB++;
            }
            else if (convertResult.generatedRGB == null)
            {
                m_currentLog.ErrorRGB++;
            }

            if (convertResult.generatedThumb == true)
            {
                m_currentLog.ProcessedThumb++;
            }
            else if (convertResult.generatedThumb == false)
            {
                m_currentLog.SkippedThumb++;
            }
            else if (convertResult.generatedThumb  == null)
            {
                m_currentLog.ErrorThumb++;
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            btnStop.IsEnabled = false;
            if (m_bw != null)
            {
                m_bw.CancelAsync();
            }
        }
    }
}
