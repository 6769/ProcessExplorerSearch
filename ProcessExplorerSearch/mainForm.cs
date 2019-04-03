using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace ProcessExplorerSearch
{
    //占用文件进程信息结构体
    struct FILE_PROCESS_INFO_T
    {
        public string processName;  //占用进程名称
        public int processPid;      //占用进程PID
        public string fileType;     //文件类型
        public string filePath;     //文件路径(含文件名称)
    };

    //显示进程组件结构体
    struct FILE_PROCESS_INFO_COM_T
    {
        public TextBox processName;  //占用进程名称
        public TextBox processPid;   //占用进程PID
        public TextBox fileType;     //文件类型
        public TextBox filePath;     //文件路径(含文件名称)

        public CheckBox selectBox;   //是否被勾选框
    };

    public partial class mainForm : Form
    {
        private static int s_iProcessNum = 0;
        private static FILE_PROCESS_INFO_T[] s_ntFileProcInfo = null;
        private static FILE_PROCESS_INFO_COM_T[] s_ntInfoShowCom = null;
        private static string s_userSearchFilePath = "";

        public mainForm()
        {
            InitializeComponent();
        }

        //根据PID结束进程
        private bool killProcessByPid(int iPid)
        {
            try
            {
                Process.GetProcessById(iPid).Kill();
            }
            catch
            {
                return false;
            }

            return true;
        }

        //查找文件被占用进程信息
        private void findFileOccuppiedProcessInfo(string fileName, bool bShowNoneTip = true)
        {
            if ("" == fileName || null == fileName)
                return;

            if(fileName.Length < 3)
            {
                MessageBox.Show("请输入至少3个字符!");
                return;
            }

            Process tool = new Process();
            tool.StartInfo.FileName = "handle.exe";//占用文件的进程
            tool.StartInfo.Arguments = fileName + " /accepteula";
            tool.StartInfo.UseShellExecute = false;
            tool.StartInfo.RedirectStandardOutput = true;
            tool.StartInfo.CreateNoWindow = true;  //不显示控制台程序窗口
            tool.Start();
            tool.WaitForExit();

            string outputTool = tool.StandardOutput.ReadToEnd();
            string[] nStrResArray = outputTool.Split('\n');

            //经过解析
            //无文件被占用时 nStrResArray.Length = 7 ,且 nStrResArray[5] = "No matching handles found."
            //仅1个文件被占用时 nStrResArray.Length = 7 ,nStrResArray[5] = "第一个被占用进程信息"
            //n个同名文件被占用时 nStrResArray.Length = 7+n-1 ,nStrResArray[5+n-1] = "第n个被占用进程信息"

            if (true == nStrResArray[5].Contains("No matching handles found."))
            {
                clearProceinfoPanel();
                s_iProcessNum = 0;

                if(true == bShowNoneTip)
                {
                    //MessageBox.Show();
                    toolStripStatusLabel1.Text = "未查找到占用进程";
                }
                
                return;
            }

            s_iProcessNum = nStrResArray.Length - 7 + 1;
            s_ntFileProcInfo = new FILE_PROCESS_INFO_T[s_iProcessNum];

            for (int i = 0; i < s_iProcessNum; i++)
            {
                // nStrResArray[5 + i] 解析为:
                // wps.exe            pid: 21364  type: File           728: F:\C#\ProcessExplorerSearch\ProcessExplorerSearch\bin\Debug\aaa.docx

                // nStrResArray[5 + i]处理分解为:
                // wps.exe            pid
                // 21364  type
                // File           728
                // F
                //\C#\ProcessExplorerSearch\ProcessExplorerSearch\bin\Debug\aaa.docx
                string[] nStrAllTemp = nStrResArray[5 + i].Split(':');
                string strNameTemp = nStrAllTemp[0];
                string strPidTemp = nStrAllTemp[1];
                string strTypeTemp = nStrAllTemp[2];
                string strPathTemp = null;

                //有些占用文件返回路径可能不含盘符
                if(nStrAllTemp.Length < 5)
                    strPathTemp = nStrAllTemp[3];
                else
                    strPathTemp = nStrAllTemp[3] + ":" + nStrAllTemp[4];

                //进一步提取具体信息
                s_ntFileProcInfo[i].processName = strNameTemp.Split(' ')[0];
                s_ntFileProcInfo[i].processPid = int.Parse(strPidTemp.Split(' ')[1]);
                s_ntFileProcInfo[i].fileType = strTypeTemp;
                s_ntFileProcInfo[i].filePath = strPathTemp;

                //MessageBox.Show(s_ntFileProcInfo[i].processName);
                //MessageBox.Show(s_ntFileProcInfo[i].processPid.ToString());
                //MessageBox.Show(strTypeTemp);
                //MessageBox.Show(strPathTemp);
            }

            toolStripStatusLabel1.Text = "共查找到 " + s_iProcessNum.ToString() + " 个信息";
        }

        //清空面板中已查询到的进程信息
        private void clearProceinfoPanel()
        {
            int i = 0;

            if (null != s_ntInfoShowCom)
            {
                for (i = 0; i < s_ntInfoShowCom.Length; i++)
                {
                    flowLayoutPanel1.Controls.Remove(s_ntInfoShowCom[i].processName);
                    flowLayoutPanel1.Controls.Remove(s_ntInfoShowCom[i].processPid);
                    flowLayoutPanel1.Controls.Remove(s_ntInfoShowCom[i].fileType);
                    flowLayoutPanel1.Controls.Remove(s_ntInfoShowCom[i].filePath);
                    flowLayoutPanel1.Controls.Remove(s_ntInfoShowCom[i].selectBox);
                }
            }
        }

        private void showSearchProcessInfo()
        {
            if (s_iProcessNum <= 0)
                return;

            clearProceinfoPanel();

            s_ntInfoShowCom = new FILE_PROCESS_INFO_COM_T[s_iProcessNum];
            FILE_PROCESS_INFO_T tInfoTemp = new FILE_PROCESS_INFO_T();

            for (int i = 0; i < s_iProcessNum; i++)
            {
                tInfoTemp = s_ntFileProcInfo[i];

                s_ntInfoShowCom[i].processName = new TextBox();
                s_ntInfoShowCom[i].processName.ReadOnly = true;
                s_ntInfoShowCom[i].processName.Width = 100;
                s_ntInfoShowCom[i].processName.Height = 21;
                s_ntInfoShowCom[i].processName.Text = tInfoTemp.processName;
                flowLayoutPanel1.Controls.Add(s_ntInfoShowCom[i].processName);

                s_ntInfoShowCom[i].processPid = new TextBox();
                s_ntInfoShowCom[i].processPid.ReadOnly = true;
                s_ntInfoShowCom[i].processPid.Width = 52;
                s_ntInfoShowCom[i].processPid.Height = 21;
                s_ntInfoShowCom[i].processPid.Text = tInfoTemp.processPid.ToString();
                flowLayoutPanel1.Controls.Add(s_ntInfoShowCom[i].processPid);

                s_ntInfoShowCom[i].fileType = new TextBox();
                s_ntInfoShowCom[i].fileType.ReadOnly = true;
                s_ntInfoShowCom[i].fileType.Width = 45;
                s_ntInfoShowCom[i].fileType.Height = 21;
                s_ntInfoShowCom[i].fileType.Text = tInfoTemp.fileType;
                flowLayoutPanel1.Controls.Add(s_ntInfoShowCom[i].fileType);

                s_ntInfoShowCom[i].filePath = new TextBox();
                s_ntInfoShowCom[i].filePath.ReadOnly = true;
                s_ntInfoShowCom[i].filePath.Text = tInfoTemp.filePath;
                s_ntInfoShowCom[i].filePath.Width = 348;
                s_ntInfoShowCom[i].filePath.Height = 21;
                flowLayoutPanel1.Controls.Add(s_ntInfoShowCom[i].filePath);

                s_ntInfoShowCom[i].selectBox = new CheckBox();
                s_ntInfoShowCom[i].selectBox.Width = 21;
                s_ntInfoShowCom[i].selectBox.Height = 21;
                s_ntInfoShowCom[i].selectBox.Text = "";
                flowLayoutPanel1.Controls.Add(s_ntInfoShowCom[i].selectBox);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if ("" == s_userSearchFilePath)
                return;

            findFileOccuppiedProcessInfo(s_userSearchFilePath,true);
            showSearchProcessInfo();
            
        }

        private void btnFilePath_Click(object sender, EventArgs e)
        {
            //获取主程序所在路径
            string mainExePath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;          

            openFileDialog1.Filter = "文件(*.*)|*.*";
            openFileDialog1.InitialDirectory = mainExePath;
            openFileDialog1.Multiselect = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBoxFilePath.Text = openFileDialog1.FileName;
                s_userSearchFilePath = openFileDialog1.FileName;
            }
        }

        private void textBoxFilePath_TextChanged(object sender, EventArgs e)
        {
            s_userSearchFilePath = textBoxFilePath.Text;
        }

        private void btnKillProcess_Click(object sender, EventArgs e)
        {
            int i = 0;

            if (s_iProcessNum <= 0)
                return;

            if (null == s_ntInfoShowCom)
                return;

            for(i = 0; i < s_iProcessNum; i++)
            {
                if(true == s_ntInfoShowCom[i].selectBox.Checked)
                {
                    if(DialogResult.OK == MessageBox.Show("确定要关闭 "+ s_ntFileProcInfo[i].processName + " 进程吗?(请慎重！)", "重要提醒", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning))
                    {
                        killProcessByPid(s_ntFileProcInfo[i].processPid);

                        //刷新占用进程
                        findFileOccuppiedProcessInfo(s_userSearchFilePath,false);
                        showSearchProcessInfo();
                        break;
                    } 
                }
            }
        }
    }
}
