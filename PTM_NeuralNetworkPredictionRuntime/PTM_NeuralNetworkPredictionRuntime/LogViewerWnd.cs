using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PTM_NeuralNetworkPredictionRuntime
{
    public partial class LogViewerWnd : Form
    {
        public LogViewerWnd(string log)
        {
            InitializeComponent();
            richTextBox1.Text = log;
        }
    }
}