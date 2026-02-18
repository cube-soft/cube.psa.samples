using System.Windows.Forms;

namespace Cube.Psa.DesktopBridge
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            RootPanel = new TableLayoutPanel();
            DestinationPanel = new TableLayoutPanel();
            DestinationTextBox = new TextBox();
            DestinationButton = new Button();
            FooterPanel = new FlowLayoutPanel();
            SaveButton = new Button();
            RootPanel.SuspendLayout();
            DestinationPanel.SuspendLayout();
            FooterPanel.SuspendLayout();
            SuspendLayout();
            // 
            // RootPanel
            // 
            RootPanel.ColumnCount = 3;
            RootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            RootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            RootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            RootPanel.Controls.Add(DestinationPanel, 1, 1);
            RootPanel.Controls.Add(FooterPanel, 1, 3);
            RootPanel.Dock = DockStyle.Fill;
            RootPanel.Location = new System.Drawing.Point(0, 0);
            RootPanel.Name = "RootPanel";
            RootPanel.RowCount = 5;
            RootPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            RootPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            RootPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            RootPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            RootPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            RootPanel.Size = new System.Drawing.Size(784, 141);
            RootPanel.TabIndex = 0;
            // 
            // DestinationPanel
            // 
            DestinationPanel.ColumnCount = 2;
            DestinationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            DestinationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            DestinationPanel.Controls.Add(DestinationTextBox, 0, 0);
            DestinationPanel.Controls.Add(DestinationButton, 1, 0);
            DestinationPanel.Dock = DockStyle.Fill;
            DestinationPanel.Location = new System.Drawing.Point(23, 23);
            DestinationPanel.Name = "DestinationPanel";
            DestinationPanel.RowCount = 1;
            DestinationPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            DestinationPanel.Size = new System.Drawing.Size(738, 34);
            DestinationPanel.TabIndex = 0;
            // 
            // DestinationTextBox
            // 
            DestinationTextBox.Dock = DockStyle.Fill;
            DestinationTextBox.Font = new System.Drawing.Font("Yu Gothic UI", 11F);
            DestinationTextBox.Location = new System.Drawing.Point(3, 3);
            DestinationTextBox.Name = "DestinationTextBox";
            DestinationTextBox.Size = new System.Drawing.Size(652, 27);
            DestinationTextBox.TabIndex = 0;
            // 
            // DestinationButton
            // 
            DestinationButton.Dock = DockStyle.Fill;
            DestinationButton.Location = new System.Drawing.Point(661, 3);
            DestinationButton.Name = "DestinationButton";
            DestinationButton.Size = new System.Drawing.Size(74, 28);
            DestinationButton.TabIndex = 1;
            DestinationButton.Text = "...";
            DestinationButton.UseVisualStyleBackColor = true;
            // 
            // FooterPanel
            // 
            FooterPanel.Controls.Add(SaveButton);
            FooterPanel.Dock = DockStyle.Fill;
            FooterPanel.FlowDirection = FlowDirection.RightToLeft;
            FooterPanel.Location = new System.Drawing.Point(23, 74);
            FooterPanel.Name = "FooterPanel";
            FooterPanel.Size = new System.Drawing.Size(738, 44);
            FooterPanel.TabIndex = 1;
            // 
            // button1
            // 
            SaveButton.Location = new System.Drawing.Point(585, 3);
            SaveButton.Name = "button1";
            SaveButton.Size = new System.Drawing.Size(150, 36);
            SaveButton.TabIndex = 0;
            SaveButton.Text = "Save";
            SaveButton.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(784, 141);
            Controls.Add(RootPanel);
            Name = "MainForm";
            Text = "Cube PSA v4 Sample App";
            RootPanel.ResumeLayout(false);
            DestinationPanel.ResumeLayout(false);
            DestinationPanel.PerformLayout();
            FooterPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel RootPanel;
        private TableLayoutPanel DestinationPanel;
        private FlowLayoutPanel FooterPanel;
        private TextBox DestinationTextBox;
        private Button DestinationButton;
        private Button SaveButton;
    }
}
