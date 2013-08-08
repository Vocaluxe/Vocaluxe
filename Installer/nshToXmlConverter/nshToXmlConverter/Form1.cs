using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace nshToXmlConverter
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            List<string> filesList = new List<string>();
            filesList.AddRange(files);
            foreach (string file in filesList)
            {
                string extension = Path.GetExtension(file).ToLower();

                switch (extension)
                {
                    case ".xml":
                        if (!CConverter.ConvertXmlToNsh(file))
                            MessageBox.Show("Can't convert " + Path.GetFileName(file) + " to nsh.");
                        break;

                    case ".nsh":
                        if(!CConverter.ConvertNshToXml(file))
                            MessageBox.Show("Can't convert " + Path.GetFileName(file) + " to xml.");
                        break;
                }
            }
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
            {
                e.Effect = DragDropEffects.All;
            }
        }
    }
}
