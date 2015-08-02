using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Intersect
{
    class ProgramWindow:UCWindow
    {
        public TextBox programNameTextBox
        { 
            get;
            set;
        }
        public Program program;

        protected bool checkUIElementValid()
        {
            StringValidationRule rule = new StringValidationRule();
            rule.maxLength = Program.PRNAME_MAX_LENGTH;
            ValidationResult result = rule.Validate(programNameTextBox.Text, null);
            if (!result.IsValid)
                return false;
            return true;
        }

        public void close(object sender, EventArgs e)
        {
            program = new Program();
            base.close();
        }
    }
}
