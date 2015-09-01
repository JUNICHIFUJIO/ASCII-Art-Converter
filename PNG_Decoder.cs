using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PNGHandler
{
   public enum Colour_Types { GREYSCALE = 0, TRUECOLOR = 2, INDEXED_COLOUR = 3, GREYSCALE_WITH_ALPHA = 4, TRUECOLOUR_WITH_ALPHA = 6 };
   public enum Interlace_Methods { NO_INTERLACE = 0, ADAM7 = 1 };
   public enum Filter_Types { NONE = 0, SUB = 1, UP = 2, AVERAGE = 3, PAETH = 4};

   class PNG_Decoder
   {
      // Constructor
      /// <summary>
      /// Creates an instance of the PNG_Decoder class, which will parse the
      /// array of bytes passed in to interpret it for information specific to
      /// a PNG format file.
      /// </summary>
      /// <param name="file_data">The raw file data from the PNG format file.</param>
      public PNG_Decoder(byte[] file_data)
      {
         // set the PNG data
         data_p = file_data;

         // as a fail-safe, set the chunk pointers
         IHDR_p = null;
         PLTE_p = null;

         // create a CRC handler class to check/make crcs
         crc_handler = new CRC_Handler(CRC_Modes.CRC_32);

         // Get the chunks out of the data
         PNG_Chunks = get_png_chunks(file_data, out is_valid_png_p);

         // Get the IHDR info
         get_IHDR_info();

         // Get the PLTE info
         get_PLTE_info();

         // Get the # of bytes per pixel
         pixel_width_p = calculate_pixel_width();

         // Get the IDAT chunks
         IDAT_chunks_p = get_IDAT_chunks(PNG_Chunks);

         // ERROR TESTING
         // since CRCs aren't valid for some reason, set is_valid_png to true so the following won't be null
         is_valid_png_p = true;

         // decompress the datastream
         decompressed_datastream_p = decompress_datastream(IDAT_chunks_p);
         // unfilter the datastream
         unfiltered_datastream_p = unfilter_datastream(decompressed_datastream_p);
      }

      // Methods
      /// <summary>
      /// Returns an array of PNG_Chunk objects that each store information
      /// related to that conceptual chunk. The chunks are constructed based
      /// off of the PNG specification and the raw PNG file data is passed in
      /// for parsing.
      /// 
      /// AREAS OF IMPROVEMENT: The checking that the passed in data aligns with
      /// the specifications of a PNG file are rather light. If completely invalid
      /// data were to be passed in, the method would continue checking regardless
      /// of the many potential opportunities to discover the file is invalid and
      /// impossible to parse.
      /// </summary>
      /// <param name="file_data">The raw file data from the PNG format file.</param>
      /// <param name="is_valid_png_file">An uninitialized boolean that will determine
      /// whether the passed in data represents a valid PNG file.</param>
      /// <returns></returns>
      private PNG_Chunk[] get_png_chunks(byte[] file_data, out bool is_valid_png_file)
      {
         if (data_p == null)
         {
            is_valid_png_file = false;
            return null;
         }

         Dictionary<string, int> chunk_map = new Dictionary<string, int>();

         System.Collections.Generic.List<PNG_Chunk> chunk_list =
            new System.Collections.Generic.List<PNG_Chunk>();

         string[] expected_headers = { "IHDR", "PLTE", "IDAT", "IEND" };
         int expected_header_index = 0;
         is_valid_png_file = true;
         bool palette_present = false;

         // move index past the file header (8 bytes)
         int byte_index = 8;
         while (byte_index < file_data.Length)
         {
            PNG_Chunk chunk = new PNG_Chunk(file_data, ref byte_index);
            if (chunk_map.ContainsKey(chunk.chunk_name))
            {
               chunk_map[chunk.chunk_name] += 1;
            }
            else
            {
               chunk_map.Add(chunk.chunk_name, 1);
            }

            if (chunk.chunk_name.Equals("PLTE"))
            {
               palette_present = true;
               if (chunk_map.ContainsKey("bKGD")
                  || chunk_map.ContainsKey("hIST")
                  || chunk_map.ContainsKey("tRNS"))
               {
                  is_valid_png_file = false;
               }
               PLTE_p = chunk;
            }

            // handle flagging the cases where the png file 
            // has an invalid ordering of the chunks
            if (expected_headers[expected_header_index].Equals("IHDR"))
            {
               if (!chunk.chunk_name.Equals("IHDR"))
               {
                  is_valid_png_file = false;
               }
               else
               {
                  ++expected_header_index;
                  IHDR_p = chunk;
               }
            }
            else if (expected_headers[expected_header_index].Equals("IDAT"))
            {
               if (chunk.chunk_name.Equals("IEND"))
               {
                  ++expected_header_index;
                  break;
               }

               string[] invalid_chunk_names = { "pHYs", "sPLT" };

               for (int i = 0; i < invalid_chunk_names.Length; i++)
               {
                  if (chunk.chunk_name.Equals(invalid_chunk_names[i]))
                  {
                     is_valid_png_file = false;
                  }
                  if (!is_valid_png_file)
                  {
                     break;
                  }
               }
            }
            // handle PLTE sections
            else if (!expected_headers[expected_header_index].Equals("PLTE"))
            {
               if (chunk.chunk_name.Equals("IDAT"))
               {
                  ++expected_header_index;
               }
               // before the palette chunk is found...
               else if (!palette_present)
               {
                  string[] invalid_chunk_names = { "bKGD", "hIST", "tRNS" };

                  for (int i = 0; i < invalid_chunk_names.Length; i++)
                  {
                     if (chunk.chunk_name.Equals(invalid_chunk_names[i]))
                     {
                        is_valid_png_file = false;
                     }
                     if (!is_valid_png_file)
                     {
                        break;
                     }
                  }

                  if (chunk.chunk_name.Equals("iCCP")
                     && chunk_map.ContainsKey("sBIT"))
                  {
                     is_valid_png_file = false;
                  }
                  else if (chunk.chunk_name.Equals("sBIT")
                     && chunk_map.ContainsKey("iCCP"))
                  {
                     is_valid_png_file = false;
                  }
               }
               // after the palette chunk is found
               else
               {
                  string[] invalid_chunk_names = { "cHRM", "gAMA", "iCCP", "sBIT", "sRGB" };

                  for (int i = 0; i < invalid_chunk_names.Length; i++)
                  {
                     if (chunk.chunk_name.Equals(invalid_chunk_names[i]))
                     {
                        is_valid_png_file = false;
                     }
                     if (!is_valid_png_file)
                     {
                        break;
                     }
                  }
               }

               // add the ancillary chunks coming before PLTE if they exist
               if (chunk.chunk_name.Equals("cHRM"))
               {
                  cHRM_p = chunk;
               }
               else if (chunk.chunk_name.Equals("gAMA"))
               {
                  gAMA_p = chunk;
               }
               else if (chunk.chunk_name.Equals("iCCP"))
               {
                  iCCP_p = chunk;
               }
               else if (chunk.chunk_name.Equals("sBIT"))
               {
                  sBIT_p = chunk;
               }
               else if (chunk.chunk_name.Equals("sRGB"))
               {
                  sRGB_p = chunk;
               }
               else if (chunk.chunk_name.Equals("bKGD"))
               {
                  bKGD_p = chunk;
                  if(colour_type == Colour_Types.GREYSCALE
                     || colour_type == Colour_Types.GREYSCALE_WITH_ALPHA)
                  {
                     bg_greyscale_p = chunk.chunk_data[1];
                  }
                  else if(colour_type == Colour_Types.TRUECOLOR
                     || colour_type == Colour_Types.TRUECOLOUR_WITH_ALPHA)
                  {
                     bg_R_p = chunk.chunk_data[1];
                     bg_G_p = chunk.chunk_data[3];
                     bg_B_p = chunk.chunk_data[5];
                  }
                  else
                  {
                     bg_palette_index_p = chunk.chunk_data[0];
                  }
               }
               else if (chunk.chunk_name.Equals("hIST"))
               {
                  hIST_p = chunk;
               }
               else if (chunk.chunk_name.Equals("tRNS"))
               {
                  tRNS_p = chunk;
                  if (colour_type == Colour_Types.GREYSCALE)
                  {
                     trns_grey_p = chunk.chunk_data[1];
                  }
                  else if (colour_type == Colour_Types.TRUECOLOR)
                  {
                     trns_R_p = chunk.chunk_data[1];
                     trns_G_p = chunk.chunk_data[3];
                     trns_B_p = chunk.chunk_data[5];
                  }
               }
               else if (chunk.chunk_name.Equals("pHYs"))
               {
                  pHYs_p = chunk;
               }
               else if (chunk.chunk_name.Equals("sPLT"))
               {
                  if (sPLT_p == null)
                  {
                     sPLT_p = new List<PNG_Chunk>();
                  }
                  sPLT_p.Add(chunk);
               }
            }

            else
            {
               // should have reached end of file already, there is extra
               // junk at the end of the file
            }

            // check the CRC
            if (!is_correct_CRC(chunk.chunk_data, chunk.chunk_CRC))
            {
               is_valid_png_file = false;
               // break out of the loop...?
            }

            // add the chunk to the list
            chunk_list.Add(chunk);

            // if it's an ancillary chunk where order doesn't matter...record it
            if (chunk.chunk_name.Equals("tIME"))
            {
               tIME_p = chunk;
            }
            else if (chunk.chunk_name.Equals("iTXt"))
            {
               if (iTXt_p == null)
               {
                  iTXt_p = new List<PNG_Chunk>();
               }
               iTXt_p.Add(chunk);
            }
            else if (chunk.chunk_name.Equals("tEXt"))
            {
               if (tEXt_p == null)
               {
                  tEXt_p = new List<PNG_Chunk>();
               }
               tEXt_p.Add(chunk);
            }
            else if (chunk.chunk_name.Equals("zTXt"))
            {
               if (zTXt_p == null)
               {
                  zTXt_p = new List<PNG_Chunk>();
               }
               zTXt_p.Add(chunk);
            }
         }

         // verify the # of each type of chunk
         // check the necessary ones first
         if (!chunk_map.ContainsKey("IHDR")
            || chunk_map["IHDR"] != 1)
         {
            is_valid_png_file = false;
         }
         else if (!chunk_map.ContainsKey("IDAT")
            || chunk_map["IDAT"] < 1)
         {
            is_valid_png_file = false;
         }
         else if (!chunk_map.ContainsKey("IEND")
            || chunk_map["IEND"] != 1)
         {
            is_valid_png_file = false;
         }

         // check the ancillary chunks now that have 0 or 1 present
         string[] ancillary_names = {"tIME", "pHYs", "sBIT", "gAMA", "cHRM", "tRNS", "hIST", "bKGD",
                                    "PLTE"};
         foreach (string name in ancillary_names)
         {
            if (chunk_map.ContainsKey(name)
               && chunk_map[name] != 1)
            {
               is_valid_png_file = false;
               break;
            }
         }

         return chunk_list.ToArray();
      }

            public bool is_correct_CRC(byte[] data, byte[] data_crc)
            {
               if(data == null
                  || data_crc == null){
                     throw new ArgumentNullException("PNG_Decoder: is_valid_CRC passed invalid argument(s).");
               }
               else if(data_crc.Length != 4)
               {
                  throw new ArgumentException("PNG_Decoder: Invalid CRC found.");
               }

               ulong crc = crc_handler.get_CRC(data);
               ulong file_crc = 0;
               foreach (byte data_crc_byte in data_crc)
               {
                  file_crc = file_crc << 8;
                  file_crc += data_crc_byte;
               }

               // ERROR TESTING // Console.WriteLine("CRC to hex: {0}, file crc to hex: {1}", Hexadecimal.ToHex((uint)crc), Hexadecimal.ToHex((uint)file_crc));

               if (crc == file_crc)
               {
                  return true;
               }
               return false;
            }

      /// <summary>
      /// Takes the info stored in the PNG_Decoder object to extract pertinent
      /// information supplied in the PNG's IHDR (Image HeaDeR) chunk.
      /// </summary>
      private void get_IHDR_info()
      {
         if (IHDR_p == null)
         {
            return;
         }

         // Width 4 bytes
         // Height 4 bytes
         // Bit Depth 1 byte
         // Colour type 1 byte
         // Compression Method 1 byte
         // Filter Method 1 byte
         // Interlace Method 1 byte

         int index = 0; // the byte index

         // get width info
         width_p = IHDR_p.chunk_data[index++];
         for (int i = 1; i < 4; i++)
         {
            width_p = width_p << 8;
            width_p += IHDR_p.chunk_data[index++];
         }
         // invalid png file if width is 0?

         // get height info
         height_p = IHDR_p.chunk_data[index++];
         for (int i = 1; i < 4; i++)
         {
            height_p = height_p << 8;
            height_p += IHDR_p.chunk_data[index++];
         }
         // invalid png file if height is 0?

         if(width_p < 1
            || height_p < 1)
         {
            is_valid_png_p = false;
         }

         // get bit depth
         bit_depth_p = IHDR_p.chunk_data[index++];

         // get colour type
         colour_type_p = IHDR_p.chunk_data[index++];

         // get compression method
         compression_method_p = IHDR_p.chunk_data[index++];

         // get filter method
         filter_method_p = IHDR.chunk_data[index++];

         // get interlace method
         interlace_method_p = IHDR.chunk_data[index++];
      }

      /// <summary>
      /// Takes the info stored in the PNG_Decoder object to extract pertinent
      /// information supplied in the PNG's PLTE (PaLeTtE) chunk.
      /// </summary>
      private void get_PLTE_info()
      {
         if (PLTE_p == null)
         {
            palette_table_p = null;
            palette_alpha_table_p = null;
            return;
         }

         if (colour_type == Colour_Types.GREYSCALE
            || colour_type == Colour_Types.TRUECOLOR)
         {
            palette_table_p = null;
            palette_alpha_table_p = null;
            return;
         }
         else
         {
            int n_entries = PLTE_p.chunk_length / 3;
            // set up the palette alpha table
            if (tRNS_p != null)
            {
               // if alpha table exists...
               // copy alpha table over from tRNS chunk
               palette_alpha_table_p = new byte[tRNS_p.chunk_length];
               for (int i = 0; i < palette_alpha_table_p.Length; i++)
               {
                  palette_alpha_table_p[i] = tRNS_p.chunk_data[i];
               }
            }
            // set up the palette table
            palette_table_p = new byte[n_entries][];
            int byte_index = 0;
            for (int i = 0; i < n_entries; i++)
            {
               palette_table_p[i] = new byte[3];
               // get R value
               palette_table_p[i][0] = PLTE_p.chunk_data[byte_index++];
               // get G value
               palette_table_p[i][1] = PLTE_p.chunk_data[byte_index++];
               // get B value
               palette_table_p[i][2] = PLTE_p.chunk_data[byte_index++];
            }
         }

         // ERROR TESTING
         Console.WriteLine("ERROR TESTING: PLTE size: {0}", PLTE_p.chunk_length);
      }

      /// <summary>
      /// Calculates how many bytes wide the data for a pixel is in the PNG file.
      /// </summary>
      /// <returns># of bytes for each pixel.</returns>
      private int calculate_pixel_width()
      {
         int pixel_width = 0;

         // set the correct pixel width (how many channels each pixel contains)
         if (colour_type == Colour_Types.GREYSCALE)
         {
            pixel_width = 1;
         }
         else if (colour_type == Colour_Types.TRUECOLOR)
         {
            pixel_width = 3;
         }
         else if (colour_type == Colour_Types.INDEXED_COLOUR)
         {
            if (palette_alpha_table_p == null)
            {
               pixel_width = 1;
            }
            else
            {
               pixel_width = 2;
            }
         }
         else if (colour_type == Colour_Types.GREYSCALE_WITH_ALPHA)
         {
            pixel_width = 2;
         }
         else if (colour_type == Colour_Types.TRUECOLOUR_WITH_ALPHA)
         {
            pixel_width = 4;
         }

         if (pixel_width < 1)
         {
            throw new Exception("PNG_Decoder: Invalid # of samples per pixel -> Invalid Colour Type found in PNG file.");
         }

         return pixel_width;
      }

      /// <summary>
      /// Retrieves the IDAT chunks from an array of all the chunks of information
      /// in the PNG file.
      /// </summary>
      /// <param name="chunks">The array of all the chunks weaned from the PNG file.</param>
      /// <returns>An array of the IDAT chunks (in order).</returns>
      private PNG_Chunk[] get_IDAT_chunks(PNG_Chunk[] chunks)
      {
         List<PNG_Chunk> IDAT_chunk_list = new List<PNG_Chunk>();

         foreach (PNG_Chunk chunk in chunks)
         {
            if (chunk.chunk_name.Equals("IDAT"))
            {
               IDAT_chunk_list.Add(chunk);
            }
         }

         return IDAT_chunk_list.ToArray();
      }

      /// <summary>
      /// Decompresses the stored IDAT information into a byte array.
      /// </summary>
      private byte[] decompress_datastream(PNG_Chunk[] idat_chunks)
      {
         if (!is_valid_png
            || idat_chunks == null)
         {
            return null;
         }

         if(idat_chunks == null){
            return null;
         }

         // extract just the data from the IDAT chunks
         System.Collections.Generic.List<byte> compressed_bytes =
            new System.Collections.Generic.List<byte>();

         foreach(PNG_Chunk IDAT_chunk in idat_chunks){
            // take the data from each idat chunk, remove the first two
            // bytes from each chunk (which are
            // zlib compression specific bytes, not
            // necessary for the compressed data at all)
            for(int i = 2; i < IDAT_chunk.chunk_data.Length; i++){
               compressed_bytes.Add(IDAT_chunk.chunk_data[i]);
            }
         }

         // convert list of compressed bytes to deflated stream
         System.IO.MemoryStream memstream = new System.IO.MemoryStream(compressed_bytes.ToArray());
         System.IO.Compression.DeflateStream deflate =
            new System.IO.Compression.DeflateStream(memstream, System.IO.Compression.CompressionMode.Decompress);
         System.Collections.Generic.List<byte> decompressed_list =
            new System.Collections.Generic.List<byte>();

         // convert decompressed byte stream to a decompressed byte array to find the # of decompressed bytes
         byte[] temp = new byte[1000 * deflate.BaseStream.Length];
         int true_length = deflate.Read(temp, 0, temp.Length);

         for (int i = 0; i < true_length; i++)
         {
            decompressed_list.Add(temp[i]);
         }

         return decompressed_list.ToArray();
      }

      /// <summary>
      /// Reverses the filtering of the bytes passed in.
      /// Assumes that the bytes are a one-dimensional array of all of
      /// the bytes of data. This means that one scanline comes after
      /// another, and this method uses the stored height/width fields
      /// of the PNG file decoded to determine when a new scanline is
      /// reached. Also assumes that the PNG file is valid.
      /// </summary>
      /// <param name="decompressed_datastream">The decompressed datastream to be unfiltered.</param>
      /// <returns>An array of bytes that contains the unfiltered, decompressed datastream's bytes.</returns>
      private byte[] unfilter_datastream(byte[] decompressed_datastream)
      {
         if (!is_valid_png
            || decompressed_datastream == null)
         {
            return null;
         }

         List<byte> unfiltered_bytes = new List<byte>();

         // process the first scanline
         byte[] previous_scanline = new byte[width_p * pixel_width_p + 1];
         byte[] current_scanline = new byte[width_p * pixel_width_p + 1];
         int byte_index = 0;
         

         Filter_Types filter_type = (Filter_Types)decompressed_datastream[byte_index++];
         current_scanline[0] = decompressed_datastream_p[0];

         // specially handle the first scanline (no previous scanline) and add results to
         // the unfiltered bytes list
         for (; byte_index < width_p * pixel_width_p + 1; )
         {
            current_scanline[byte_index] = decompressed_datastream[byte_index];
            ++byte_index;
         }
         byte_index = 0;
         previous_scanline = unfilter_scanline(filter_type, current_scanline, previous_scanline);
         foreach (byte scanline_byte in previous_scanline)
         {
            unfiltered_bytes.Add(scanline_byte);
         }

         // unfilter the other scanlines, and add them to the list
         for (int i = 1; i < height_p; i++)
         {
            int scanline_length = (int)width_p * pixel_width + 1;
            int first_bit_index = (scanline_length) * i;

            current_scanline[0] = decompressed_datastream[first_bit_index + byte_index++];
            filter_type = (Filter_Types)current_scanline[0];

            for (; byte_index < scanline_length; )
            {
               current_scanline[byte_index] = decompressed_datastream[first_bit_index + byte_index];
               ++byte_index;
            }
            previous_scanline = unfilter_scanline(filter_type, current_scanline, previous_scanline);
            foreach (byte scanline_byte in previous_scanline)
            {
               unfiltered_bytes.Add(scanline_byte);
            }
            byte_index = 0;
         }

         return unfiltered_bytes.ToArray();
      }

            /// <summary>
            /// Reverses the filtering process done on the decompressed
            /// datastream to get the unfiltered datastream.
            /// </summary>
            /// <param name="filter_type">The type of filtering process done on the section of data.</param>
            private byte[] unfilter_scanline(Filter_Types filter_type, byte[] filtered_scanline_bytes, byte[] previous_scanline_bytes)
            {
               byte[] result = null;

               switch (filter_type)
               {
                  case(Filter_Types.SUB):
                     result = unfilter_sub_scanline(filtered_scanline_bytes);
                     break;
                  case (Filter_Types.UP):
                     result = unfilter_up_scanline(filtered_scanline_bytes, previous_scanline_bytes);
                     break;
                  case (Filter_Types.AVERAGE):
                     result = unfilter_avg_scanline(filtered_scanline_bytes, previous_scanline_bytes);
                     break;
                  case (Filter_Types.PAETH):
                     result = unfilter_paeth_scanline(filtered_scanline_bytes, previous_scanline_bytes);
                     break;
                  default:
                     result = new byte[filtered_scanline_bytes.Length - 1];
                     for (int i = 1; i < filtered_scanline_bytes.Length; i++)
                     {
                        result[i - 1] = filtered_scanline_bytes[i];
                     }
                     break;
               }

               return result;
            }

                  /// <summary>
                  /// Unfilters a sub-filtered scanline.
                  /// </summary>
                  /// <param name="filtered_scanline_bytes">The bytes of the scanline to be unfiltered.</param>
                  /// <returns>An array of bytes that contains the unfiltered scanline.</returns>
                  private byte[] unfilter_sub_scanline(byte[] filtered_scanline_bytes)
                  {
                     if(filtered_scanline_bytes == null
                        || filtered_scanline_bytes.Length != width_p * pixel_width_p + 1)
                     {
                        return null;
                     }
                     else if (filtered_scanline_bytes[0] != (byte)Filter_Types.SUB)
                     {
                        throw new ArgumentException("PNG_Decoder: Invalid defiltering type used (SUB).");
                     }

                     byte[] result = new byte[filtered_scanline_bytes.Length - 1];

                     // ignore the filter byte
                     int filtered_index = 1; // previous_scanline_index = filtered_index - 1
                     byte a = 0; // left of current byte

                     // process the bytes of the scanline
                     for (; filtered_index < filtered_scanline_bytes.Length; )
                     {
                        if (filtered_index > pixel_width_p)
                        {
                           a = result[filtered_index - pixel_width_p - 1];
                        }

                        result[filtered_index - 1] = (byte)(filtered_scanline_bytes[filtered_index] + a);
                        ++filtered_index;
                     }

                     return result;
                  }

                  /// <summary>
                  /// Unfilters an up-filtered scanline.
                  /// </summary>
                  /// <param name="filtered_scanline_bytes">The bytes of the scanline to be unfiltered.</param>
                  /// <param name="previous_scanline_bytes">The bytes of the previous, unfiltered scanline.</param>
                  /// <returns>An array of bytes that contains the unfiltered scanline.</returns>
                  private byte[] unfilter_up_scanline(byte[] filtered_scanline_bytes, byte[] previous_scanline_bytes)
                  {
                     // guard conditions
                     if (filtered_scanline_bytes == null
                        || filtered_scanline_bytes.Length != width_p * pixel_width_p + 1)
                     {
                        return null;
                     }
                     else if(previous_scanline_bytes == null
                        && previous_scanline_bytes.Length != width_p * pixel_width_p)
                     {
                        throw new ArgumentNullException("PNG_Decoder: Invalid defiltering type used (UP).");
                     }
                     else if (filtered_scanline_bytes[0] != (byte)Filter_Types.UP)
                     {
                        throw new ArgumentException("PNG_Decoder: Invalid defiltering type used (UP).");
                     }

                     byte[] result = new byte[filtered_scanline_bytes.Length - 1];

                     // ignore the filter byte
                     int filtered_index = 1; // previous_scanline_index = filtered_index - 1
                     byte b = previous_scanline_bytes[filtered_index - 1]; // equivalent byte from previous scanline (unfiltered)

                     // process the bytes of the scanline
                     for (; filtered_index < filtered_scanline_bytes.Length; )
                     {
                        b = previous_scanline_bytes[filtered_index - 1];

                        result[filtered_index - 1] = (byte)(filtered_scanline_bytes[filtered_index] + b);
                        ++filtered_index;
                     }

                     return result;
                  }

                  /// <summary>
                  /// Unfilters an average-filtered scanline.
                  /// </summary>
                  /// <param name="filtered_scanline_bytes">The bytes of the scanline to be unfiltered.</param>
                  /// <param name="previous_scanline_bytes">The bytes of the previous, unfiltered scanline.</param>
                  /// <returns>An array of bytes that contains the unfiltered scanline.</returns>
                  private byte[] unfilter_avg_scanline(byte[] filtered_scanline_bytes, byte[] previous_scanline_bytes)
                  {
                     // guard conditions
                     if (filtered_scanline_bytes == null
                        || filtered_scanline_bytes.Length != width_p * pixel_width_p + 1)
                     {
                        return null;
                     }
                     else if (previous_scanline_bytes == null
                        && previous_scanline_bytes.Length != width_p * pixel_width_p)
                     {
                        throw new ArgumentNullException("PNG_Decoder: Invalid defiltering type used (AVERAGE).");
                     }
                     else if (filtered_scanline_bytes[0] != (byte)Filter_Types.AVERAGE)
                     {
                        throw new ArgumentException("PNG_Decoder: Invalid defiltering type used (AVERAGE).");
                     }

                     byte[] result = new byte[filtered_scanline_bytes.Length - 1];

                     // ignore the filter byte
                     int filtered_index = 1; // previous_scanline_index = filtered_index - 1
                     byte a = 0; // left of current byte
                     byte b = previous_scanline_bytes[filtered_index - 1]; // equivalent byte from previous scanline (unfiltered)

                     // process the bytes of the scanline
                     for (; filtered_index < filtered_scanline_bytes.Length; )
                     {
                        if (filtered_index > pixel_width_p)
                        {
                           // special case for the first pixel
                           a = result[filtered_index - pixel_width_p - 1];
                        }
                        b = previous_scanline_bytes[filtered_index - 1];

                        result[filtered_index - 1] = (byte)(filtered_scanline_bytes[filtered_index] + ((a + b) >> 1));
                        ++filtered_index;
                     }

                     return result;
                  }

                  /// <summary>
                  /// Unfilters a paeth-filtered scanline. Uses the Paeth predictor.
                  /// </summary>
                  /// <param name="filtered_scanline_bytes">The bytes of the scanline to be unfiltered.</param>
                  /// <param name="previous_scanline_bytes">The bytes of the previous, unfiltered scanline.</param>
                  /// <returns>An array of bytes that contains the unfiltered scanline.</returns>
                  private byte[] unfilter_paeth_scanline(byte[] filtered_scanline_bytes, byte[] previous_scanline_bytes)
                  {
                     // guard conditions
                     if (filtered_scanline_bytes == null
                        || filtered_scanline_bytes.Length != width_p * pixel_width_p + 1)
                     {
                        return null;
                     }
                     else if (previous_scanline_bytes == null
                        && previous_scanline_bytes.Length != width_p * pixel_width_p)
                     {
                        throw new ArgumentNullException("PNG_Decoder: Invalid defiltering type used (PAETH).");
                     }
                     else if (filtered_scanline_bytes[0] != (byte)Filter_Types.PAETH)
                     {
                        throw new ArgumentException("PNG_Decoder: Invalid defiltering type used (PAETH).");
                     }

                     byte[] result = new byte[filtered_scanline_bytes.Length - 1];

                     // ignore the filter byte
                     int filtered_index = 1; // previous_scanline_index = filtered_index - 1
                     byte a = 0; // left of current byte
                     byte b = previous_scanline_bytes[filtered_index - 1]; // equivalent byte from previous scanline (unfiltered)
                     byte c = 0; // left of b
                     byte pred = Paeth_Predictor(a, b, c);

                     // process the bytes of the scanline
                     for (; filtered_index < filtered_scanline_bytes.Length; )
                     {
                        if (filtered_index > pixel_width_p)
                        {
                           a = result[filtered_index - pixel_width_p - 1];
                           c = previous_scanline_bytes[filtered_index - pixel_width_p - 1];
                        }
                        b = previous_scanline_bytes[filtered_index - 1];
                        pred = Paeth_Predictor(a, b, c);

                        result[filtered_index - 1] = (byte)(filtered_scanline_bytes[filtered_index] + pred);
                        ++filtered_index;
                     }

                     return result;
                  }

                        /// <summary>
                        /// Returns the byte (between a, b, and c) that is closest to the value of
                        /// a + b - c.
                        /// </summary>
                        /// <param name="a">The unfiltered byte to the left of the current filtered one.</param>
                        /// <param name="b">The equivalent, unfiltered byte from the previous scanline.</param>
                        /// <param name="c">The unfiltered byte to the left of b.</param>
                        /// <returns>The byte (between a, b, and c) that is closest to the value of a + b - c.</returns>
                        private byte Paeth_Predictor(byte a, byte b, byte c)
                        {
                           int p = a + b - c;
                           int pa = Math.Abs(p - a);
                           int pb = Math.Abs(p - b);
                           int pc = Math.Abs(p - c);
                           if(pa <= pb
                              && pa <= pc)
                           {
                              return a;
                           }
                           else if (pb <= pc)
                           {
                              return b;
                           }
                           else
                           {
                              return c;
                           }
                        }

      public byte[] to_greyscale()
      {
         return to_greyscale_handler(unfiltered_datastream_p);
      }

            private byte[] to_greyscale_handler(byte[] unfiltered_data)
            {
               byte[] result = null;

               switch (colour_type)
               {
                  case(Colour_Types.GREYSCALE):
                     result = unfiltered_data;
                     break;
                  case(Colour_Types.TRUECOLOR):
                  case(Colour_Types.TRUECOLOUR_WITH_ALPHA):
                     result = truecolor_to_greyscale(unfiltered_data);
                     break;
                  case(Colour_Types.INDEXED_COLOUR):
                     result = indexed_colour_to_greyscale(unfiltered_data);
                     break;
                  case(Colour_Types.GREYSCALE_WITH_ALPHA):
                     result = greyscale_with_alpha_to_greyscale(unfiltered_data);
                     break;
               }

               return result;
            }

                  private byte[] truecolor_to_greyscale(byte[] unfiltered_data)
                  {
                     if(colour_type != Colour_Types.TRUECOLOR
                        && colour_type != Colour_Types.TRUECOLOUR_WITH_ALPHA)
                     {
                        throw new InvalidOperationException("PNG_Decoder: Invalid greyscale conversion (Truecolor (w/ or w/o alpha)).");
                     }
                     List<byte> converted_bytes = new List<byte>();

                     int byte_index = 0;

                     for (int line_index = 0; line_index < height_p; line_index++)
                     {
                        for (int pixel_index = 0; pixel_index < width_p; pixel_index++)
                        {
                           byte R = unfiltered_data[byte_index++];
                           byte G = unfiltered_data[byte_index++];
                           byte B = unfiltered_data[byte_index++];

                           double grey_value = (double)rgb_to_grey(R, G, B);
                           // process alpha
                           if (pixel_width_p == 4)
                           {
                              byte alpha = unfiltered_data[byte_index++];
                              double translucence = (double)(255 - alpha) / 255;

                              if (bKGD_p != null)
                              {
                                 double bg_grey_value = (double)rgb_to_grey(bg_R_p, bg_G_p, bg_B_p);

                                 grey_value = (1 - translucence) * grey_value + translucence * bg_grey_value;

                                 // grey_value *= ((double)unfiltered_data[byte_index++])/255;
                              }
                              else// if (alpha > 0)
                              {
                                 // ERROR TESTING: Invalid adjustment below. Has incorporate some other color somehow
                                 grey_value = (translucence * byte.MaxValue) + ((1 - translucence) * grey_value);
                              }
                           }
                           // create the greyscale byte
                           byte greyscale_byte = (byte)(int)grey_value;

                           // ERROR TESTING
                           //if (greyscale_byte != 255)
                           //{
                           //   Console.WriteLine("line: {0}, pixel: {1}; byte: {2}", line_index, pixel_index, greyscale_byte);
                           //}

                           converted_bytes.Add(greyscale_byte);
                        }
                     }

                     return converted_bytes.ToArray();
                  }

                        private byte rgb_to_grey(byte R, byte G, byte B)
                        {
                           double grey_value = 0;

                           // process R
                           grey_value += .299 * R;
                           // process G
                           grey_value += .587 * G;
                           // process B
                           grey_value += .114 * B;

                           return (byte)(int)grey_value;
                        }

                  private byte[] indexed_colour_to_greyscale(byte[] unfiltered_data)
                  {
                     if (palette_table_p == null)
                     {
                        throw new InvalidOperationException("PNG_Decoder: Invalid greyscale conversion (Indexed)");
                     }

                     int byte_index = 0;
                     List<byte> converted_bytes = new List<byte>();

                     while (byte_index < unfiltered_data.Length)
                     {
                        byte palette_index = unfiltered_data[byte_index++];
                        byte alpha = 255; // 255 == sentinel value (opaque)

                        byte R = (byte)palette_table_p[palette_index][0];
                        byte G = (byte)palette_table_p[palette_index][1];
                        byte B = (byte)palette_table_p[palette_index][2];
                        
                        if (palette_alpha_table_p != null)
                        {
                           alpha = unfiltered_data[byte_index++];
                        }

                        double translucence = (double)(255 - alpha) / 255;
                        double grey_value = (double)rgb_to_grey(R, G, B);

                        if (bKGD_p != null)
                        {
                           double bg_grey_value = (double)rgb_to_grey(bg_R_p, bg_G_p, bg_B_p);

                           grey_value = (1 - translucence) * grey_value + translucence * bg_grey_value;
                        }
                        else// if (alpha > 0)
                        {
                           grey_value = (translucence * byte.MaxValue) + ((1 - translucence) * grey_value);
                        }

                        byte converted_byte = (byte)(int)grey_value;

                        // keep track of this converted byte
                        converted_bytes.Add(converted_byte);
                     }

                     return converted_bytes.ToArray();
                  }

                  private byte[] greyscale_with_alpha_to_greyscale(byte[] unfiltered_data)
                  {
                     if (colour_type != Colour_Types.GREYSCALE_WITH_ALPHA)
                     {
                        throw new InvalidOperationException("PNG_Decoder: Invalid state when attempting to convert pixel to greyscale equivalent (Greyscale_with_alpha).");
                     }

                     List<byte> converted_bytes = new List<byte>();
                     int byte_index = 0;

                     for (int row = 0; row < height_p; row++)
                     {
                        for (int pixel = 0; pixel < width_p; pixel++)
                        {
                           // get the greyscale byte
                           double greyscale_value = (double)unfiltered_data[byte_index++];

                           // get the alpha value
                           greyscale_value *= ((double)unfiltered_data[byte_index++]) / 255;

                           // convert to a greyscale byte
                           byte greyscale_byte = (byte)(int)greyscale_value;

                           converted_bytes.Add(greyscale_byte);
                        }
                     }

                     return converted_bytes.ToArray();
                  }

      // Fields
      private byte[] data_p;
      private bool is_valid_png_p;
      private int pixel_width_p;
      private PNG_Chunk[] PNG_Chunks;
      private CRC_Handler crc_handler;
      // IHDR Fields
      private PNG_Chunk IHDR_p;
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
            private byte[][] palette_table_p;
            private byte[] palette_alpha_table_p;
      // IDAT Fields
      private PNG_Chunk[] IDAT_chunks_p;
            private byte[] decompressed_datastream_p;
            private byte[] unfiltered_datastream_p;
      // CRC Fields
      private byte[][] CRCs_p;
      // Ancillary Chunk information
      private PNG_Chunk tIME_p;
      private List<PNG_Chunk> zTXt_p;
      private List<PNG_Chunk> tEXt_p;
      private List<PNG_Chunk> iTXt_p;
      private PNG_Chunk pHYs_p;
      private List<PNG_Chunk> sPLT_p;
      private PNG_Chunk iCCP_p;
      private PNG_Chunk sRGB_p;
      private PNG_Chunk sBIT_p;
      private PNG_Chunk gAMA_p;
      private PNG_Chunk cHRM_p;
      private PNG_Chunk tRNS_p;
            private byte trns_grey_p;
            private byte trns_R_p;
            private byte trns_G_p;
            private byte trns_B_p;
      private PNG_Chunk hIST_p;
      private PNG_Chunk bKGD_p;
            private byte bg_R_p;
            private byte bg_G_p;
            private byte bg_B_p;
            private byte bg_greyscale_p;
            private byte bg_palette_index_p;


      // Properties
      public bool is_valid_png
      {
         get { return is_valid_png_p; }
      }
      public int pixel_width
      {
         get { return pixel_width_p; }
      }
      public PNG_Chunk IHDR
      {
         get { return IHDR_p; }
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

      public byte[][] palette_table
      {
         get { return palette_table_p; }
      }
      public byte[] palette_alpha_table
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
      public byte[] unfiltered_datastream
      {
         get { return unfiltered_datastream_p; }
      }
   }
}
