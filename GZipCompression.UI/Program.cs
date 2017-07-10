using System;
using System.Diagnostics;
using System.IO;

namespace GZipCompression
{
    sealed class Program
    {
        static int Main(string[] args)
        {
            const string CommandNotFound = "Command not found";
            const string CompressMode = "compress";
            const string DecompressMode = "decompress";
            const string FileNotFound = "Could not find the specified file";
            const string WrongExtension = "The file extension does not match the file extension typically used for that file format";

            try
            {
#if DEBUG
                //args = new string[3];
                //args[0] = "compress";
                //args[1] = @"...";
                //args[2] = @"...";
                //args[0] = "decompress";
                //args[1] = @"...";
                //args[2] = @"...";
#endif
                #region Check arguments

                if (args == null)
                {
                    throw new Exception(CommandNotFound);
                }

                if (args.Length == 1)
                {
                    if (HelpRequired(args[0]) == true)
                    {
                        DisplayHelp();
                        return 0;
                    }
                }

                if (args.Length < 3)
                {
                    throw new Exception(CommandNotFound);
                }

                #endregion Check arguments

                var compressor = new Compressor();
                var stopWatch = new Stopwatch();

                compressor.HandleThreadExceptionEvent += (exception) =>
                {
                    Console.WriteLine($"Error occured: {exception.Message}\nSource: {exception.Source}");
                };

                if (args[0] == CompressMode)
                {
                    Console.WriteLine("Compressing...");

                    stopWatch.Start();
                    compressor.Compress(args[1], args[2]);
                    stopWatch.Stop();

                    Console.WriteLine($"Finished. Elapsed time: {stopWatch.Elapsed}");
                }
                else if (args[0] == DecompressMode)
                {
                    Console.WriteLine("Decompressing...");

                    stopWatch.Start();
                    compressor.Decompress(args[1], args[2]);
                    stopWatch.Stop();

                    Console.WriteLine($"Finished. Elapsed time: {stopWatch.Elapsed}");
                }
                else
                {
                    throw new Exception(CommandNotFound);
                }
            }
            catch (FileNotFoundException exception)
            {
                Console.WriteLine($"{FileNotFound}\nSource: {exception.Source}");
                return 1;
            }
            catch (FileFormatException exception)
            {
                Console.WriteLine($"{WrongExtension}\nSource: {exception.Source}");
                return 1;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error occured: {exception.Message}\nSource: {exception.Source}");
                return 1;
            }
            finally
            {
#if DEBUG
                Console.ReadKey();
#endif
            }

            return 0;
        }

        #region Private Methods

        /// <summary>
        /// Displays the help summary.
        /// </summary>
        private static void DisplayHelp()
        {
            Console.WriteLine(@"
A multithreaded compression program.
Written by Alexander Ivanov, email: dodecad@outlook.com
Command line parameters: mode[compress/decompress] sourcePath targetPath");
        }

        /// <summary>
        /// Checks whether the help screen is required.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <returns>A result of the operation.</returns>
        private static bool HelpRequired(string param)
        {
            return param == "-h" || param == "--help" || param == "/?";
        }

        #endregion Private Methods
    }
}