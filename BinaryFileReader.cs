using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PNGHandler
{
   class BinaryFileReader
   {
      // Testing mainframe
      public static void Main(String[] args)
      {
         if (args.Length != 1)
         {
            StringBuilder error_message = new StringBuilder("Invalid use:");
            error_message.Append(String.Format("\targ #{0}: {1}", 1, "file_name"));

            throw new Exception(error_message.ToString());
         }

         BinaryFileReader reader = new BinaryFileReader(args[0], System.Environment.CurrentDirectory);
         //reader.display();

         PNG_Decoder decoder = new PNG_Decoder(reader.file_data);
         byte[] greyscale_bytes = decoder.to_greyscale();

         /*
         Console.WriteLine("The converted bytes to greyscale are:");
         Console.Write("[{0}", greyscale_bytes[0]);
         for (int i = 1; i < greyscale_bytes.Length; i++)
         {
            Console.Write(", ");
            if (i % 5 == 0)
            {
               Console.WriteLine();
            }
            Console.Write(greyscale_bytes[i]);
         }
         Console.WriteLine("]");
          * */

         Console.WriteLine();
         Console.WriteLine();

         string[] ASCII_lines = ASCII_Converter.convert_to_ASCII_art(greyscale_bytes, decoder.height, decoder.width);

         // make a text file
         StringBuilder txt_path_builder = new StringBuilder(reader.filepath.Remove(reader.filepath.LastIndexOf(".")));
         txt_path_builder.Append(".txt");

         ASCII_Converter.to_txt(ASCII_lines, txt_path_builder.ToString());
      }

      // Constructor
      public BinaryFileReader(string filename, string path)
      {
         // guard conditions
         if (filename.Length < 1
            || path.Length < 1)
         {
            throw new System.ArgumentException("BinaryFileReader: Invalid constructor arguments.");
         }

         set_filepath(filename, path);
         // open the png file
         long stream_length;
         filestream = open_file(ref filepath_p, out stream_length);
         // extract the bytes from the file
         //file_data_p = extract_bytes(stream_length);
      }

            private void set_filepath(string filename, string path)
            {
               // cleans up extra backspaces and adds trailing backspace
               path = to_formal_path(path);
               filepath_p = create_full_path(filename, path);
            }

                  private string to_formal_path(string path)
                  {
                     // parse and clean directory pathway
                     string[] path_components = path.Split('\\');
                     int index = 0;
                     while (path_components[index].Length == 0)
                     {
                        ++index;
                     }
                     StringBuilder new_path = new StringBuilder(path_components[index++]);

                     for (; index < path_components.Length; index++)
                     {
                        if (path_components[index].Length > 0)
                        {
                           new_path.Append("\\");
                           new_path.Append(path_components[index]);
                        }
                     }

                     new_path.Append("\\");

                     return new_path.ToString();
                  }

                  private string create_full_path(string filename, string path)
                  {
                     // guard conditions
                     if (filename == null
                        || path == null)
                     {
                        throw new System.ArgumentNullException("PNGBinaryFileReader: Null argument passed when constructing full file path.");
                     }

                     bool invalid_arguments = filename.Length == 0
                        || path.Length < 2
                        || !filename.Contains(".")
                        || !path.Contains("\\");
                     if (invalid_arguments)
                     {
                        throw new System.ArgumentException("BinaryFileReader: Invalid arguments, unable to construct full file path.");
                     }

                     StringBuilder full_path = new StringBuilder(path);
                     
                     // add the file name to the path
                     full_path.Append(filename);

                     return full_path.ToString();
                  }

            private System.IO.BinaryReader open_file(ref string full_path, out long stream_length)
            {
               System.IO.BinaryReader stream = null;
               int original_path_length = full_path.LastIndexOf('\\');
               string original_path = full_path.Substring(0, original_path_length + 1);
               string original_filename = full_path.Substring(original_path_length + 1);
               stream_length = 0;

               // continue trying to open the file
               while (true)
               {
                  try
                  {
                     stream = attempt_to_open_target_file(full_path, out stream_length);
                     break;
                  }
                  catch (ArgumentException)
                  {
                     string new_filename = original_filename;
                     string new_path = original_path;

                     if (full_path.Length <= 0)
                     {
                        new_filename = query_new_filename();
                        if (new_filename == null)
                        {
                           return null;
                        }
                        new_path = query_new_path();
                        if (new_path == null)
                        {
                           return null;
                        }
                     }
                     else
                     {
                        if (!full_path.Contains('.'))
                        {
                           new_filename = query_new_filename();
                           if (new_filename == null)
                           {
                              return null;
                           }
                        }
                        if (!full_path.Contains('\\'))
                        {
                           new_path = query_new_path();
                           if (new_path == null)
                           {
                              return null;
                           }
                        }
                     }

                     full_path = create_full_path(new_filename, new_path);
                     continue;
                  }
                  catch (System.IO.IOException ioexcep)
                  {
                     if (ioexcep is System.IO.DirectoryNotFoundException)
                     {
                        string new_path = query_new_path();
                        if (new_path == null)
                        {
                           return null;
                        }
                        full_path = create_full_path(original_filename, new_path);
                     }
                     else if (ioexcep is System.IO.FileNotFoundException)
                     {
                        string new_filename = query_new_filename();
                        if (new_filename == null)
                        {
                           return null;
                        }
                        full_path = create_full_path(new_filename, original_path);
                     }
                     continue;
                  }
               }

               return stream;
            }

                  /// <summary>
                  /// Opens the target file specified in full_path.
                  /// </summary>
                  /// <exception cref="System.ArgumentNullException">Null string encountered when trying to open file.</exception>
                  /// <exception cref="System.ArgumentException">Invalid arguments passed to file opening mechanism.</exception>
                  /// <exception cref="System.IO.IOException">IO error.</exception>
                  /// <exception cref="System.IO.DirectoryNotFoundException">Cannot find target directory.</exception>
                  /// <exception cref="System.IO.FileNotFoundException">Cannot find target file.</exception>
                  /// <param name="full_file_path">A string specifying the directory, filename, and file extension of the file to open.</param>
                  /// <returns>A StreamReader object pointing to the target file.</returns>
                  private System.IO.BinaryReader attempt_to_open_target_file(string full_file_path, out long stream_length)
                  {
                     stream_length = 0;

                     // guard conditions
                     if (full_file_path == null)
                     {
                        throw new ArgumentNullException("BinaryFileReader: Null string encountered when trying to open file.");
                     }
                     bool invalid_arguments = full_file_path.Length <= 0
                        || !full_file_path.Contains(".")
                        || !full_file_path.Contains("\\");

                     if (invalid_arguments)
                     {
                        throw new ArgumentException("BinaryFileReader: Invalid arguments passed to file opening mechanism.");
                     }

                     System.IO.BinaryReader filestream = null;
                     try
                     {
                        // ERROR TESTING
                        // get the stream's length since binaryreader has no method of retrieving it
                        //System.IO.FileStream temp = new System.IO.FileStream(full_file_path, System.IO.FileMode.Open);
                        //stream_length = temp.Length;
                        //temp.Close();

                        // gather the bytes from the file
                        file_data_p = System.IO.File.ReadAllBytes(full_file_path);

                        // assign the filestream
                        filestream = new System.IO.BinaryReader(System.IO.File.OpenRead(full_file_path));
                        if (filestream == null)
                        {
                           throw new System.IO.IOException("BinaryFileReader: IO error");
                        }
                     }
                     catch (System.IO.DirectoryNotFoundException)
                     {
                        throw new System.IO.DirectoryNotFoundException("PNGBinaryFileReader: Cannot find target directory.");
                     }
                     catch (System.IO.FileNotFoundException)
                     {
                        throw new System.IO.FileNotFoundException("PNGBinaryFileReader: Cannot find target file.");
                     }

                     return filestream;
                  }

                  /// <summary>
                  /// Queries the console for a new filename to use as the target file's name.
                  /// </summary>
                  /// <returns>A string representing the new filename to use as the target file's name. Null returned if user wants to abort.</returns>
                  private string query_new_filename()
                  {
                     Console.WriteLine("Invalid filename.");
                     Console.WriteLine("What filename do you want to try now? (Type \"quit\" to abort)");
                     string new_filename = Console.ReadLine();

                     while (new_filename.Length == 0
                        || !new_filename.Contains('.')
                        || new_filename.Contains('\\'))
                     {
                        if (new_filename.ToLower().Equals("quit"))
                        {
                           return null;
                        }
                        Console.WriteLine("Invalid entry. Try again.");
                        new_filename = Console.ReadLine();
                     }

                     return new_filename;
                  }

                  /// <summary>
                  /// Queries the console for a new directory path to use as the target file's directory path.
                  /// </summary>
                  /// <returns>A string representing the new directory path to use as the target file's directory path. Null returned if user wants to abort.</returns>
                  private string query_new_path()
                  {
                     Console.WriteLine("Invalid path.");
                     Console.WriteLine("What directory pathway do you want to try now? (Type \"quit\" to abort)");
                     string new_path = Console.ReadLine();

                     while (new_path.Length == 0
                        || !new_path.Contains('\\')
                        || new_path.Contains('.'))
                     {
                        if (new_path.ToLower().Equals("quit"))
                        {
                           return null;
                        }
                        Console.WriteLine("Invalid entry. Try again.");
                        new_path = Console.ReadLine();
                     }

                     new_path = to_formal_path(new_path);

                     return new_path;
                  }

            private byte[] extract_bytes(long stream_length)
            {
               // extract the bytes
               byte[] result = new byte[stream_length];
               //filestream.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
               //filestream.ReadBytes((int)stream_length);

               // ERROR TESTING
               filestream.Close();
               using (System.IO.StreamReader temp = new System.IO.StreamReader(filepath))
               {
                  string data = temp.ReadToEnd();
                  for (int i = 0; i < data.Length; i++)
                  {
                     result[i] = (byte)data[i];
                  }
                  temp.Close();
               }

               return result;
            }

      override public string ToString()
      {
         if (file_data == null)
         {
            return "Null";
         }
         else if (file_data.Length == 0)
         {
            return "Empty array";
         }

         System.Text.StringBuilder result = new System.Text.StringBuilder();

         result.Append("{");
         result.Append(String.Format("[{0}]", file_data[0]));

         for (int i = 1; i < file_data.Length; i++)
         {
            result.Append(", ");
            if (i % 5 == 0)
            {
               result.Append('\n');
            }

            // buffer to standardize and align byte values
            if (file_data[i] < 10)
            {
               result.Append("  ");
            }
            else if (file_data[i] < 100)
            {
               result.Append(" ");
            }

            result.Append(String.Format("[{0}]", file_data[i]));
         }

         result.Append("}");

         return result.ToString();
      }

      public void display()
      {
         // display the file name
         // display the bytes in the array
         Console.WriteLine("Byte array from file \"{0}\"", filepath);
         Console.WriteLine("\thas {0} bytes", file_data.Length);
         Console.WriteLine();
         Console.WriteLine(ToString());
      }

      // Fields
      private string filepath_p;
      private System.IO.BinaryReader filestream;
      private byte[] file_data_p;

      // Properties
      public byte[] file_data
      {
         get
         {
            return file_data_p;
         }
      }
      public string filepath
      {
         get { return filepath_p; }
      }
   }
}
