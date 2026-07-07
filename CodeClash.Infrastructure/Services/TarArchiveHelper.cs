using System;
using System.IO;
using System.Text;

namespace CodeClash.Infrastructure.Services;

public static class TarArchiveHelper
{
    public static Stream CreateTarStream(params (string FileName, string Content)[] files)
    {
        var memStream = new MemoryStream();
        foreach (var file in files)
        {
            WriteFileToTar(memStream, file.FileName, file.Content);
        }
        
        // Write two 512-byte blocks of zeros to signify end of archive
        byte[] emptyBlock = new byte[1024];
        memStream.Write(emptyBlock, 0, emptyBlock.Length);
        
        memStream.Position = 0;
        return memStream;
    }

    private static void WriteFileToTar(Stream stream, string fileName, string content)
    {
        byte[] contentBytes = Encoding.UTF8.GetBytes(content);
        byte[] header = new byte[512];

        // 1. File name (offset 0, length 100)
        byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
        Array.Copy(nameBytes, 0, header, 0, Math.Min(nameBytes.Length, 99));

        // 2. File mode (offset 100, length 8) - "0000755\0" for permissions (executable)
        byte[] modeBytes = Encoding.UTF8.GetBytes("0000755\0");
        Array.Copy(modeBytes, 0, header, 100, modeBytes.Length);

        // 3. Owner UID (offset 108, length 8)
        byte[] uidBytes = Encoding.UTF8.GetBytes("0000000\0");
        Array.Copy(uidBytes, 0, header, 108, uidBytes.Length);

        // 4. Owner GID (offset 116, length 8)
        byte[] gidBytes = Encoding.UTF8.GetBytes("0000000\0");
        Array.Copy(gidBytes, 0, header, 116, gidBytes.Length);

        // 5. File size (offset 124, length 12) - octal string
        string sizeString = Convert.ToString(contentBytes.Length, 8).PadLeft(11, '0') + "\0";
        byte[] sizeBytes = Encoding.UTF8.GetBytes(sizeString);
        Array.Copy(sizeBytes, 0, header, 124, sizeBytes.Length);

        // 6. Modification time (offset 136, length 12) - octal string representing Unix time
        long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string mtimeString = Convert.ToString(unixTime, 8).PadLeft(11, '0') + "\0";
        byte[] mtimeBytes = Encoding.UTF8.GetBytes(mtimeString);
        Array.Copy(mtimeBytes, 0, header, 136, mtimeBytes.Length);

        // 7. Checksum (offset 148, length 8) - spaces
        byte[] checksumSpaceBytes = Encoding.UTF8.GetBytes("        ");
        Array.Copy(checksumSpaceBytes, 0, header, 148, 8);

        // 8. Type flag (offset 156, length 1) - '0' for normal file
        header[156] = (byte)'0';

        // Magic "ustar" (offset 257, length 6)
        byte[] magicBytes = Encoding.UTF8.GetBytes("ustar\0");
        Array.Copy(magicBytes, 0, header, 257, magicBytes.Length);

        // Calculate checksum
        long checksum = 0;
        for (int i = 0; i < 512; i++)
        {
            checksum += header[i];
        }

        // Write checksum (offset 148, length 6 octal digits + null byte + space)
        string checksumString = Convert.ToString(checksum, 8).PadLeft(6, '0') + "\0 ";
        byte[] checksumBytes = Encoding.UTF8.GetBytes(checksumString);
        Array.Copy(checksumBytes, 0, header, 148, checksumBytes.Length);

        // Write header and file content
        stream.Write(header, 0, header.Length);
        stream.Write(contentBytes, 0, contentBytes.Length);

        // Align file content to 512-byte blocks
        int paddingLength = 512 - (contentBytes.Length % 512);
        if (paddingLength < 512)
        {
            byte[] padding = new byte[paddingLength];
            stream.Write(padding, 0, padding.Length);
        }
    }
}
