using System;
using System.Collections.Generic;
using System.IO;
public class FileOperations
{
    public static void CopyDir(string source, string destination, bool overwrite = false)
    {
        if (!Directory.Exists(source))
        {
            Logger.Error($"Path {source} does not exist.");
            return;
        }

        if (!Directory.Exists(destination)) { Directory.CreateDirectory(destination); }

        string[] files = Directory.GetFiles(source);

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string destinationFile = Path.Combine(destination, fileName);
            File.Copy(file, destinationFile, overwrite);
        }

        string[] dirs = Directory.GetDirectories(source);

        foreach (string dir in dirs)
        {
            string dirName = Path.GetFileName(dir);
            string destinationDir = Path.Combine(destination, dirName);
            CopyDir(dir, destinationDir, overwrite);            
        }
    }
}