﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SubpTool.Subp
{
    [XmlType("Entry")]
    public class SubpEntry
    {
        private const short MagicNumber = 0x4C01;

        public SubpEntry()
        {
            Lines = new List<SubpLine>();
        }

        [XmlAttribute("Id")]
        public uint SubtitleId { get; set; }

        [XmlAttribute("Priority")]
        public byte SubtitlePriority { get; set; }

        [XmlArray("Lines")]
        public List<SubpLine> Lines { get; set; }

        public static SubpEntry ReadSubpEntry(Stream input, Encoding encoding)
        {
            SubpEntry subpEntry = new SubpEntry();
            subpEntry.Read(input, encoding);
            return subpEntry;
        }

        private void Read(Stream input, Encoding encoding)
        {
            BinaryReader reader = new BinaryReader(input, encoding, true);
            short magicNumber = reader.ReadInt16();
            byte lineCount = reader.ReadByte();
            SubtitlePriority = reader.ReadByte();
            // TODO: Check if this is string length and (encoded) byte count.
            short stringLength1 = reader.ReadInt16();
            short stringLength2 = reader.ReadInt16();
            // TODO: Analyze what these values are used for
            short unknown3 = reader.ReadInt16();
            short flags = reader.ReadInt16();

            SubpTiming[] timings = new SubpTiming[lineCount];
            for (int i = 0; i < lineCount; i++)
            {
                timings[i] = SubpTiming.ReadSubpTiming(input);
            }

            byte[] data = reader.ReadBytes(stringLength1);
            string subtitles = encoding.GetString(data).TrimEnd('\0');


            // TODO: Check if $ can be escaped somehow
            // TODO: Check if Split('$').Count == lineCount
            string[] lines = subtitles.Split('$');
            for (int i = 0; i < lineCount; i++)
            {
                Lines.Add(new SubpLine(lines.Length > i ? lines[i] : "", timings[i]));
            }
        }

        public SubpIndex GetIndex(Stream outputStream)
        {
            return new SubpIndex
            {
                SubtitleId = SubtitleId,
                Offset = (uint) outputStream.Position
            };
        }

        private string GetJoinedSubtitleLines()
        {
            return string.Join("$", Lines.Select(l => l.Text));
        }

        public void Write(Stream outputStream, Encoding encoding)
        {
            BinaryWriter writer = new BinaryWriter(outputStream, encoding, true);
            writer.Write(MagicNumber);
            writer.Write((byte) Lines.Count);
            writer.Write(SubtitlePriority);

            string subtitles = GetJoinedSubtitleLines() + '\0';
            byte[] encodedData = encoding.GetBytes(subtitles);

            writer.Write((short) encodedData.Length);
            writer.Write((short) encodedData.Length);
            writer.Write((short) 0);
            writer.Write((short) 0);

            foreach (var line in Lines)
            {
                line.Timing.Write(outputStream);
            }
            writer.Write(encodedData);
        }
    }
}
