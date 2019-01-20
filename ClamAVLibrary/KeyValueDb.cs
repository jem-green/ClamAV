using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using System.IO;

namespace ClamAVLibrary
{
    class NoSQL
    {
        private string path = "";
        private string filename = "";
        private int recordLength = 0;
        private string filePath = "";
        private int records = 0;

        NoSQL(string path, string filename)
        {
            this.path = path;
            this.filename = filename;
            filePath = path + Path.DirectorySeparatorChar + filename;
        }

        int RecordLength
        {
            get
            {
                return (recordLength);
            }
            set
            {
                recordLength = value;
            }
        }

        int Count
        {
            get
            {
                return (records);
            }
        }

        public void Store(int index, byte[] value)
        {
            
            using (FileStream fileStream = File.Open(filePath, FileMode.OpenOrCreate))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    if (recordLength == 0)
                    {
                        recordLength = value.Length;
                    }
                    if(index > records)
                    {
                        records = index;
                    }
                    int offset = index * recordLength;
                    binaryWriter.Seek(offset, SeekOrigin.Begin);
                    binaryWriter.Write(value);
                }
            }
        }

        public byte[] Retrieve(int index)
        {
            byte[] value;
            if (index < records)
            {
                using (FileStream fileStream = File.Open(filePath, FileMode.OpenOrCreate))
                {
                    int offset = index * recordLength;
                    fileStream.Position = offset;
                    using (BinaryReader binaryReader = new BinaryReader(fileStream))
                    {
                        value = binaryReader.ReadBytes(recordLength);
                    }
                }
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
            return (value);
        }
    }
}
