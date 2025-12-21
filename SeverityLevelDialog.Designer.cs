namespace Sudoku
{
    partial class SeverityLevelDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components=null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support-do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources=new System.ComponentModel.ComponentResourceManager(typeof(SeverityLevelDialog));
            this.groupBox1=new System.Windows.Forms.GroupBox();
            this.hard=new System.Windows.Forms.RadioButton();
            this.intermediate=new System.Windows.Forms.RadioButton();
            this.easy=new System.Windows.Forms.RadioButton();
            this.ok=new System.Windows.Forms.Button();
            this.cancel=new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.AccessibleDescription=null;
            this.groupBox1.AccessibleName=null;
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.BackgroundImage=null;
            this.groupBox1.Controls.Add(this.hard);
            this.groupBox1.Controls.Add(this.intermediate);
            this.groupBox1.Controls.Add(this.easy);
            this.groupBox1.Font=null;
            this.groupBox1.ForeColor=System.Drawing.SystemColors.ControlText;
            this.groupBox1.Name="groupBox1";
            this.groupBox1.TabStop=false;
            // 
            // hard
            // 
            this.hard.AccessibleDescription=null;
            this.hard.AccessibleName=null;
            resources.ApplyResources(this.hard, "hard");
            this.hard.BackgroundImage=null;
            this.hard.Font=null;
            this.hard.Name="hard";
            this.hard.TabStop=true;
            this.hard.UseVisualStyleBackColor=true;
            // 
            // intermediate
            // 
            this.intermediate.AccessibleDescription=null;
            this.intermediate.AccessibleName=null;
            resources.ApplyResources(this.intermediate, "intermediate");
            this.intermediate.BackgroundImage=null;
            this.intermediate.Font=null;
            this.intermediate.Name="intermediate";
            this.intermediate.TabStop=true;
            this.intermediate.UseVisualStyleBackColor=true;
            // 
            // easy
            // 
            this.easy.AccessibleDescription=null;
            this.easy.AccessibleName=null;
            resources.ApplyResources(this.easy, "easy");
            this.easy.BackgroundImage=null;
            this.easy.Font=null;
            this.easy.Name="easy";
            this.easy.TabStop=true;
            this.easy.UseVisualStyleBackColor=true;
            // 
            // ok
            // 
            this.ok.AccessibleDescription=null;
            this.ok.AccessibleName=null;
            resources.ApplyResources(this.ok, "ok");
            this.ok.BackgroundImage=null;
            this.ok.DialogResult=System.Windows.Forms.DialogResult.OK;
            this.ok.Font=null;
            this.ok.Name="ok";
            // 
            // cancel
            // 
            this.cancel.AccessibleDescription=null;
            this.cancel.AccessibleName=null;
            resources.ApplyResources(this.cancel, "cancel");
            this.cancel.BackgroundImage=null;
            this.cancel.DialogResult=System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Font=null;
            this.cancel.Name="cancel";
            // 
            // SeverityLevelDialog
            // 
            this.AcceptButton=this.ok;
            this.AccessibleDescription=null;
            this.AccessibleName=null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode=System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage=null;
            this.CancelButton=this.cancel;
            this.ControlBox=false;
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.ok);
            this.Controls.Add(this.groupBox1);
            this.Font=null;
            this.FormBorderStyle=System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon=null;
            this.MaximizeBox=false;
            this.MinimizeBox=false;
            this.Name="SeverityLevelDialog";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton hard;
        private System.Windows.Forms.RadioButton intermediate;
        private System.Windows.Forms.RadioButton easy;
        private System.Windows.Forms.Button ok;
        private System.Windows.Forms.Button cancel;
    }
}