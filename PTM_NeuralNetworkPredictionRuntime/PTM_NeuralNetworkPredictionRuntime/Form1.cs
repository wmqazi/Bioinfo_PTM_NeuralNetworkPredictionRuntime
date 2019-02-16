using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Qazi.MachineLearningModels.NeuralNetworks;
using Qazi.Commons;
using Qazi.GUI.CommonDialogs;
using Qazi.Peptides;
using Qazi.DataPreprocessing;
using Qazi.MachineLearningModels.MatthewsCorrelationCoefficientLib;


namespace PTM_NeuralNetworkPredictionRuntime
{
    public partial class Form1 : Form
    {

        private NeuralNetwork ANN;
        private NeuralNetworkConfiguration nnc;
        private DataTable ValidationDataTable;
        private ValidationEngine validationEngine;
        private RuntimeEngine runtimeEngine;
        private StringBuilder strBuilder;
        private Dictionary<string, string> codeDictionary;
        public Form1()
        {
            InitializeComponent();
            strBuilder = new StringBuilder();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            Application.DoEvents();
            textBox1.Text = openFileDialog1.FileName;
            nnc = NeuralNetworkSerializationManager.Open(openFileDialog1.FileName);
            lblToplogy.Text = nnc.TopoloyString;
            lblLearningRate.Text = nnc.LearningRate.ToString();
            lblNumberOfConnections.Text = nnc.WeightConfiguration.Count.ToString();
        }

        void ANN_NeuralNetworkTaskProgress(object sender, Qazi.Common.WorkerProgressEventArg e)
        {
            SetMessage(e.UserState);
            progressBar1.Value = (int)e.ProgressPercentage;
            Application.DoEvents();
        }

        void ANN_NeuralNetworkTaskStarted(object sender, Qazi.Common.WorkerStartedEventArg e)
        {
            SetMessage(e.UserState);
            Application.DoEvents();
        }

        void ANN_NeuralNetworkTaskCompleted(object sender, Qazi.Common.WorkerCompletedEventArg e)
        {
            progressBar1.Value = 0;
           SetMessage(e.UserStateMessage);
           Application.DoEvents();
        }

