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

        public static string FormatName(string name)
        {
            return Regex.Replace(name, @"\s+", "_");
        }

        public static string FindExtension(string folder, string extension)
        {
            string[] files = Directory.GetFiles(folder);
            foreach (string fileName in files)
            {
                if (Path.GetExtension(fileName) == extension)
                {
                    return Path.Combine(folder, fileName);
                }
            }
            string[] directories = Directory.GetDirectories(folder);
            foreach (string directory in directories)
            {
                string result = FindExtension(Path.Combine(folder, directory), extension);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
