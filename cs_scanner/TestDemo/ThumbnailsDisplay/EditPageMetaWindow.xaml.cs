using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using UFileClient.Common;
namespace UFileClient.Display
{
    public delegate bool MyInvoke(FileMetaData f);
    /// <summary>
    /// 修改页文件窗口对应的交互逻辑
    /// 该类实现增加和修改页文件（修改文件操作下）的功能。
    /// </summary>
    public partial class EditPageMetaWindow : Window
    {
        //构造方法。
        public EditPageMetaWindow()
        {
            InitializeComponent();

        }
        public EditPageMetaWindow(MyInvoke myInvoke)
        {
            InitializeComponent();
            this.mi = myInvoke;
        }
        public MyInvoke mi = null;

        //定义一个Boolean类型值，来判断是否点击添加或者修改按钮，若点击了值改为true.
        public Boolean confirmTag = false;


        /// <summary>
        ///【修改】按钮按下触发的动作。
        /// </summary>
        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            //各个窗口可用，下面的应该放在保存按钮下
            textBoxDesp.IsEnabled = true;
            textBoxName.IsEnabled = true;
            textBoxTemplateID.IsEnabled = true;
            //为页文件对象的文件名赋值
            //propertyPageFile.PageName = textBoxName.Text.Trim();
            ////为页文件对象的页类型赋值
            //propertyPageFile.PageType = textBoxFileType.Text.Trim();
            ////为页文件对象的页描述信息赋值
            //propertyPageFile.PageDesp = textBoxDesp.Text.Trim();
            //把confirmTag的值设置为true，则表示点击了修改按钮
            confirmTag = true;
            buttonUpdate.IsEnabled = false;
            buttonSave.IsEnabled = true;
            //关闭当前窗口

        }

        /// <summary>
        ///【取消】按钮按下触发的动作。
        /// </summary>
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            //关闭当前窗口
            this.Close();
        }


        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            FileMetaData ftemp = new FileMetaData();
            if (confirmTag == true)
            {
                string desp = textBoxDesp.Text;
                string name = textBoxName.Text;
                string templateId = textBoxTemplateID.Text;
                bool IsCheck = checkTextBox(name, desp, templateId);
                if (IsCheck)
                {
                    ftemp.FileName = textBoxName.Text;
                    ftemp.FileType = textBoxFileType.Text;
                    ftemp.FileDesp = textBoxDesp.Text;
                    ftemp.TemplateId = textBoxTemplateID.Text;
                    this.mi(ftemp);
                    System.Windows.Forms.MessageBox.Show("保存成功");
                    this.Close();
                }
                else 
                {
                    return;
                }

            }
            this.Close();
        }
        private bool checkTextBox(string name,string desp ,string templateId )
        {
            if (name.Contains("."))
            {
                System.Windows.Forms.MessageBox.Show("文件名字中不可以包含 . 符号，请重新输入");
                return false;
            }
            if (name.Length > 256)
            {
                System.Windows.Forms.MessageBox.Show("文件名字过长，请重新输入");
                return false;
            }
            if (string.IsNullOrEmpty(name.Trim()))
            {
                System.Windows.Forms.MessageBox.Show("文件名字不能为空，请输入");

                return false;
            }
            if (name.Contains("\\"))
            {
                System.Windows.Forms.MessageBox.Show("文件名字不能包含转义字符，请重新输入");
                return false;
            }
            if (templateId.Contains("\\"))
            {
                System.Windows.Forms.MessageBox.Show("文件切分模板id中不能包含转义字符，请重新输入");
                return false;
            }
            if (templateId.Contains("|"))
            {
                System.Windows.Forms.MessageBox.Show("文件切分模板id中不能包含|符号，请重新输入");
                return false;
            } 
            if (templateId.Contains(":"))
            {
                System.Windows.Forms.MessageBox.Show("文件切分模板id中不能包含:符号，请重新输入");
                return false;
            }
            if (desp.Contains("\\"))
            {
                System.Windows.Forms.MessageBox.Show("文件描述中不能包含转义字符，请重新输入");
                return false;
            }
            if (desp.Contains("|"))
            {
                System.Windows.Forms.MessageBox.Show("文件描述中不可以包含|符号，请重新输入");
                return false;
            }
            if (desp.Contains("："))
            {
                System.Windows.Forms.MessageBox.Show("文件描述中不可以包含：符号，请重新输入");
                return false;
            }

            return true;
        }


    }
}
