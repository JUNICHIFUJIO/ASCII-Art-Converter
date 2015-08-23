using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// environment's current directory doesn't have \\, just \ for the path

namespace PNGHandler
{
   public enum Colour_Types {GREYSCALE = 0, TRUECOLOR = 2, INDEXED_COLOUR = 3, GREYSCALE_WITH_ALPHA = 4, TRUECOLOUR_WITH_ALPHA = 6};
   public enum Interlace_Methods { NO_INTERLACE = 0, ADAM7 = 1};

   /// <summary>
   /// A class designed to open a PNG file, determine if it's valid, and allow easy access to the data held inside the file.
   /// </summary>
   class PNGFileReader
   {
      static void Main(string[] args)
      {
         PNGFileReader blah = new PNGFileReader("blank.png", System.Environment.CurrentDirectory);
         blah.display_chunks();

         Console.ReadKey();
      }

      /// <summary>
      /// Opens a file for parsing.
      /// </summary>
      /// <param name="filename">The target file's name, with extension.</param>
      /// <param name="path">The directory for the file to be found in.</param>
      public PNGFileReader(string filename, string path)
      {
         // guard conditions
         if (filename.Length < 1
            || path.Length < 1)
         {
            throw new System.ArgumentException("PNGFileReader: Invalid constructor arguments.");
         }

         set_filepath(filename, path);
         // open the png file
         filestream = open_file(ref filepath_p);
         // set chunk pointers
         set_chunk_info();
         // verify the integrity of the PNG file
         set_is_valid_png();
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
            if(filename == null
               || path == null)
            {
               throw new System.ArgumentNullException("PNGFileReader: Null argument passed when constructing full file path.");
            }

            bool invalid_arguments = filename.Length == 0
               || path.Length < 2
               || !filename.Contains(".")
               || !path.Contains("\\");
            if (invalid_arguments)
            {
               throw new System.ArgumentException("PNGFileReader: Invalid arguments, unable to construct full file path.");
            }

            StringBuilder full_path = new StringBuilder(path);
            /*
            // append trailing backslash to folder if it doesn't exist
            if (path[path.Length - 1] != '\\')
            {
               if (path[path.Length - 2] != '\\')
               {
                  // current directory doesn't add the trailing backslash character
                  full_path.Append("\\\\");
               }
               else
               {
                  full_path.Append("\\");
               }
            }
             * 
             * */
            // add the file name to the path
            full_path.Append(filename);

            return full_path.ToString();
         }