        public void SetMessage(string msg)
        {
            strBuilder.Append(msg+Environment.NewLine);
            if (checkBox1.Checked == true)
            {
                //richTextBox1.AppendText(msg + Environment.NewLine);
                //richTextBox1.ScrollToCaret();
                richTextBox1.Text = msg;
                Application.DoEvents();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Run Validation
            CreateNeuralNetwork();
            List<string> listOfInputattribute = new List<string>();
            foreach (object attribute in checkedListBox1.CheckedItems)
            {
                listOfInputattribute.Add(attribute.ToString());
            }
            float predictionThreshold = float.Parse(txtPredictionThreshold.Text);
            float positiveLevel = float.Parse(txtPostiveClassificationLevel.Text);
            float negativeLevel = float.Parse(txtNegativeClassificationLevel.Text);
            validationEngine = new ValidationEngine(1, ANN, listOfInputattribute, comboBox1.Text, predictionThreshold, positiveLevel, negativeLevel);
            validationEngine._ValidationDataTable = ValidationDataTable;
            validationEngine.Run();
            lblMCC.Text = validationEngine.PerformanceEvaluation.MCC.ToString();
            lblAcc.Text = validationEngine.PerformanceEvaluation.Accuracy.ToString();
            lblSn.Text = validationEngine.PerformanceEvaluation.Sensitivity.ToString();
            lblSp.Text = validationEngine.PerformanceEvaluation.Specificity.ToString();
        }

        private void CreateNeuralNetwork()
        {
            ANN = new NeuralNetwork(1);
            ANN.NeuralNetworkTaskCompleted += new Qazi.Common.WorkerCompletedEventHandler(ANN_NeuralNetworkTaskCompleted);
            ANN.NeuralNetworkTaskStarted += new Qazi.Common.WorkerStartedWithStatusUpdateEventHandler(ANN_NeuralNetworkTaskStarted);
            ANN.NeuralNetworkTaskProgress += new Qazi.Common.WorkerProgressUpdateEventHandler(ANN_NeuralNetworkTaskProgress);
            ANN.CreateNetwork(nnc.TopoloyString, nnc.LearningRate);
            ANN.OverrideWeights(nnc.WeightConfiguration);
            richTextBox1.Text = "ANN Loaded...";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            openFileDialog2.ShowDialog();
        }

        private void openFileDialog2_FileOk(object sender, CancelEventArgs e)
        {
            DataSet ds = new DataSet();
            ds.ReadXml(openFileDialog2.FileName);
            DataTableSelectorWnd dtswnd = new DataTableSelectorWnd("Select DataTable", ds);
            dtswnd.ShowDialog();
            ValidationDataTable = ds.Tables[dtswnd.TableName];
            dataGridView1.DataSource = ValidationDataTable;
            lblValidationRecordCount.Text = ValidationDataTable.Rows.Count.ToString();
            int idex = 0;
            foreach (DataColumn col in ValidationDataTable.Columns)
            {
                checkedListBox1.Items.Add(col.ColumnName);
                checkedListBox1.SetItemCheckState(idex, CheckState.Checked);
                comboBox1.Items.Add(col.ColumnName);
                idex++;
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            LogViewerWnd lwnd = new LogViewerWnd(strBuilder.ToString());
            lwnd.Show(this);
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            //Perform Prediction
            CreateNeuralNetwork();
                        
            DataTable dt = new DataTable("Runtime Result");
            dt.Columns.Add("Pattern");
            dt.Columns.Add("Position");
            dt.Columns.Add("Prediction");

            DataTable peptideDataTable;
            List<string> listOfTargetResidue = new List<string>();
            listOfTargetResidue.Add(txtTargetResidues.Text);
            int sizeOfPeptide = int.Parse(textBox2.Text);
            peptideDataTable = ResidueBasedPeptideGenerator.ExtractPeptide(txtSequence.Text, listOfTargetResidue, sizeOfPeptide, false);

            List<string> listOfInputAttributesStringCollectionType = new List<string>();
            List<string> listOfInputAttributesListType = new List<string>();
            for (int index = (-1 * sizeOfPeptide); index <= sizeOfPeptide; index++)
            {
                listOfInputAttributesStringCollectionType.Add("P" + index.ToString());
                listOfInputAttributesListType.Add("P" + index.ToString());
            }

            DataTableEncodingManager encodingManager = new DataTableEncodingManager(codeDictionary, listOfInputAttributesListType, peptideDataTable, true, true);
            encodingManager.Run();
            DataTable encodedPeptideDataTable = encodingManager._EncodedDataTable;

            int binaryStringLenghtPepAminoAcid = 0;
            foreach (object key in codeDictionary.Keys)
            {
                if (codeDictionary[key.ToString()].Length > binaryStringLenghtPepAminoAcid)
                    binaryStringLenghtPepAminoAcid = codeDictionary[key.ToString()].Length;
            }

            listOfInputAttributesStringCollectionType = new List<string>();
            int ctr;
            for (int index = (-1 * sizeOfPeptide); index <= sizeOfPeptide; index++)
            {
                for (ctr = 1; ctr <= binaryStringLenghtPepAminoAcid; ctr++)
                {
                    listOfInputAttributesStringCollectionType.Add("P" + index.ToString() + "_" + ctr.ToString());
                }
            }

            runtimeEngine = new RuntimeEngine(1, ANN, listOfInputAttributesStringCollectionType);
            DataRow row;
            string pattern;
            
            float predictedValue;
            float patternIndex = 1;
            float totalPattern = encodedPeptideDataTable.Rows.Count;
            richTextBox1.Text = "Prediction Evaluation Started...";
            DataRow encodedPatternRrow;
            DataRow patternRow;
            dataGridView2.DataSource = encodedPeptideDataTable;
            for(patternIndex = 0; patternIndex < totalPattern; patternIndex++)
            {
                encodedPatternRrow = encodedPeptideDataTable.Rows[(int)patternIndex];
                patternRow = peptideDataTable.Rows[(int)patternIndex];
                pattern = "";
                for (ctr = 0; ctr < listOfInputAttributesListType.Count; ctr++)
                {
                    pattern = pattern + patternRow[listOfInputAttributesListType[ctr]].ToString();
                }
                //row
                runtimeEngine.PatternRow = encodedPatternRrow;
                predictedValue = runtimeEngine.Run();
                row = dt.NewRow();
                row["Pattern"] = pattern;
                row["Prediction"] = predictedValue.ToString();
                row["Position"] = patternRow["Position"].ToString();
                dt.Rows.Add(row);
                SetMessage(pattern + " => " + predictedValue.ToString());
                richTextBox1.Text = Convert.ToString((patternIndex+1))+ " - " + pattern + " => " + predictedValue.ToString() + Environment.NewLine;
            }
            dataGridView2.DataSource = dt;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            openFileDialog3.ShowDialog();
        }

        private void openFileDialog3_FileOk(object sender, CancelEventArgs e)
        {
            Qazi.BinaryFileIOManager.BinaryFileSerializationManager fileManager;
            fileManager = new Qazi.BinaryFileIOManager.BinaryFileSerializationManager();
            codeDictionary = (Dictionary<string, string>)fileManager.Open(openFileDialog3.FileName);
            textBox3.Text = openFileDialog3.FileName;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            txtSequence.Text = "";
            lblMCC.Text = "";
            lblAcc.Text = "";
            lblSn.Text = "";
            lblSp.Text = "";
            dataGridView2.DataSource = null;
        }
    }
}