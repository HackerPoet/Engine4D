using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WavUtil {
    public static bool ReadWav(string filename, out float[] data, out int sampleRate, out int channels) {
        data = null;
        sampleRate = 0;
        channels = 0;
        try {
            using (FileStream fs = File.Open(filename, FileMode.Open)) {
                BinaryReader reader = new BinaryReader(fs);

                // chunk 0
                int chunkID = reader.ReadInt32();
                int fileSize = reader.ReadInt32();
                int riffType = reader.ReadInt32();

                // chunk 1
                int fmtID = reader.ReadInt32();
                int fmtSize = reader.ReadInt32(); // bytes for this chunk (expect 16 or 18)
                while (fmtID != 0x20746D66) {
                    reader.ReadBytes(fmtSize);
                    fmtID = reader.ReadInt32();
                    fmtSize = reader.ReadInt32();
                }

                // format chunk
                int fmtCode = reader.ReadInt16();
                channels = reader.ReadInt16();
                sampleRate = reader.ReadInt32();
                int byteRate = reader.ReadInt32();
                int fmtBlockAlign = reader.ReadInt16();
                int bitDepth = reader.ReadInt16();

                if (fmtSize == 18) {
                    // Read any extra values
                    int fmtExtraSize = reader.ReadInt16();
                    reader.ReadBytes(fmtExtraSize);
                }

                // chunk 2
                int dataID = reader.ReadInt32();
                int bytes = reader.ReadInt32();
                while (dataID != 0x61746164) {
                    reader.ReadBytes(bytes);
                    dataID = reader.ReadInt32();
                    bytes = reader.ReadInt32();
                }

                // DATA!
                byte[] byteArray = reader.ReadBytes(bytes);
                int bytesForSamp = bitDepth / 8;
                int nValues = bytes / bytesForSamp;

                switch (bitDepth) {
                    case 64:
                        double[] asDouble = new double[nValues];
                        Buffer.BlockCopy(byteArray, 0, asDouble, 0, bytes);
                        data = Array.ConvertAll(asDouble, e => (float)e);
                        return true;
                    case 32:
                        data = new float[nValues];
                        Buffer.BlockCopy(byteArray, 0, data, 0, bytes);
                        return true;
                    case 16:
                        short[] asInt16 = new short[nValues];
                        Buffer.BlockCopy(byteArray, 0, asInt16, 0, bytes);
                        data = Array.ConvertAll(asInt16, e => e / (float)(short.MaxValue + 1));
                        return true;
                    default:
                        return false;
                }
            }
        } catch {
            Debug.Log("Failed to load wave file: " + filename);
            return false;
        }
    }

    public static bool WriteWav(string filename, float[] data, int sampleRate, int channels) {
        try {
            using (FileStream fs = File.Open(filename, FileMode.OpenOrCreate)) {
                BinaryWriter writer = new BinaryWriter(fs);
                int subChunk2Size = data.Length * 4;

                // chunk 0
                writer.Write(0x46464952);                //int32 chunkID
                writer.Write(36 + subChunk2Size);        //int32 fileSize
                writer.Write(0x45564157);                //int32 riffType

                // chunk 1
                writer.Write(0x20746D66);                //int32 fmtID
                writer.Write(16);                        //int32 fmtSize

                // format chunk
                writer.Write((ushort)3);                 //int16 fmtCode
                writer.Write((ushort)channels);          //int16 channels
                writer.Write(sampleRate);                //int32 sampleRate
                writer.Write(sampleRate * channels * 4); //int32 byteRate
                writer.Write((ushort)(channels * 4));    //int16 fmtBlockAlign
                writer.Write((ushort)32);                //int16 bitDepth

                // chunk 2
                writer.Write(0x61746164);                //int32 chunkID
                writer.Write(subChunk2Size);             //int32 chunkSize

                // data
                byte[] byteArray = new byte[subChunk2Size];
                Buffer.BlockCopy(data, 0, byteArray, 0, subChunk2Size);
                writer.Write(byteArray, 0, subChunk2Size);
                return true;
            }
        } catch {
            Debug.Log("Failed to save wave file: " + filename);
            return false;
        }
    }
}
