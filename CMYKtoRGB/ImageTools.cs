using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;

namespace CMYKtoRGB
{
    public class ImageTools
    {
        public struct ConvertResult
        {
            public bool? generatedRGB; // generated / skipped / error
            public bool? generatedThumb;
            public Exception error;
        }

        protected static string[] m_extensions = { "jpg", "jpeg" };
        public static float ThumbScale = 0.35f;
        public const string OUTPUTFOLDER_RGB = "RGBOutput";
        public const string OUTPUTFOLDER_THUMB = "ThumbOutput";
        public static string Lower_OUTPUTFOLDER_RGB { get; private set; } = OUTPUTFOLDER_RGB.ToLower();
        public static string Lower_OUTPUTFOLDER_THUMB { get; private set; } = OUTPUTFOLDER_THUMB.ToLower();

        public static FileInfo[] ChooseFolder(out string folderPath, SearchOption searchOption = SearchOption.AllDirectories)
        {
            folderPath = FileHelpers.ChooseFolder();
            if(folderPath == null)
            {
                return null;
            }
            if (string.IsNullOrEmpty(folderPath))
            {
                return new FileInfo[0];
            }
            var dirInfo = new DirectoryInfo(folderPath);
            var result = m_extensions 
            .SelectMany(ext => dirInfo.GetFiles(string.Format("*.{0}", ext), searchOption))
            .Where(fi => !fi.DirectoryName.ToLower().Contains(Lower_OUTPUTFOLDER_RGB)
                        && !fi.DirectoryName.ToLower().Contains(Lower_OUTPUTFOLDER_THUMB))
            .ToArray();

            return result;
        }

        private static bool OutputPathCheck(FileInfo imageFI, string outputFolderName, out string outputFolderPath , out string outputFilePath)
        {
            outputFolderPath = string.Format("{0}/{1}", imageFI.DirectoryName, outputFolderName);
            outputFilePath = string.Format("{0}/{1}", outputFolderPath, imageFI.Name);
            FileHelpers.CreateDirectoryIfNotExists(outputFilePath);
            return File.Exists(outputFilePath);
        }


        public static ConvertResult ConvertToRGB(FileInfo imageFI, bool withThumbnail, bool overwrite = false)
        {
            ConvertResult result;
            result.error = null;
            result.generatedRGB = null;
            result.generatedThumb = null;

            string outputFolderPath;
            string outputFilePath;
            var outputFileExists = OutputPathCheck(imageFI, OUTPUTFOLDER_RGB, out outputFolderPath, out outputFilePath);
            MagickImage image;
            try
            {
                image = new MagickImage(imageFI);
            }
            catch (Exception ex)
            {
                result.error = ex;
                return result;
            }

            try
            {
                image.AddProfile(ColorProfile.SRGB);

                if (overwrite || !outputFileExists)
                {
                    image.Write(outputFilePath);
                    result.generatedRGB = true;
                }
                else
                {
                    result.generatedRGB = false; // Skipped
                }

            }
            catch (Exception ex)
            {
                image.Dispose();
                result.error = ex;
                // Cannot generate thumbnail because there is no RGB image to derive from
                return result;
            }
            try
            {
                if (withThumbnail)
                {
                    outputFileExists = OutputPathCheck(imageFI, OUTPUTFOLDER_THUMB, out outputFolderPath, out outputFilePath);
                    if (overwrite || !outputFileExists)
                    {
                        float newWidth = image.Width * ThumbScale;
                        float newHeight = image.Height * ThumbScale;
                        image.Resize((int)newWidth, (int)newHeight);
                        image.Write(outputFilePath);
                        result.generatedThumb = true;
                    }
                    else
                    {
                        result.generatedThumb = false;
                    }
                }
                else
                {
                    result.generatedThumb = false;
                }
            }
            catch (Exception ex)
            {
                result.error = ex;
            }
            finally
            {
                image.Dispose();
            }

            return result;
        }

    }
}
