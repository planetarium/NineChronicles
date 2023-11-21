using System.IO;

namespace Airbridge.Editor {
    class Utils {
        public static void ReplaceDirectory(string source, string destination) {
            if (!Directory.Exists(source)) 
            {
                return;
            }
            if (!Directory.Exists(destination)) 
            {
                Directory.CreateDirectory(destination);
            }

            string[] files = Directory.GetFiles(source);
            string[] directories = Directory.GetDirectories(source);

            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string destinationFile = Path.Combine(destination, name);
                File.Copy(file, destinationFile, true);
            }

            foreach (string directory in directories)
            {
                string name = Path.GetDirectoryName(directory);
                string destinationDirectory = Path.Combine(destination, name);
                ReplaceDirectory(directory, destinationDirectory);
            }
        }
    }
}