using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Controls;

namespace Intersect
{
    class ModifyProgramWindow : ProgramWindow
    {
        public ModifyProgramWindow()
        {
            program = new Program();
        }

        public void show(int programID, int pmID)
        {
            base.show();
            program.id = programID;
            program.projectID = pmID;
            program.select();
            StringValidationRule rule = new StringValidationRule();
            rule.maxLength = Program.PRNAME_MAX_LENGTH;
            Tool.bind(program, "name", BindingMode.TwoWay, programNameTextBox, TextBox.TextProperty
                , new List<ValidationRule>() { rule });
        }

        public int update()
        {
            if (!checkUIElementValid())
            {
                Tool.M("请完整填写信息");
                return -1;
            }
            string validMsg = program.checkValid();
            if (validMsg != "")
            {
                Tool.M(validMsg);
                return -1;
            }
            program.update();
            close();
            return program.id;
        }
    }
}
