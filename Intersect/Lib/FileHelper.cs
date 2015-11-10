using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Intersect.Lib
{
    public class FileHelper
    {
        //在目录下，有a.cs，a.css，将统一删除。
        public static void DeleteSameNameFiles(string folder, string name)
        {
            name = Regex.Replace(name, @"\.[^.]$", "");
            string[] files = Directory.GetFiles(folder);
            foreach (string file in files)
            {
                string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(file);
                if (fileNameWithoutExtension == name)
                {
                    File.Delete(file);
                }
            }
        }

    }
}
