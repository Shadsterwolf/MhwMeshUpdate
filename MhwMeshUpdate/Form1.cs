using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MhwMeshUpdate
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog oFile = new OpenFileDialog();
            if (!(oFile.ShowDialog() == DialogResult.Cancel))
            {
                tbxModFile.Text = oFile.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog oFile = new OpenFileDialog();
            if (!(oFile.ShowDialog() == DialogResult.Cancel))
            {
                tbxMeshFile.Text = oFile.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (tbxModFile.Text != "" && tbxMeshFile.Text != "" )
            {
                byte[] modFile, meshFile;
                try
                {
                    modFile = System.IO.File.ReadAllBytes(tbxModFile.Text);
                    meshFile = System.IO.File.ReadAllBytes(tbxMeshFile.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                    return;
                }
                uint vertexCount = GetVertexCount(modFile);
                //MessageBox.Show("vertexCount: " + vertexCount);
                uint meshCount = (uint)meshFile.Length / 16;
                //MessageBox.Show("meshCount: " + meshCount);
                if (vertexCount != meshCount)
                {
                    MessageBox.Show("Error: Mod Vertex and Mesh count do not match!" + "\n\n" + "vertexCount: " + vertexCount + "\n" + "meshCount: " + meshCount);
                }

                uint vertexOffset = GetVertexOffset(modFile);
                //MessageBox.Show("vertexOffset: " + vertexOffset);
                UpdateMeshData(tbxModFile.Text, vertexOffset, vertexCount, meshFile);
                MessageBox.Show("Done!");
            }
            else
            {
                MessageBox.Show("Need both files before updating!");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (tbxModFile.Text != "" && tbxMeshFile.Text != "")
            {
                byte[] modFile, meshFile;
                try
                {
                    modFile = System.IO.File.ReadAllBytes(tbxModFile.Text);
                    meshFile = System.IO.File.ReadAllBytes(tbxMeshFile.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                    return;
                }
                uint meshCount = (uint)meshFile.Length / 16;
                UpdateMeshData(tbxModFile.Text, (uint)int.Parse(tbxOffset.Text, System.Globalization.NumberStyles.HexNumber), (uint)int.Parse(tbxHops.Text), meshCount, meshFile);
                MessageBox.Show("Done!");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (tbxModFile.Text != "")
            {
                byte[] modFile;
                try
                {
                    modFile = System.IO.File.ReadAllBytes(tbxModFile.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                    return;
                }
                uint objectCount = ReadByte(modFile, 0x08);
                //MessageBox.Show(objectCount.ToString());
                uint objectOffset = ReadLong(modFile, 0x48, true);
                UpdateLodLowPoly(tbxModFile.Text, objectOffset, 0x50, objectCount);
                MessageBox.Show("Done!");
            }
        }

        public static Boolean IsModFile(byte[] modFile)
        {
            return ReadLong(modFile, 0) == 1297040384;
        }

        public static uint GetVertexCount(byte[] modFile)
        {
            if (IsModFile(modFile))
                return ReadLong(modFile, 0x0C, true);
            else
                return 0;
        }

        public static uint GetVertexOffset(byte[] modFile)
        {
            return ReadLong(modFile, 0x50, true);
        }

        public static void UpdateMeshData(string file, uint pos, uint vertexCount, byte[] meshFile)
        {
            UpdateMeshData(file, pos, 24, vertexCount, meshFile);
        }

        public static void UpdateMeshData(string file, uint pos, uint hops, uint vertexCount, byte[] meshFile)
        {
            byte[] modFile = System.IO.File.ReadAllBytes(file);
            for (int i = 0; i < vertexCount; i++)
            {
                for(int j = 0; j < 16; j++)
                {
                    modFile[pos + j] = meshFile[(i * 0x10) + j];
                }
                pos += hops;
            }

            System.IO.StreamWriter stream = new System.IO.StreamWriter(file); //StreamWriter doesn't like to be used to overwrite data :(
            stream.BaseStream.Write(modFile, 0, modFile.Length); //Just write the updated byte array
            stream.Close(); //Save
            stream.Dispose(); //End stream
        }

        public static void UpdateLodLowPoly(string file, uint pos, uint hops, uint objectCount)
        {
            byte[] modFile = System.IO.File.ReadAllBytes(file);

            for (int i = 0; i < objectCount; i++)
            {
                uint lod = ReadLong(modFile, pos + 8, true);
                //MessageBox.Show(lod.ToString());
                if (lod >= 1 && lod <= 16)
                {
                    modFile[pos] = 0;
                    modFile[pos+1] = 0;
                    modFile[pos+2] = 0;
                    modFile[pos+3] = 0;
                    modFile[pos+8] = 0xFF;
                    modFile[pos+9] = 0xFF;
                }
                else if (lod >= 17)
                {
                    modFile[pos] = 0;
                    modFile[pos + 1] = 0;
                    modFile[pos + 8] = 1;
                    modFile[pos + 9] = 0;
                }
                pos += hops;
            }
           
            System.IO.StreamWriter stream = new System.IO.StreamWriter(file); //StreamWriter doesn't like to be used to overwrite data :(
            stream.BaseStream.Write(modFile, 0, modFile.Length); //Just write the updated byte array
            stream.Close(); //Save
            stream.Dispose(); //End stream
        }

        private static ushort Makeu16(byte b1, byte b2) //16-bit change (ushort, 0xFFFF)
        {
            return (ushort)(((ushort)b1 << 8) | (ushort)b2);
        }

        private static uint Makeu32(byte b1, byte b2, byte b3, byte b4) //32-bit change (uint, 0xFFFFFFFF)
        {
            return ((uint)b1 << 24) | ((uint)b2 << 16) | ((uint)b3 << 8) | (uint)b4;
        }

        private static byte[] Breaku16(ushort u16) //Byte change from 16-bits (byte, 0xFF, 0xFF)
        {
            return new byte[] { (byte)(u16 >> 8), (byte)(u16 & 0xFF) };
        }

        private static byte[] Breaku32(uint u32) //Byte change from 32-bits (byte, 0xFF, 0xFF, 0xFF, 0xFF)
        {
            return new byte[] { (byte)(u32 >> 24), (byte)((u32 >> 16) & 0xFF), (byte)((u32 >> 8) & 0xFF), (byte)(u32 & 0xFF) };
        }

        public static byte ReadByte(byte[] modFile, uint pos)
        {
            return modFile[pos];
        }

        public static uint ReadLong(byte[] modFile, uint pos)
        {
            return ReadLong(modFile, pos, false);
        }
        public static uint ReadLong(byte[] modFile, uint pos, Boolean swapEndian)
        {
            if (swapEndian)
                return SwapLongEndian(Makeu32(modFile[pos], modFile[pos + 1], modFile[pos + 2], modFile[pos + 3]));
            else
                return Makeu32(modFile[pos], modFile[pos + 1], modFile[pos + 2], modFile[pos + 3]);
        }

        public static uint SwapLongEndian(uint value)
        {
            var b1 = (value >> 0) & 0xff;
            var b2 = (value >> 8) & 0xff;
            var b3 = (value >> 16) & 0xff;
            var b4 = (value >> 24) & 0xff;

            return b1 << 24 | b2 << 16 | b3 << 8 | b4 << 0;
        }

        public static uint SwapShortEndian(uint value)
        {
            var b1 = (value >> 0) & 0xff;
            var b2 = (value >> 8) & 0xff;

            return  b1 << 8 | b2 << 0;
        }
    }
}
