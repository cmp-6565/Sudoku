namespace Sudoku
{
    partial class Comment
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
            System.ComponentModel.ComponentResourceManager resources=new System.ComponentModel.ComponentResourceManager(typeof(Comment));
            this.ok=new System.Windows.Forms.Button();
            this.cancel=new System.Windows.Forms.Button();
            this.label1=new System.Windows.Forms.Label();
            this.commentTextBox=new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // ok
            // 
            this.ok.DialogResult=System.Windows.Forms.DialogResult.OK;
            resources.ApplyResources(this.ok, "ok");
            this.ok.Name="ok";
            this.ok.UseVisualStyleBackColor=true;
            // 
            // cancel
            // 
            this.cancel.DialogResult=System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.cancel, "cancel");
            this.cancel.Name="cancel";
            this.cancel.UseVisualStyleBackColor=true;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name="label1";
            // 
            // commentTextBox
            // 
            resources.ApplyResources(this.commentTextBox, "commentTextBox");
            this.commentTextBox.Name="commentTextBox";
            // 
            // Comment
            // 
            this.AcceptButton=this.ok;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode=System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton=this.cancel;
            this.ControlBox=false;
            this.Controls.Add(this.commentTextBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.ok);
            this.MaximizeBox=false;
            this.MinimizeBox=false;
            this.Name="Comment";
            this.SizeGripStyle=System.Windows.Forms.SizeGripStyle.Hide;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ok;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox commentTextBox;
    }
}