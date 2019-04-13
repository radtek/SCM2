using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Swift
{
    // 可读缓冲区
    public interface IReadableBuffer
    {
        bool ReadFromNetOrder
        {
            get;
            set;
        }
        int Available
        {
            get;
        }
        void Skip(int cnt);
        bool PeekBool(ref bool v);
        bool PeekInt(ref int v);
        bool PeekInt(int offset, ref int v);
        byte[] PeekBytes(int cnt);
        byte ReadByte();
        byte[] ReadBytes(int cnt);
        bool ReadBool();
        bool[] ReadBoolArr();
        short ReadShort();
        short[] ReadShortArr();
        int ReadInt();
        int[] ReadIntArr();
        long ReadLong();
		long[] ReadLongArr();
		ulong ReadULong();
		ulong[] ReadULongArr();
        float ReadFloat();
        float[] ReadFloatArr();
        double ReadDouble();
        double[] ReadDoubleArr();
        char ReadChar();
        char[] ReadCharArr();
        string ReadString();
        string[] ReadStringArr();
        void TravelReplaceBytes4Read(int offsetFromReadPos, int len, Func<byte, byte> fun);

    }

    // 可写缓冲区
    public interface IWriteableBuffer
    {
        bool WriteToNetOrder
        {
            get;
            set;
        }
        int Available
        {
            get;
        }
        void Write(byte[] src, int offset, int cnt);
        void Write(byte[] src);
        void Write(byte v);
        void Write(bool v);
        void Write(bool[] v);
        void Write(short v);
        void Write(short[] v);
        void Write(int v);
        void Write(int[] v);
        void Write(long v);
		void Write(long[] v);
		void Write(ulong v);
		void Write(ulong[] v);
        void Write(float v);
        void Write(float[] v);
        void Write(double v);
        void Write(double[] v);
        void Write(char v);
        void Write(char[] v);
        void Write(string v);
        void Write(string[] v);
    }

    // 可保留空间的缓冲区
    public interface IReservableBuffer
    {
        int Reserve(int cnt);
        void Unreserve(int id, byte[] src);
    }
}
