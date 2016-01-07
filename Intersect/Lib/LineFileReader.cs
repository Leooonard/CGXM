using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Intersect.Lib
{
    class LineFileReader
    {
        private FileStream fileStream;
        private StreamReader streamReader;

        public LineFileReader(string filePath)
        {
            fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            streamReader = new StreamReader(fileStream, Encoding.Default);
        }

        public List<string> readByLine()
        {
            List<string> lines = new List<string>();
            string content = readLine();
            while (lineValid(content))
            {
                lines.Add(content);
                content = readLine();
            }
            return lines;
        }

        public void close()
        {
            fileStream.Close();
            streamReader.Close();
        }

        private void seekToBegin()
        {
            fileStream.Seek(0, SeekOrigin.Begin);
        }

        private string readLine()
        {
            return streamReader.ReadLine();
        }

        private bool lineValid(string line)
        {
            if (line == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
