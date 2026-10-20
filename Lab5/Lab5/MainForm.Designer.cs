namespace Lab5
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
            tabControl = new TabControl();
            tabTask1 = new TabPage();
            tabTask2 = new TabPage();
            tabTask3 = new TabPage();
            tabControl.SuspendLayout();
            SuspendLayout();
            //
            // tabControl
            //
            tabControl.Controls.Add(tabTask1);
            tabControl.Controls.Add(tabTask2);
            tabControl.Controls.Add(tabTask3);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1100, 700);
            tabControl.TabIndex = 0;
            //
            // tabTask1
            //
            tabTask1.Location = new Point(4, 29);
            tabTask1.Name = "tabTask1";
            tabTask1.Size = new Size(1092, 667);
            tabTask1.TabIndex = 0;
            tabTask1.Text = "1. L-системы";
            tabTask1.UseVisualStyleBackColor = true;
            //
            // tabTask2
            //
            tabTask2.Location = new Point(4, 29);
            tabTask2.Name = "tabTask2";
            tabTask2.Size = new Size(1092, 667);
            tabTask2.TabIndex = 1;
            tabTask2.Text = "2. Midpoint displacement";
            tabTask2.UseVisualStyleBackColor = true;
            //
            // tabTask3
            //
            tabTask3.Location = new Point(4, 29);
            tabTask3.Name = "tabTask3";
            tabTask3.Size = new Size(1092, 667);
            tabTask3.TabIndex = 2;
            tabTask3.Text = "3. Кривые Безье";
            tabTask3.UseVisualStyleBackColor = true;
            //
            // MainForm
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 700);
            Controls.Add(tabControl);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Лабораторная работа №5";
            tabControl.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControl;
        private TabPage tabTask1;
        private TabPage tabTask2;
        private TabPage tabTask3;
    }
}
