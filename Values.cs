using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Sudoku
{
    [Serializable]
    public abstract class Values: ICloneable
    {
        private Int64 count=0;

        public Int64 Counter
        {
            get { return count; }
            set { count=value; }
        }

        public const byte Undefined=0;

        public abstract void SetValue(int row, int col, byte value, Boolean fixedValue);

        public abstract byte GetValue(int row, int col);

        public abstract Boolean FixedValue(int row, int col);

        public abstract Boolean ComputedValue(int row, int col);

        public abstract void Init();

        public object Clone()
        {
            MemoryStream memoryStream=new MemoryStream();
            BinaryFormatter binaryFormatter=new BinaryFormatter();

            binaryFormatter.Serialize(memoryStream, this);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return (Values)binaryFormatter.Deserialize(memoryStream);
        }
    }
}