         /// <summary>
         /// Repeatedly tries opening a file until told to quit or until a valid file is found.
         /// </summary>
         /// <param name="full_path">Full path specifying a specific target file in a target directory path.</param>
         /// <returns>StreamReader pointing to the target file.</returns>
         private System.IO.StreamReader open_file(ref string full_path)
         {
            System.IO.StreamReader stream = null;
            int original_path_length = full_path.LastIndexOf('\\');
            string original_path = full_path.Substring(0, original_path_length+1);
            string original_filename = full_path.Substring(original_path_length + 1);

            // continue trying to open the file
            while (true) { 
               try
               {
                  stream = attempt_to_open_target_file(full_path);
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
                  else{
                     if (!full_path.Contains('.'))
                     {
                        new_filename = query_new_filename();
                        if(new_filename == null)
                        {
                           return null;
                        }
                     }
                     if(!full_path.Contains('\\')){
                        new_path = query_new_path();
                        if(new_path == null)
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
                     if(new_path == null){
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
            private System.IO.StreamReader attempt_to_open_target_file(string full_file_path)
            {
               // guard conditions
               if (full_file_path == null)
               {
                  throw new ArgumentNullException("PNGFileReader: Null string encountered when trying to open file.");
               }
               bool invalid_arguments = full_file_path.Length <= 0
                  || !full_file_path.Contains(".")
                  || !full_file_path.Contains("\\");

               if (invalid_arguments)
               {
                  throw new ArgumentException("PNGFileReader: Invalid arguments passed to file opening mechanism.");
               }

               System.IO.StreamReader filestream = null;
               try{
                  filestream = new System.IO.StreamReader(full_file_path);
                  if(filestream == null){
                     throw new System.IO.IOException("PNGFileReader: IO error");
                  }
               }
               catch(System.IO.DirectoryNotFoundException){
                  throw new System.IO.DirectoryNotFoundException("PNGFileReader: Cannot find target directory.");
               }
               catch(System.IO.FileNotFoundException){
                  throw new System.IO.FileNotFoundException("PNGFileReader: Cannot find target file.");
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

               while(new_path.Length == 0
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

         private void set_chunk_info()
         {
            PNG_Chunks = get_png_chunks();

            IDHR_p = null;
            PLTE_p = null;
            List<PNG_Chunk> IDAT_chunk_list = new List<PNG_Chunk>();
            IDAT_chunks_p = null;
            CRCs_p = new byte[PNG_Chunks.Length][];

            PNG_Chunks = get_png_chunks();
            for (int i = 0; i < PNG_Chunks.Length; i++)
            {
               PNG_Chunk chunk = PNG_Chunks[i];
               if (chunk.chunk_name.Equals("IHDR"))
               {
                  if(IDHR_p != null){
                     throw new Exception("PNGFileReader: Multiple IDHR chunks detected.");
                  }
                  else{
                     if (chunk.chunk_length != 13)
                     {
                        throw new Exception("PNGFileReader: Invalid IDHR chunk length detected.");
                     }
                     IDHR_p = chunk;
                  }
               }
               else if (chunk.chunk_name.Equals("PLTE_p"))
               {
                  if (PLTE_p != null)
                  {
                     throw new Exception("PNGFileReader: Multiple PLTE chunks detected.");
                  }
                  else
                  {
                     PLTE_p = chunk;
                  }
               }
               else if (chunk.chunk_name.Equals("IDAT"))
               {
                  IDAT_chunk_list.Add(chunk);
               }
               else if (chunk.is_IEND())
               {
                  // convert IDAT chunk list to chunk array
                  IDAT_chunks_p = new PNG_Chunk[IDAT_chunk_list.Count];
                  int index = 0;
                  foreach (PNG_Chunk IDAT_chunk in IDAT_chunk_list)
                  {
                     IDAT_chunks_p[index++] = IDAT_chunk;
                  }

                  if (i != PNG_Chunks.Length - 1)
                  {
                     throw new Exception("PNGFileReader: Extra chunks after the IEND chunk.");
                  }
                  break;
               }

               // add the CRC to the CRC array
               CRCs_p[i] = chunk.chunk_CRC;
            }

            set_IDHR_info();
            set_PLTE_info();
            set_IDAT_info();
         }

            /// <summary>
            /// Sets the easily accessible IDHR info
            /// (width, height, bit depth, colour type,
            /// compression method, filter method, and
            /// interlace method) based off of the
            /// pointed-to IDHR chunk.
            /// </summary>
            private void set_IDHR_info()
            {
               if (IDHR.chunk_length != 13)
               {
                  throw new Exception("PNGFileReader: Invalid IDHR chunk length detected.");
               }

               int index = 0;
               // set the image width
               width_p = IDHR.chunk_data[index++];
               for (; index < 4; index++)
               {
                  width_p = (width_p << 8) + IDHR.chunk_data[index];
               }
               // set the image height
               height_p = IDHR.chunk_data[index++];
               for (; index < 8; index++)
               {
                  height_p = (height_p << 8) + IDHR.chunk_data[index];
               }
               // set the bit depth
               bit_depth_p = IDHR.chunk_data[index++];
               // set the colour_type
               colour_type_p = IDHR.chunk_data[index++];
               // set the compression method
               compression_method_p = IDHR.chunk_data[index++];
               // set the filter method
               filter_method_p = IDHR.chunk_data[index++];
               // set the interlace method
               interlace_method_p = IDHR.chunk_data[index++];

               // check for valid entries
               if (colour_type > Colour_Types.TRUECOLOUR_WITH_ALPHA)
               {
                  throw new Exception("PNGFileReader: Invalid colour type (greyscale, truecolor, indexed, etc.)");
               }
               if (compression_method_p != 0)
               {
                  throw new Exception("PNGFileReader: Invalid compression method detected.");
               }
               if (filter_method_p != 0)
               {
                  throw new Exception("PNGFileReader: Invalid filter method detected.");
               }
               if (interlace_method > Interlace_Methods.ADAM7)
               {
                  throw new Exception("PNGFileReader: Invalid interlace method detected.");
               }
            }

            private void set_PLTE_info()
            {
               if (PLTE_p == null)
               {
                  return;
               }
            }

            private void set_IDAT_info()
            {
               if (IDAT_chunks_p == null)
               {
                  return;
               }
            }

         private void set_is_valid_png()
         {
            is_valid_png_p = is_valid_chunk_count() && is_valid_chunk_order();

            /*
            is_valid_png_p = true;
            reset_stream();
            string streamstring = filestream.ReadToEnd();
            // 1) check for PNG signature
            if (streamstring.Length < 8)
            {
               is_valid_png_p = false;
               return;
            }
            int[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
            for (int i = 0; i < signature.Length; i++)
            {
               if ((int)streamstring[i] != signature[i])
               {
                  is_valid_png_p = false;
                  return;
               }
            }
            streamstring = streamstring.Substring(8);
            // 2) check for the IHDR length
            int IHDR_length = (int)streamstring[0];
            for (int i = 1; i < 4; i++)
            {
               IHDR_length = IHDR_length << 8 + (int)streamstring[i];
            }
            streamstring = streamstring.Substring(4);
            if (IHDR_length != 13)
            {
               is_valid_png_p = false;
               return;
            }
            // 3) check for the IHDR name
            if (!streamstring.Substring(0, 4).Equals("IHDR"))
            {
               is_valid_png_p = false;
               return;
            }
            streamstring = streamstring.Substring(4);
            // 4) check the IHDR CRC
            // 5) 
               reset_stream();
             * */
         }

            private bool is_valid_chunk_count()
            {
               string[] chunk_names = {"IHDR",
                                         "tIME",
                                         "pHYs",
                                         "iCCP",
                                         "sRGB",
                                      "sBIT",
                                      "gAMA",
                                      "cHRM",
                                      "PLTE",
                                      "tRNS",
                                      "hIST",
                                      "bKGD",
                                      "IDAT",
                                      "IEND"};
               int[] chunk_counts = new int[chunk_names.Length];

               // gather the counts
               for (int i = 0; i < PNG_Chunks.Length; i++)
               {
                  PNG_Chunk chunk = PNG_Chunks[i];
                  for (int j = 0; j < chunk_names.Length; j++)
                  {
                     if (chunk.chunk_name.Equals(chunk_names[j]))
                     {
                        chunk_counts[j]++;
                        break;
                     }
                  }
               }

               for (int i = 0; i < chunk_counts.Length; i++)
               {
                  if(chunk_names[i].Equals("IHDR")
                     || chunk_names[i].Equals("IEND"))
                  {
                     if (chunk_counts[i] != 1)
                     {
                        return false;
                     }
                  }
                  else if (chunk_names[i].Equals("IDAT"))
                  {
                     if (chunk_counts[i] < 1)
                     {
                        return false;
                     }
                  }
                  else
                  {
                     if (chunk_counts[i] > 1)
                     {
                        return false;
                     }
                  }
               }

               return true;
            }

            private bool is_valid_chunk_order()
            {
               bool IDAT_reached = false;

               if (!PNG_Chunks[0].chunk_name.Equals("IHDR"))
               {
                  return false;
               }

               for (int i = 1; i < PNG_Chunks.Length; i++)
               {
                  PNG_Chunk chunk = PNG_Chunks[i];
                  if (chunk.chunk_name.Equals("IDAT"))
                  {
                     IDAT_reached = true;
                  }
                  else if(chunk.chunk_name.Equals("IEND")
                     && !IDAT_reached)
                  {
                     return false;
                  }
                  else if (IDAT_reached)
                  {
                     return false;
                  }
               }

               return true;
            }

      /// <summary>
      /// Creates a PNGFileReader that looks for the file with the given filename in the current working directory.
      /// </summary>
      /// <param name="filename">Name of the file to open.</param>
      public PNGFileReader(string filename) : this(filename, System.Environment.CurrentDirectory) { }

      //-----------------------------------------------------------------------
      // Methods
      public void display_chunks()
      {
         string[] png_strings = interpret_png_file_chunks();

         foreach (string png_string in png_strings)
         {
            Console.WriteLine(png_string);
         }
      }

      public string[] interpret_png_file_chunks()
      {
         PNG_Chunk[] file_chunks = get_png_chunks();
         string[] readable_chunks_arr = new string[file_chunks.Length];
         //StringBuilder readable_chunk = new StringBuilder();

         for (int i = 0; i < file_chunks.Length; i++)
         {
            /*
            readable_chunk.Append(String.Format("Chunk #{0}:\n", i + 1));
            readable_chunk.Append(String.Format("\tChunk name: {0}\n", file_chunks[i].chunk_name));
            readable_chunk.Append(String.Format("\tChunk length: {0}\n", file_chunks[i].chunk_length));
            readable_chunk.Append(String.Format("\tChunk data: {0}\n", file_chunks[i].chunk_data));
            readable_chunk.Append(String.Format("\t\t(Chunk data length: {0})\n", file_chunks[i].chunk_data.Length));
            readable_chunk.Append(String.Format("\tChunk CRC: {0}\n", file_chunks[i].chunk_CRC));

            readable_chunks_arr[i] = readable_chunk.ToString();

            readable_chunk.Clear();
             * */

            readable_chunks_arr[i] = String.Format("Chunk #{0}:\n\t", i + 1) + file_chunks[i].ToString();
         }

         return readable_chunks_arr;
      }

         public PNG_Chunk[] get_png_chunks()
         {
            string file_bytes_string = get_png_file_chunks_string();
            PNG_Chunk[] file_chunks = parse_chunk_string(file_bytes_string);
            return file_chunks;
         }

            private string get_png_file_chunks_string()
            {
               reset_stream();
               string file_string = filestream.ReadToEnd();
               reset_stream();

               if (!file_string.Substring(1, 3).Equals("PNG"))
               {
                  throw new Exception("PNGFileReader: File is not of type PNG");
               }

               // get past the 8 byte header
               string file_chunks_string = file_string.Substring(8);

               return file_chunks_string;
            }

               private void reset_stream()
               {
                  filestream.BaseStream.Seek(0, new System.IO.SeekOrigin());
               }

            private PNG_Chunk[] parse_chunk_string(string chunk_string)
            {
               // the layout of each chunk is supposed to be
               List<PNG_Chunk> chunk_list = new List<PNG_Chunk>();

               while (chunk_string.Length >= 12)
               {
                  // interpret the chunks
                  chunk_list.Add(new PNG_Chunk(ref chunk_string));
               }

               // turn the list into an array
               PNG_Chunk[] chunk_arr = new PNG_Chunk[chunk_list.Count];

               int index = 0;
               foreach (PNG_Chunk chunk in chunk_list)
               {
                  chunk_arr[index++] = chunk;
               }

               return chunk_arr;
            }

      // Fields
      private System.IO.StreamReader filestream;
      private string filepath_p;
      private bool is_valid_png_p;
      private PNG_Chunk[] PNG_Chunks;
      // IDHR Fields
      private PNG_Chunk IDHR_p;
         // Width 4 bytes
         // Height 4 bytes
         // Bit Depth 1 byte
         // Colour type 1 byte
         // Compression Method 1 byte
         // Filter Method 1 byte
         // Interlace Method 1 byte
         private uint width_p;
         private uint height_p;
         private uint bit_depth_p;
         private uint colour_type_p;
            // Greyscale (0): Allowed bit depths: 1, 2, 4, 8, 16
            // Truecolor (2): 8, 16
            // Indexed-colour (3): 1, 2, 4, 8
            // Greyscale with alpha (4): 8, 16
            // Truecolour with alpha (6): 8, 16
         private uint compression_method_p;
            // indicates how you compress the scanline array
         private uint filter_method_p;
            // indicates how you rearrange bytes to make
            // them more compressible
               // 0 = adaptive filtering with five basic filter types
         private uint interlace_method_p;
            // Transmission order of the image data (gradual display)
            // 0 = No interlace
            // 1 = Adam7 interlace: (seven passes of reduced images)
      // PLTE Fields
      private PNG_Chunk PLTE_p;
         private int[][] palette_table_p;
         private int[] palette_alpha_table_p;
      // IDAT Fields
      private PNG_Chunk[] IDAT_chunks_p;
         private byte[] decompressed_datastream_p;
      // CRC Fields
      private byte[][] CRCs_p;

      // Properties
      public string filepath
      {
         get { return filepath_p; }
      }
      public bool is_valid_png
      {
         get { return is_valid_png_p; }
      }
      public PNG_Chunk IDHR
      {
         get { return IDHR_p; }
      }
      public int width
      {
         get { return (int)width_p; }
      }
      public int height
      {
         get { return (int)height_p; }
      }
      public int bit_depth
      {
         get { return (int)bit_depth_p; }
      }
      public Colour_Types colour_type
      {
         get { return (Colour_Types)colour_type_p; }
      }
      public Interlace_Methods interlace_method
      {
         get { return (Interlace_Methods)interlace_method_p; }
      }

      public int[][] palette_table
      {
         get { return palette_table_p; }
      }
      public int[] palette_alpha_table
      {
         get { return palette_alpha_table_p; }
      }
      public byte[] decompressed_datastream
      {
         get { return decompressed_datastream_p; }
      }
      public byte[][] CRCs
      {
         get { return CRCs_p; }
      }

      // required chunks:
      // 1) IHDR: Must be first chunk. Contains width, height, bit depth, and color height.
      // 2) PLTE: Contains the palette; list of colors
      // 3) IDAT: Contains the image, which may be split among multiple IDAT chunks
      // 4) IEND: Image end
   }
}
