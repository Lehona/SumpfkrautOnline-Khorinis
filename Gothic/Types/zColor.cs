﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinApi;

namespace Gothic.Types
{
    public class zColor : zClass, IDisposable
    {
        public const int ByteSize = 4;

        public zColor()
        {

        }

        public zColor(int address)
            : base(address)
        {

        }

        public static zColor Create(byte r, byte g, byte b, byte a)
        {
            int ptr = Process.Alloc(ByteSize).ToInt32();
            int arr = BitConverter.ToInt32(new byte[] { b, g, r, a }, 0);
            Process.THISCALL<NullReturnCall>(ptr, 0x00401F20, new IntArg(arr));

            return new zColor(ptr);
        }

        public byte B
        {
            get { return Process.ReadByte(Address + 0); }
            set { Process.Write(Address + 0, (byte)value); }
        }

        public byte G
        {
            get { return Process.ReadByte(Address + 1); }
            set { Process.Write(Address + 1, (byte)value); }
        }

        public byte R
        {
            get { return Process.ReadByte(Address + 2); }
            set { Process.Write(Address + 2, (byte)value); }
        }

        public byte A
        {
            get { return Process.ReadByte(Address + 3); }
            set { Process.Write(Address + 3, (byte)value); }
        }

        public void Set(uint color)
        {
            Process.Write(Address, color);
        }

        public override uint ValueLength()
        {
            return 4;
        }

        private bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                Process.Free(new IntPtr(Address), ByteSize);
                disposed = true;
            }
        }

        public override string ToString()
        {
            return string.Format("zColor({0} {1} {2} {3}", R, G, B, A);
        }
    }
}
