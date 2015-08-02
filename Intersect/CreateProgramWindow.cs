using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Controls;

namespace Intersect
{
    class CreateProgramWindow : ProgramWindow
    {
        public CreateProgramWindow()
        {
            program = new Program();
        }

        public void show(int pmID)
        {
            base.show();
            program.projectID = pmID;
            StringValidationRule rule = new StringValidationRule();
            rule.maxLength = Program.PRNAME_MAX_LENGTH;
            Ut.bind(program, "name", BindingMode.TwoWay, programNameTextBox, TextBox.TextProperty
                , new List<ValidationRule>() { rule });
        }

        public int save()
        {
            if (!checkUIElementValid())
            {
                Ut.M("请完整填写信息");
                return -1;
            }
            string validMsg = program.checkValid(new List<string>() { "id"});
            if (validMsg != "")
            {
                Ut.M(validMsg);
                return -1;
            }
            program.save();
            int prID = Program.GetLastProgramID();
            close();
            return prID;
        }
    }
}
